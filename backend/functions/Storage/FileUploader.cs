using System.Data;
using Azure.Storage.Blobs;
using Azure.Storage.Blobs.Models;
using Microsoft.Extensions.Logging;

namespace functions.Storage
{
    public class FileUploader : IFileUploader
    {

        private readonly ILogger<FileUploader> _logger;
        public FileUploader(ILogger<FileUploader> logger)
        {
            _logger = logger;
        }

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

            string? connectionString = Environment.GetEnvironmentVariable("STORAGE_CONNECTION_STRING", EnvironmentVariableTarget.Process);
            if (string.IsNullOrEmpty(connectionString))
            {
                throw new NoNullAllowedException($"Environment value STORAGE_CONNECTION_STRING cannot be null.");
            }

            var containerClient = new BlobContainerClient(connectionString, containerName);
            var user = username.Split('@')[0];
            string fullPath = $"{user}/{fileName}";
            var blobClient = containerClient.GetBlobClient(fullPath);

            _logger.LogInformation("Saving {fullPath} to {containerName} container with type {contentType}", fullPath, containerName, contentType);
            BlobUploadOptions options = new()
            {
                HttpHeaders = new BlobHttpHeaders { ContentType = contentType },
                Metadata = new Dictionary<string, string> { { "uploadedBy", username } }
            };
            if (!string.IsNullOrEmpty(originalFileName))
            {
                options.Metadata.Add("originalFilename", originalFileName);
            }
            await blobClient.UploadAsync(stream, options: options, cancellationToken: cancellationToken);
            _logger.LogInformation("{fullPath} Saved", fullPath);
        }
    }
}