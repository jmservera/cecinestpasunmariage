using System.Buffers.Text;
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
    public class Upload(ILogger<Upload> logger, IStorageManager uploader)
    {
        [Function("Upload")]
        public async Task<HttpResponseData> Run([HttpTrigger(AuthorizationLevel.Anonymous, "post")] HttpRequestData req, FunctionContext context)
        {
            var contentType = req.Headers.GetValues("Content-Type").FirstOrDefault();
            var originalFileName = req.Query["name"];

            if (string.IsNullOrEmpty(contentType))
            {
                var badRequestResponse = req.CreateResponse(HttpStatusCode.BadRequest);
                await badRequestResponse.WriteStringAsync("Content-Type not provided.");
                return badRequestResponse;
            }

            logger.LogInformation("Content-Type: {contentType}", contentType);

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

                var name = $"{uploader.GenerateUniqueName()}.{format.FileExtensions.First()}";
                req.Body.Position = 0;
                await uploader.UploadAsync(username, name, GetPhotos.PicsContainerName, req.Body, contentType, originalFileName, context.CancellationToken);
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
                await uploader.UploadAsync(username, name, GetPhotos.ThumbnailsContainerName, thumb, contentType, originalFileName, context.CancellationToken);
            }


            var response = req.CreateResponse(HttpStatusCode.OK);
            return response;
        }
    }
}
