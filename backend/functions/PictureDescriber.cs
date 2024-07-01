using System.IO;
using System.Threading.Tasks;
using Azure.Storage.Blobs;
using Azure.Storage.Blobs.Models;
using functions.AI;
using functions.Storage;
using Microsoft.Azure.CognitiveServices.Vision.Face;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Extensions.Logging;
using Microsoft.SemanticKernel;
using Microsoft.SemanticKernel.ChatCompletion;
using Telegram.Bot.Types;

namespace functions
{
    public class PictureDescriber(ILogger<PictureDescriber> logger, IFaceClient faceClient, IFileUploader fileUploader,
    IChatCompletionService chatCompletionService)
    {
        private readonly ILogger<PictureDescriber> _logger = logger;
        private readonly IFaceClient _faceClient = faceClient;

        private readonly IFileUploader _fileUploader = fileUploader;
        private readonly IChatCompletionService _chatCompletionService = chatCompletionService;

        [Function(nameof(PictureDescriber))]
        public async Task Run([BlobTrigger(GetPhotos.ThumbnailsContainerName+ "/{name}", Connection = "")] BlobClient client, string name)
        {
            var properties=await client.GetPropertiesAsync();
            var metadata=properties.Value.Metadata;
            if (metadata.ContainsKey("people"))
            {
                _logger.LogInformation("Blob {name} already has people metadata", name);
                return;
            }
            
            _logger.LogInformation("C# Blob trigger function Processed blob\n Name: {name}", name);
            FaceRecognition face= new(_logger, _faceClient);
            var blob=await client.OpenReadAsync();            
            var people=await face.IdentifyInPersonGroupAsync(blob);
            foreach (var person in people)
            {
                _logger.LogInformation("Person '{person}' is identified for the face in: {name}", person,name);
            }

            _logger.LogInformation("Adding people metadata to blob {name}", name);           
            metadata.Add(FileUploader.PeopleMetadataKey, string.Join(',', people));

            // now get a nice desc
            var history= new ChatHistory();
            history.AddSystemMessage("You are an AI assistant that helps people find a funny description or title of pictures that may contain people known by the requester.");
           
            var blob2= await client.OpenReadAsync();
            byte[] image=new byte[blob2.Length];
            await blob2.ReadAsync(image);
            ImageContent imageContent=new(new ReadOnlyMemory<byte>(image),properties.Value.ContentType);

            var items=new ChatMessageContentItemCollection{
                new TextContent($"In this picture you see the following people: {string.Join(',',people)}. Please find a funny title for this picture."),
                imageContent
            };
            history.AddUserMessage(items);
            var result= await _chatCompletionService.GetChatMessageContentsAsync(history);
            
            metadata[FileUploader.DescriptionMetadataKey]= result[^1].Content;

            _logger.LogInformation("Setting AI description for blob {name}: {descrition}", name,result[^1].Content);

            await client.SetMetadataAsync(metadata);
            await _fileUploader.ReplicateMetadataAsync(name, GetPhotos.ThumbnailsContainerName, GetPhotos.PicsContainerName);                        
        }
    }
}
