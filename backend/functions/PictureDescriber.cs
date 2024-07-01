using System.IO;
using System.Text;
using System.Text.Json;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using Azure;
using Azure.Storage.Blobs;
using Azure.Storage.Blobs.Models;
using functions.AI;
using functions.Storage;
using Microsoft.Azure.CognitiveServices.Vision.Face;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Extensions.Azure;
using Microsoft.Extensions.Logging;
using Microsoft.SemanticKernel;
using Microsoft.SemanticKernel.ChatCompletion;
using Telegram.Bot.Types;

namespace functions
{
    public partial class PictureDescriber(ILogger<PictureDescriber> logger, IFaceClient faceClient, IStorageManager fileUploader,
    IChatCompletionService chatCompletionService)
    {
        [GeneratedRegex("^\"|\"$")]
        private static partial Regex RemoveDoubleQuotes();
        private readonly ILogger<PictureDescriber> _logger = logger;
        private readonly IFaceClient _faceClient = faceClient;

        private readonly IStorageManager _fileUploader = fileUploader;
        private readonly IChatCompletionService _chatCompletionService = chatCompletionService;

        [Function(nameof(PictureDescriber))]
        public async Task Run([BlobTrigger(GetPhotos.PicsContainerName + "/{name}", Connection = "")] BlobClient client, string name)
        {
            var properties = await client.GetPropertiesAsync();
            var metadata = properties.Value.Metadata;
            if (metadata.ContainsKey(StorageManager.PeopleMetadataKey))
            {
                _logger.LogInformation("Blob {name} already has people metadata", name);
                return;
            }
            if (metadata.ContainsKey(StorageManager.DescriptionMetadataKey))
            {
                _logger.LogInformation("Blob {name} already has description metadata", name);
                return;
            }

            _logger.LogInformation("C# Blob trigger function processing blob\n Name: {name}", name);
            IReadOnlyList<string> people = [];
            try
            {
                FaceRecognition face = new(_logger, _faceClient);
                var blob = await client.OpenReadAsync();
                people = await face.IdentifyInPersonGroupAsync(blob);
                foreach (var person in people)
                {
                    _logger.LogInformation("Person '{person}' is identified for the face in: {name}", person, name);
                }

                _logger.LogInformation("Adding people metadata to blob {name}", name);
                metadata.Add(StorageManager.PeopleMetadataKey, string.Join(',', people));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error while identifying people in blob {name}", name);
            }

            var descriptions = await generateDescription(name, properties.Value.ContentType, people);
            foreach (var (lang, description) in descriptions)
            {
                metadata[StorageManager.DescriptionMetadataKey + lang] = description;
            }
            metadata[StorageManager.DescriptionMetadataKey] = descriptions["en"];

            await client.SetMetadataAsync(metadata);
            await _fileUploader.ReplicateMetadataAsync(name, GetPhotos.PicsContainerName, GetPhotos.ThumbnailsContainerName);
        }

        private async Task<Dictionary<string, string>> generateDescription(string name, string contentType, IReadOnlyList<string> people)
        {
            // now get a nice description
            string? connectionString = Environment.GetEnvironmentVariable("STORAGE_CONNECTION_STRING", EnvironmentVariableTarget.Process);

            BlobContainerClient containerThumbnailsClient = new(connectionString, GetPhotos.ThumbnailsContainerName);
            var thumbnailClient = containerThumbnailsClient.GetBlobClient(name);
            var thumbnail = await thumbnailClient.OpenReadAsync();
            byte[] image = new byte[thumbnail.Length];
            await thumbnail.ReadAsync(image);
            ImageContent imageContent = new(new ReadOnlyMemory<byte>(image), contentType);

            var items = new ChatMessageContentItemCollection{
                new TextContent(people.Count>0?$"In this picture you see the following people: {string.Join(',',people)}. Please find a funny title for this picture that includes the provided names.":
                "Please find a funny title for this picture."
                ),
                imageContent
            };
            var history = new ChatHistory();
            history.AddSystemMessage("You are an AI assistant that helps people find a funny description or title of pictures that may contain people known by the requester.");
            history.AddUserMessage(items);
            var result = await _chatCompletionService.GetChatMessageContentsAsync(history);
            var description = result[^1].Content;
            if (description != null)
            {
                var dict = new Dictionary<string, string> { { "en", description } };
                history.Clear();
                if (people.Count > 0)
                {
                    history.AddSystemMessage($"You are an AI assistant that helps people translate funny English sentences into Spanish and French, considering the destination language characteristics and do not translate the names for the people in the picture: {string.Join(',', people)} . The output should be a json file with \"es\" and \"fr\" as keys for the translations. Here is the output schema:\n"
                                            + "{\n\"es\": \"Spanish translation\",\n\"fr\": \"French translation\"\n}");
                }
                else
                {
                    history.AddSystemMessage($"You are an AI assistant that helps people translate funny English sentences into Spanish and French, considering the destination language characteristics. The output should be a json file with \"es\" and \"fr\" as keys for the translations. Here is the output schema:\n"
                                            + "{\n\"es\": \"Spanish translation\",\n\"fr\": \"French translation\"\n}");
                }
                history.AddUserMessage(description);

                _logger.LogInformation("Getting translations for blob {name}: {descrition}", name, result[^1].Content);


                // encode result[^1].Content to ascii
                var translations = await _chatCompletionService.GetChatMessageContentsAsync(history);
                // read json dictionary from translations[^1].Content
                var json = translations[^1].Content;
                if (json == null)
                {
                    throw new InvalidOperationException("No translations found");
                }
                _logger.LogInformation("Translations for blob {name}: {translations}", name, json);
                // transform the json to a dictionary
                var translationsDict = JsonSerializer.Deserialize<Dictionary<string, string>>(json);
                // add translationsDict to dict
                foreach (var (key, value) in translationsDict)
                {
                    dict.Add(key, value);
                }

                var descriptions = dict.Select(s => new KeyValuePair<string, string>(s.Key,
                    //url encode string to be stored in metadata
                    Uri.EscapeDataString(
                        //remove double quotes
                        RemoveDoubleQuotes().Replace(s.Value, "")
                    ))).ToDictionary();

                return descriptions;
            }
            return [];
        }
    }
}
