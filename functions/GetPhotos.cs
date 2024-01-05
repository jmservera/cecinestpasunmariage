using System.Net;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Azure.Functions.Worker.Http;
using Microsoft.Extensions.Logging;
using Azure.Storage.Blobs;
using Azure.Storage.Blobs.Models;

namespace functions
{
    public class GetPhotos
    {
        private readonly ILogger _logger;

        public GetPhotos(ILoggerFactory loggerFactory)
        {
            _logger = loggerFactory.CreateLogger<GetPhotos>();
        }

        [Function("GetPhotos")]
        public HttpResponseData Run([HttpTrigger(AuthorizationLevel.Function, "get", "post")] HttpRequestData req, int page=1)
        {
            _logger.LogInformation("C# HTTP trigger function processed a request.");

            //access to blob storage
            //get connection string from environment variable
            string connectionString = Environment.GetEnvironmentVariable("STORAGE_CONNECTION_STRING", EnvironmentVariableTarget.Process);
            
            string containerName = "pics";


            BlobContainerClient containerClient = new BlobContainerClient(connectionString, containerName);


            List<String> picUris = new List<String>();

            // get uri for each blob item
            foreach (BlobItem blobItem in containerClient.GetBlobs().ToList().OrderBy(b => b.Name).Take(page * 10))
            {
                BlobClient blobClient = containerClient.GetBlobClient(blobItem.Name);
                
                var blobSasUri = blobClient.GenerateSasUri(Azure.Storage.Sas.BlobSasPermissions.Read, DateTimeOffset.UtcNow.AddMinutes(15));
                picUris.Add(blobSasUri.ToString());
            }

            var response = req.CreateResponse(HttpStatusCode.OK);
            response.Headers.Add("Content-Type", "application/json; charset=utf-8");

            var json = System.Text.Json.JsonSerializer.Serialize(picUris);
            _logger.LogInformation(json);
            response.WriteString(json);
            return response;
        }
    }
}