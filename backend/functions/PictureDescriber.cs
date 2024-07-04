using System.IO;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization.Metadata;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using Azure;
using Azure.AI.Vision.ImageAnalysis;
using Azure.Storage.Blobs;
using Azure.Storage.Blobs.Models;
using functions.AI;
using functions.Storage;
using Microsoft.Azure.CognitiveServices.Vision.Face;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Extensions.Azure;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Microsoft.SemanticKernel;
using Microsoft.SemanticKernel.ChatCompletion;
using SixLabors.ImageSharp;
using Telegram.Bot.Types;

namespace functions
{
    public partial class PictureDescriber(ILogger<PictureDescriber> logger, IFaceClient faceClient,
     ImageAnalysisClient imageClient,
     IStorageManager storageManager,
    IChatCompletionService chatCompletionService,
    IConfiguration configuration)
    {
        [GeneratedRegex("^\"|\"$")]
        private static partial Regex RemoveDoubleQuotes();
        private readonly ILogger<PictureDescriber> _logger = logger;
        private readonly IFaceClient _faceClient = faceClient;
        private readonly IStorageManager _storageManager = storageManager;
        private readonly IChatCompletionService _chatCompletionService = chatCompletionService;
        private readonly ImageAnalysisClient _imageClient = imageClient;
        private readonly IConfiguration _configuration = configuration;

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

            var descriptions = await GenerateDescriptionsAsync(name, properties.Value.ContentType, people);
            foreach (var (lang, description) in descriptions)
            {
                metadata[StorageManager.DescriptionMetadataKey + lang] = description;
            }
            metadata[StorageManager.DescriptionMetadataKey] = descriptions["en"];

            await client.SetMetadataAsync(metadata);
            await _storageManager.ReplicateMetadataAsync(name, GetPhotos.PicsContainerName, GetPhotos.ThumbnailsContainerName);
        }

        private async Task<Dictionary<string, string>> GenerateDescriptionsAsync(string name, string contentType, IReadOnlyList<string> people)
        {
            // now get a nice description
            var connectionString = _configuration.GetValue<string>("STORAGE_CONNECTION_STRING")?? throw new InvalidOperationException("STORAGE_CONNECTION_STRING is not set.");

            //use the related thumbnail to make it faster and save some tokens
            BlobContainerClient containerThumbnailsClient = new(connectionString, GetPhotos.ThumbnailsContainerName);
            var thumbnailClient = containerThumbnailsClient.GetBlobClient(name);
            var thumbnail = await thumbnailClient.OpenReadAsync();
            if (thumbnail != null)
            {
                byte[] image = new byte[thumbnail.Length];
                await thumbnail.ReadAsync(image);
                try
                {
                    var history = new ChatHistory();
                    
                    history.AddSystemMessage("You are an AI assistant that helps people find a funny description or title of pictures that may contain people known by the requester.");

                    ImageContent imageContent = new(new ReadOnlyMemory<byte>(image), contentType);
                    var items = new ChatMessageContentItemCollection{
                        new TextContent(people.Count>0?$"In this picture you see the following people: {string.Join(',',people)}. Please find a funny title for this picture that includes the provided names.":
                        "Please find a funny title for this picture."), imageContent};
                    history.AddUserMessage(items);

                    var result = await _chatCompletionService.GetChatMessageContentAsync(history);
                    var description = result.Content;
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

                        _logger.LogInformation("Getting translations for blob {name}: {descrition}", name, result.Content);
                        
                        var translations = await _chatCompletionService.GetChatMessageContentAsync(history);
                        
                        var translationsJsonText = translations.Content ?? throw new InvalidOperationException("No translations found");
                        _logger.LogInformation("Translations for blob {name}: {translations}", name, translationsJsonText);
                        // transform the json to a dictionary
                        var translationsDict = JsonSerializer.Deserialize<Dictionary<string, string>>(translationsJsonText);
                        if (translationsDict != null)
                        {
                            // add translationsDict to dict
                            foreach (var (key, value) in translationsDict)
                            {
                                dict.Add(key, value);
                            }
                        }

                        var descriptions = dict.Select(s => new KeyValuePair<string, string>(s.Key,
                            //url encode string to be stored in metadata
                            Uri.EscapeDataString(
                                //remove double quotes
                                RemoveDoubleQuotes().Replace(s.Value, "")
                            ))).ToDictionary();

                        return descriptions;
                    }
                }
                catch (HttpOperationException ex)
                {
                    _logger.LogError(ex, "Error while generating description for blob {name}", name);
                    if (ex.ResponseContent != null)
                    {
                        var error = JsonSerializer.Deserialize<ContentFilterResponse>(ex.ResponseContent);
                        _logger.LogError("Error details: {error}", error.Error.Message);
                    }
                    _logger.LogInformation("Trying to generate description using Azure Cognitive Services for blob {name}", name);
                    var analysisResult = await _imageClient.AnalyzeAsync(BinaryData.FromBytes(image), VisualFeatures.DenseCaptions);
                    var caption = string.Join(", ", analysisResult.Value.DenseCaptions.Values.Select(v => v.Text).Distinct());
                    _logger.LogInformation("Generated description using Azure Cognitive Services for blob {name}: {caption}", name, caption);
                    var dict = new Dictionary<string, string> { { "en", caption } };
                    var descriptions = await _chatCompletionService.GetChatMessageContentAsync("You are an AI assistant that based on a picture description you transform it to a funny sentence in English, French and Spanish.\n" +
                    "The output should be a json file with \"en\", \"fr\" and \"es\" as keys for the translations. Here is the output schema:\n" +
                    "{\n\"en\":\"English description\",\n\"fr\": \"French translation\"\n,\n\"es\": \"Spanish translation\"}\n" +
                    $"Here is the description for the picture: {caption}\n" +
                    $"Here's the people that appear in the picture: {string.Join(',', people)}\n" +
                    "Please provide a funny sentence for this description.");
                    var translationsDict = JsonSerializer.Deserialize<Dictionary<string, string>>(descriptions.Content);
                    if (translationsDict != null)
                    {
                        // add translationsDict to dict
                        foreach (var (key, value) in translationsDict)
                        {
                            dict[key] = value;
                        }
                    }
                    var finalResult = dict.Select(s => new KeyValuePair<string, string>(s.Key,
                            //url encode string to be stored in metadata
                            Uri.EscapeDataString(
                                //remove double quotes
                                RemoveDoubleQuotes().Replace(s.Value, "")
                            ))).ToDictionary();

                    return finalResult;

                }
            }
            return [];
        }
    }
}
