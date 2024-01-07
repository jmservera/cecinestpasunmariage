using System.Net;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Azure.Functions.Worker.Http;
using Microsoft.Extensions.Logging;
using Azure.Storage.Blobs;
using Azure.Storage.Blobs.Models;
using Microsoft.CodeAnalysis.CSharp.Syntax;

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
        public HttpResponseData Run([HttpTrigger(AuthorizationLevel.Anonymous, "get", "post")] HttpRequestData req, int page = 1)
        {
            _logger.LogInformation("C# HTTP trigger function processed a request.");

            //access to blob storage
            //get connection string from environment variable
            string connectionString = Environment.GetEnvironmentVariable("STORAGE_CONNECTION_STRING", EnvironmentVariableTarget.Process);

            string containerName = "pics";


            BlobContainerClient containerClient = new BlobContainerClient(connectionString, containerName);

            PhotosResponse photosResponse = new PhotosResponse();
            

            var allThePics = containerClient.GetBlobs().ToList().OrderBy(b => b.Name);
            photosResponse.NumPictures = allThePics.Count();


            foreach (BlobItem blobItem in allThePics.Take(10*page).Skip((page - 1) * 10))
            {
                BlobClient blobClient = containerClient.GetBlobClient(blobItem.Name);

                var blobSasUri = blobClient.GenerateSasUri(Azure.Storage.Sas.BlobSasPermissions.Read, DateTimeOffset.UtcNow.AddMinutes(15));
                photosResponse.picUris.Add(new Photo(){ Name = blobClient.Name.ToString(), Uri = blobSasUri.ToString() });
            }

            var response = req.CreateResponse(HttpStatusCode.OK);
            response.Headers.Add("Content-Type", "application/json; charset=utf-8");

            var json = System.Text.Json.JsonSerializer.Serialize(photosResponse);
            _logger.LogInformation(json);
            response.WriteString(json);
            return response;
        }

        protected class PhotosResponse
        {
            public List<Photo> picUris = new List<Photo>();
            public int NumPictures { get; set; }
        }

        protected class Photo
        {
            public string Name { get; set; }
            public string Uri { get; set; }
        }
    }
}