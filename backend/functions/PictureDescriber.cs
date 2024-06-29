using System.IO;
using System.Threading.Tasks;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Extensions.Logging;

namespace functions
{
    public class PictureDescriber
    {
        private readonly ILogger<PictureDescriber> _logger;

        public PictureDescriber(ILogger<PictureDescriber> logger)
        {
            _logger = logger;
        }

        [Function(nameof(PictureDescriber))]
        public async Task Run([BlobTrigger("thumbnails/{name}", Connection = "")] Stream stream, string name)
        {
            using var blobStreamReader = new StreamReader(stream);
            var content = await blobStreamReader.ReadToEndAsync();
            _logger.LogInformation("C# Blob trigger function Processed blob\n Name: {name}", name, content);
        }
    }
}
