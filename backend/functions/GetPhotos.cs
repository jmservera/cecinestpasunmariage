using System.Net;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Azure.Functions.Worker.Http;
using Microsoft.Extensions.Logging;
using Azure.Storage.Blobs;
using Azure.Storage.Blobs.Models;
using System.Security.Claims;
using functions.Storage;
using Microsoft.Extensions.Configuration;

namespace functions
{
    public class GetPhotos(ILogger<GetPhotos> logger, IConfiguration configuration)
    {
        public const string PicsContainerName = "pics";
        public const string ThumbnailsContainerName = "thumbnails";

        private readonly ILogger<GetPhotos> _logger = logger;
        private readonly IConfiguration _configuration = configuration;

        [Function("GetPhotos")]
        public async Task<HttpResponseData> Run([HttpTrigger(AuthorizationLevel.Function, "get", "post")] HttpRequestData req, int page = 0, int n = 10, string lang = "en")
        {
            _logger.LogInformation("C# HTTP trigger function request for {name}.", nameof(GetPhotos));

            //access to blob storage
            //get connection string from environment variable
            string connectionString = _configuration.GetValue<string>("STORAGE_CONNECTION_STRING") ?? throw new InvalidOperationException("STORAGE_CONNECTION_STRING is not set.");

            BlobContainerClient containerPicsClient = new(connectionString, PicsContainerName);
            BlobContainerClient containerThumbnailsClient = new(connectionString, ThumbnailsContainerName);

            PhotosResponse photosResponse = new();


            var allTumbThePics = containerThumbnailsClient.GetBlobs().ToList().OrderByDescending(b => b.Properties.LastModified);
            photosResponse.NumPictures = allTumbThePics.Count();
            var allPics = containerPicsClient.GetBlobs();

            // todo: use https://learn.microsoft.com/en-us/azure/architecture/web-apps/guides/security/secure-single-page-application-authorization
            // to secure the access to the blob storage


            List<Photo> pictures = [];

            await Parallel.ForEachAsync(allTumbThePics.Skip(page * n).Take(n), async (thumbItem, token) =>
            {
                BlobClient blobThumbClient = containerThumbnailsClient.GetBlobClient(thumbItem.Name);
                var blobSasUriThumb = blobThumbClient.GenerateSasUri(Azure.Storage.Sas.BlobSasPermissions.Read, DateTimeOffset.UtcNow.AddMinutes(15));
                var metadata = await blobThumbClient.GetPropertiesAsync(cancellationToken: token);

                // extensions may differ between the thumbnail and the picture
                var noExtension = Path.Join(Path.GetDirectoryName(thumbItem.Name), Path.GetFileNameWithoutExtension(thumbItem.Name));
                // find the original picture for the thumbnail
                var pic = allPics.Where(b => Path.Join(Path.GetDirectoryName(b.Name), Path.GetFileNameWithoutExtension(b.Name)) == noExtension).FirstOrDefault();
                if (pic is { })
                {
                    BlobClient blobPicsClient = containerPicsClient.GetBlobClient(pic.Name);
                    var blobSasUriPics = blobPicsClient.GenerateSasUri(Azure.Storage.Sas.BlobSasPermissions.Read, DateTimeOffset.UtcNow.AddMinutes(15));

                    metadata.Value.Metadata.TryGetValue(StorageManager.UploadedByMetadataKey, out string? author);
                    author = author?.Split('@')[0];
                    metadata.Value.Metadata.TryGetValue(StorageManager.DescriptionMetadataKey + lang, out string? description);
                    if (string.IsNullOrEmpty(description))
                    {
                        metadata.Value.Metadata.TryGetValue(StorageManager.DescriptionMetadataKey, out description);
                    }
                    if (string.IsNullOrEmpty(description))
                    {
                        metadata.Value.Metadata.TryGetValue(StorageManager.OriginalFilenameMetadataKey, out description);
                    }
                    if (!string.IsNullOrEmpty(description))
                    {
                        description = Uri.UnescapeDataString(description);
                    }
                    description ??= pic.Name;

                    pictures.Add(new Photo()
                    {
                        Name = blobPicsClient.Name.ToString(),
                        Uri = blobSasUriPics.ToString(),
                        ThumbnailUri = blobSasUriThumb.ToString(),
                        Author = author,
                        Description = description,
                        LastModified = thumbItem.Properties.LastModified
                    });
                }
            });

            photosResponse.Pictures = pictures.OrderByDescending(p => p.LastModified);
            string langn = "";
            if (lang != "en") { langn = $"&lang={lang}"; }
            photosResponse.Next = (page + 1) * n < photosResponse.NumPictures ? $"?page={page + 1}&n={n}{langn}" : null;

            var response = req.CreateResponse(HttpStatusCode.OK);
            response.Headers.Add("Content-Type", "application/json; charset=utf-8");

            var json = System.Text.Json.JsonSerializer.Serialize(photosResponse);
            _logger.LogInformation("Generated value: {json}", json);
            response.WriteString(json);
            return response;
        }

        struct PhotosResponse
        {
            public PhotosResponse() { }
            public IEnumerable<Photo>? Pictures { get; set; }

            public int NumPictures { get; set; }

            public string? Next { get; set; }
        }

        struct Photo
        {
            public string? Name { get; set; }
            public string? ThumbnailUri { get; set; }
            public string? Uri { get; set; }
            public string? Author { get; set; }
            public string? Description { get; set; }
            public DateTimeOffset? LastModified { get; set; }
        }
    }
}