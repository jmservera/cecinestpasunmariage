using System.Net;
using System.Security.Claims;
using functions.Claims;
using functions.Storage;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Azure.Functions.Worker.Http;
using Microsoft.Extensions.Logging;

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

        readonly string[] allowedContentTypes = ["image/jpeg", "image/png", "image/gif"];

        [Function("Upload")]
        public async Task<HttpResponseData> Run([HttpTrigger(AuthorizationLevel.User, "post")] HttpRequestData req, FunctionContext context)
        {
            var contentType = req.Headers.GetValues("Content-Type").FirstOrDefault();

            if (string.IsNullOrEmpty(contentType))
            {
                var badRequestResponse = req.CreateResponse(HttpStatusCode.BadRequest);
                await badRequestResponse.WriteStringAsync("Content-Type not provided.");
                return badRequestResponse;
            }

            _logger.LogInformation("Content-Type: {contentType}", contentType);

            if (!allowedContentTypes.Contains(contentType))
            {
                var badRequestResponse = req.CreateResponse(HttpStatusCode.BadRequest);
                await badRequestResponse.WriteStringAsync("Content-Type not allowed.");
                return badRequestResponse;
            }

            var username = ClaimsPrincipalParser.Parse(req).Identity?.Name;

            if (string.IsNullOrEmpty(username))
            {
                var unauthorizedResponse = req.CreateResponse(HttpStatusCode.Unauthorized);
                await unauthorizedResponse.WriteStringAsync("Unauthorized");
                return unauthorizedResponse;
            }

            //TODO: GENERATE NAME and EXTENSION
            var name = Path.GetTempFileName();
            await _uploader.UploadAsync(username, $"{name}.jpg", "pics", req.Body, contentType, context.CancellationToken);

            var response = req.CreateResponse(HttpStatusCode.OK);
            return response;
        }
    }
}
