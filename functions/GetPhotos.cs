using System.Net;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Azure.Functions.Worker.Http;
using Microsoft.Extensions.Logging;
using Azure.Storage.Blobs;
using Azure.Storage.Blobs.Models;
using System.Security.Claims;

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
        public HttpResponseData Run([HttpTrigger(AuthorizationLevel.User, "get", "post")] HttpRequestData req, int page = 1)
        {
            _logger.LogInformation("C# HTTP trigger function processed a request.");

            //access to blob storage
            //get connection string from environment variable
            string? connectionString = Environment.GetEnvironmentVariable("STORAGE_CONNECTION_STRING", EnvironmentVariableTarget.Process);

            string containerName = "pics";
            string thumbnailContainerName = "thumbnails";


            BlobContainerClient containerPicsClient = new(connectionString, containerName);
            BlobContainerClient containerThumbnailsClient = new(connectionString, thumbnailContainerName);

            PhotosResponse photosResponse = new();


            var allTumbThePics = containerThumbnailsClient.GetBlobs().ToList().OrderBy(b => b.Name);
            photosResponse.NumPictures = allTumbThePics.Count();
            var allPics = containerPicsClient.GetBlobs();

            // todo: use https://learn.microsoft.com/en-us/azure/architecture/web-apps/guides/security/secure-single-page-application-authorization
            // to secure the access to the blob storage

            foreach (BlobItem blobTumbItem in allTumbThePics.Skip((page - 1) * 10).Take(10))
            {
                BlobClient blobThumbClient = containerThumbnailsClient.GetBlobClient(blobTumbItem.Name);
                var blobSasUriThumb = blobThumbClient.GenerateSasUri(Azure.Storage.Sas.BlobSasPermissions.Read, DateTimeOffset.UtcNow.AddMinutes(15));

                // extensions may differ between the thumbnail and the picture
                var noExtension = Path.Join(Path.GetDirectoryName(blobTumbItem.Name), Path.GetFileNameWithoutExtension(blobTumbItem.Name));
                // find the original picture for the thumbnail
                var pic = allPics.Where(b => Path.Join(Path.GetDirectoryName(b.Name), Path.GetFileNameWithoutExtension(b.Name)) == noExtension).FirstOrDefault();
                if (pic is { })
                {
                    BlobClient blobPicsClient = containerPicsClient.GetBlobClient(pic.Name);
                    var blobSasUriPics = blobPicsClient.GenerateSasUri(Azure.Storage.Sas.BlobSasPermissions.Read, DateTimeOffset.UtcNow.AddMinutes(15));

                    photosResponse.Pictures.Add(new Photo()
                    {
                        Name = blobPicsClient.Name.ToString(),
                        Uri = blobSasUriPics.ToString(),
                        ThumbnailUri = blobSasUriThumb.ToString()
                    });
                }
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

            public List<Photo> Pictures { get; set; } = new List<Photo>();

            public int NumPictures { get; set; }
        }

        public class Photo
        {
            public string? Name { get; set; }
            public string? ThumbnailUri { get; set; }
            public string? Uri { get; set; }
        }
    }
}