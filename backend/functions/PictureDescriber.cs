using System.Text.Json;
using System.Text.RegularExpressions;
using Azure.AI.Vision.ImageAnalysis;
using Azure.Storage.Blobs;
using Microsoft.Azure.CognitiveServices.Vision.Face;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Microsoft.SemanticKernel;
using Microsoft.SemanticKernel.ChatCompletion;
using Microsoft.SemanticKernel.Connectors.OpenAI;
using SixLabors.ImageSharp;

using functions.AI;
using functions.Storage;

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
            var connectionString = _configuration.GetValue<string>("STORAGE_CONNECTION_STRING") ?? throw new InvalidOperationException("STORAGE_CONNECTION_STRING is not set.");

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
                    return await GenerateDescriptionsFromImageOrCaptionsAsync(contentType, image, people);
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

                    return await GenerateDescriptionsFromImageOrCaptionsAsync(contentType, null, people, caption);

                }
            }
            return [];
        }

        private async Task<Dictionary<string, string>> GenerateDescriptionsFromImageOrCaptionsAsync(string contentType, byte[]? image, IReadOnlyList<string> people, string? captions = null)
        {
#pragma warning disable SKEXP0010 // Type is for evaluation purposes only and is subject to change or removal in future updates. Suppress this diagnostic to proceed.
            OpenAIPromptExecutionSettings settings = new() { ResponseFormat = "json_object" };
#pragma warning restore SKEXP0010 // Type is for evaluation purposes only and is subject to change or removal in future updates. Suppress this diagnostic to proceed.
            var history = new ChatHistory();

            history.AddSystemMessage("You are an AI assistant that helps people find a funny description or title of pictures that may contain people known by the requester, in English, French and Spanish, " +
            "considering the destination language characteristics" +
            (people.Count > 0 ? " and do not translate the names for the people in the picture. " : "") +
            "The output should be a json file with the schema:\n" +
            "{\n\"en\":\"English description\",\n\"fr\": \"French translation\"\n,\n\"es\": \"Spanish translation\"}\n");

            ChatMessageContentItemCollection items = [];
            if (captions != null)
            {
                items.Add(new TextContent($"Here is the description for the picture: {captions}\n"));
            }

            items.Add(new TextContent(people.Count > 0 ? $"In this picture you see the following people: {string.Join(',', people)}. Please find a funny title for this picture that includes the provided names." :
                        "Please find a funny title for this picture."));
            if (image != null)
            {
                ImageContent imageContent = new(new ReadOnlyMemory<byte>(image), contentType);
                items.Add(imageContent);
            }
            history.AddUserMessage(items);

            var chatMessageContent = await _chatCompletionService.GetChatMessageContentAsync(history, settings);
            var description = chatMessageContent.Content ?? throw new InvalidOperationException("No translations generated");
            var localizedDescriptions = JsonSerializer.Deserialize<Dictionary<string, string>>(description) ?? throw new InvalidOperationException("No translations converted");
            return localizedDescriptions.Select(
                s => new KeyValuePair<string, string>(s.Key,
                    //url encode string to be stored in metadata
                    Uri.EscapeDataString(
                        //remove double quotes
                        RemoveDoubleQuotes().Replace(s.Value, "")
                    ))).ToDictionary();
        }
    }
}
