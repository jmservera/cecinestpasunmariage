namespace functions.Storage
{
    public interface IFileUploader
    {
        Task UploadAsync(string userName, string fileName, string containerName, Stream stream, string contentType, string? originalFileName, CancellationToken cancellationToken);

        Task ReplicateMetadataAsync(string fileName, string originalContainer, string destContainer);

        string GenerateUniqueName();
    }
}