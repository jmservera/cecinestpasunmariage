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

        public async Task UploadAsync(string username, string fileName, string containerName, Stream stream, string contentType, CancellationToken cancellationToken = default)
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
            string fullPath = $"{username}/{fileName}";
            var blobClient = containerClient.GetBlobClient(fullPath);

            _logger.LogInformation("Saving {fullPath} to {containerName} container with type {contentType}", fullPath, containerName, contentType);
            await blobClient.UploadAsync(stream, new BlobHttpHeaders { ContentType = contentType }, cancellationToken: cancellationToken);
            _logger.LogInformation("{fullPath} Saved", fullPath);
        }
    }
}