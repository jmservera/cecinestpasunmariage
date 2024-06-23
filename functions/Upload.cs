using System.Net;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Azure.Functions.Worker.Http;
using Microsoft.Extensions.Logging;

namespace functions
{
    public class Upload
    {
        private readonly ILogger<Upload> _logger;

        public Upload(ILoggerFactory loggerFactory)
        {
            _logger = loggerFactory.CreateLogger<Upload>();
        }

        string[] allowedContentTypes = new string[] { "image/jpeg", "image/png", "image/gif" };

        [Function("Upload")]
        public async Task<HttpResponseData> Run([HttpTrigger(AuthorizationLevel.Anonymous, "get", "post")] HttpRequestData req, FunctionContext context)
        {
            var contentType = req.Headers.GetValues("Content-Type").FirstOrDefault();

            _logger.LogInformation("Content-Type: {contentType}", contentType);

            if (!allowedContentTypes.Contains(contentType))
            {
                var badRequestResponse = req.CreateResponse(HttpStatusCode.BadRequest);
                await badRequestResponse.WriteStringAsync("Content-Type not allowed.");
                return badRequestResponse;
            }

            _logger.LogInformation("C# HTTP trigger function upload.");
            var response = req.CreateResponse(HttpStatusCode.OK);
            return response;
        }
    }
}
