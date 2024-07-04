using System.Data;
using Azure.Storage.Blobs;
using Azure.Storage.Blobs.Models;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;

namespace functions.Storage
{
    public class StorageManager(ILogger<StorageManager> logger, IConfiguration configuration) : IStorageManager
    {

        public const string UploadedByMetadataKey = "uploadedBy";
        public const string OriginalFilenameMetadataKey = "originalFilename";

        public const string DescriptionMetadataKey = "description";

        public const string PeopleMetadataKey = "people";
        private readonly ILogger<StorageManager> _logger = logger;

        private readonly IConfiguration _configuration = configuration;

        public string GenerateUniqueName()
        {
            //encode the guid to base64
            var base64Guid = Convert.ToBase64String(Guid.NewGuid().ToByteArray());
            // encode to base64url
            return base64Guid.Replace('+', '-').Replace('/', '_').TrimEnd('=');
        }

        public async Task UploadAsync(string username, string fileName, string containerName, Stream stream, string contentType, string? originalFileName, CancellationToken cancellationToken = default)
        {
            if (string.IsNullOrEmpty(username))
            {
                throw new ArgumentNullException(nameof(username));
            }
            if (string.IsNullOrEmpty(fileName))
            {
                throw new ArgumentNullException(nameof(fileName));
            }
            if (string.IsNullOrEmpty(containerName))
            {
                throw new ArgumentNullException(nameof(containerName));
            }

            string connectionString = _configuration.GetValue<string>("STORAGE_CONNECTION_STRING") ?? throw new InvalidOperationException("STORAGE_CONNECTION_STRING is not set.");

            var containerClient = new BlobContainerClient(connectionString, containerName);
            var user = username.Split('@')[0];
            string fullPath = $"{user}/{fileName}";
            var blobClient = containerClient.GetBlobClient(fullPath);

            _logger.LogInformation("Saving {fullPath} to {containerName} container with type {contentType}", fullPath, containerName, contentType);
            BlobUploadOptions options = new()
            {
                HttpHeaders = new BlobHttpHeaders { ContentType = contentType },
                Metadata = new Dictionary<string, string> { { UploadedByMetadataKey, username } }
            };
            if (!string.IsNullOrEmpty(originalFileName))
            {
                options.Metadata.Add(OriginalFilenameMetadataKey, originalFileName);
            }
            await blobClient.UploadAsync(stream, options: options, cancellationToken: cancellationToken);
            _logger.LogInformation("{fullPath} Saved", fullPath);
        }

        public async Task ReplicateMetadataAsync(string fileName, string originalContainer, string destContainer)
        {

            if (string.IsNullOrEmpty(fileName))
            {
                throw new ArgumentNullException(nameof(fileName));
            }
            if (string.IsNullOrEmpty(originalContainer))
            {
                throw new ArgumentNullException(nameof(originalContainer));
            }
            if (string.IsNullOrEmpty(destContainer))
            {
                throw new ArgumentNullException(nameof(destContainer));
            }

            string connectionString = _configuration.GetValue<string>("STORAGE_CONNECTION_STRING") ?? throw new InvalidOperationException("STORAGE_CONNECTION_STRING is not set.");

            _logger.LogInformation("Replicating metadata from {originalContainer}/{fileName} to {destContainer}/{fileName}", originalContainer, fileName, destContainer, fileName);
            var containerClient = new BlobContainerClient(connectionString, originalContainer);
            var originalBlobClient = containerClient.GetBlobClient(fileName);
            var properties = await originalBlobClient.GetPropertiesAsync();
            var metadata = properties.Value.Metadata;

            _logger.LogInformation("Setting metadata to {destContainer}/{fileName}", destContainer, fileName);
            var destContainerClient = new BlobContainerClient(connectionString, destContainer);
            var destBlobClient = destContainerClient.GetBlobClient(fileName);
            await destBlobClient.SetMetadataAsync(metadata);
        }
    }
}