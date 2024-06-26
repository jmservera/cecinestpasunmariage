using System.Net;
using functions.Claims;
using functions.Storage;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Azure.Functions.Worker.Http;
using Microsoft.Extensions.Logging;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.Formats;
using SixLabors.ImageSharp.Processing;

namespace functions
{
    public class Upload
    {
        private readonly ILogger<Upload> _logger;
        private readonly IFileUploader _uploader;

        public Upload(ILogger<Upload> logger, IFileUploader uploader)
        {
            _logger = logger;
            _uploader = uploader;
        }

        [Function("Upload")]
        public async Task<HttpResponseData> Run([HttpTrigger(AuthorizationLevel.Anonymous, "post")] HttpRequestData req, FunctionContext context)
        {
            var contentType = req.Headers.GetValues("Content-Type").FirstOrDefault();

            if (string.IsNullOrEmpty(contentType))
            {
                var badRequestResponse = req.CreateResponse(HttpStatusCode.BadRequest);
                await badRequestResponse.WriteStringAsync("Content-Type not provided.");
                return badRequestResponse;
            }

            _logger.LogInformation("Content-Type: {contentType}", contentType);

            var username = ClaimsPrincipalParser.Parse(req).Identity?.Name;

            if (string.IsNullOrEmpty(username))
            {
                var unauthorizedResponse = req.CreateResponse(HttpStatusCode.Unauthorized);
                await unauthorizedResponse.WriteStringAsync("Unauthorized");
                return unauthorizedResponse;
            }

            using (var image = Image.Load(req.Body))
            {
                image.Configuration.ImageFormatsManager.TryFindFormatByMimeType(contentType, out IImageFormat? format);
                if (format is null)
                {
                    var badRequestResponse = req.CreateResponse(HttpStatusCode.BadRequest);
                    await badRequestResponse.WriteStringAsync("Unsupported format.");
                    return badRequestResponse;
                }

                var name = $"{Path.GetRandomFileName()}.{format.FileExtensions.First()}";
                req.Body.Position = 0;
                await _uploader.UploadAsync(username, name, "pics", req.Body, contentType, context.CancellationToken);
                // Resize the image to create a thumbnail
                ResizeOptions resizeOptions = new()
                {
                    Mode = ResizeMode.Max,
                    Size = new Size(320, 320)
                };

                image.Mutate(o => o.Resize(resizeOptions));

                await using var thumb = new MemoryStream();
                image.Save(thumb, format);
                thumb.Position = 0;
                await _uploader.UploadAsync(username, name, "thumbnails", thumb, contentType, context.CancellationToken);
            }


            var response = req.CreateResponse(HttpStatusCode.OK);
            return response;
        }
    }
}
