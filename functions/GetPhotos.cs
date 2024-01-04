using System.Net;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Azure.Functions.Worker.Http;
using Microsoft.Extensions.Logging;

namespace functions
{
    public class GetPhotos
    {
        private readonly ILogger _logger;

        public GetPhotos(ILoggerFactory loggerFactory)
        {
            _logger = loggerFactory.CreateLogger<GetPhotos>();
        }

        [Function("GetPhotos")]
        public HttpResponseData Run([HttpTrigger(AuthorizationLevel.Function, "get", "post")] HttpRequestData req)
        {
            _logger.LogInformation("C# HTTP trigger function processed a request.");

            var response = req.CreateResponse(HttpStatusCode.OK);
            response.Headers.Add("Content-Type", "application/json; charset=utf-8");

            var obj = new { pictures = new string[] { "una", "dos", "tres" } };
            var json = System.Text.Json.JsonSerializer.Serialize(obj);
            _logger.LogInformation(json);
            response.WriteString(json);
            return response;
        }
    }
}
