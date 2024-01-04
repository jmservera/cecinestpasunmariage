using System.Net;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Azure.Functions.Worker.Http;
using Microsoft.Extensions.Logging;

namespace NoBoda.Function
{
    public class HttpTrigger
    {
        private readonly ILogger _logger;

        public HttpTrigger(ILoggerFactory loggerFactory)
        {
            _logger = loggerFactory.CreateLogger<HttpTrigger>();
        }

        [Function("GetPhotos")]
        public HttpResponseData Run([HttpTrigger(AuthorizationLevel.Anonymous, "get", "post")] HttpRequestData req)
        {
            _logger.LogInformation("C# HTTP trigger function processed a request.");

            var response = req.CreateResponse(HttpStatusCode.OK);
            response.Headers.Add("Content-Type", "text/plain; charset=utf-8");

            response.WriteString("https://noweddingpictures.blob.core.windows.net/pre/20200217_105638.jpg?sp=r&st=2024-01-04T12:24:10Z&se=2024-01-04T20:24:10Z&sv=2022-11-02&sr=b&sig=p0O64MBnu8GB61gtn2ig1D6DBmxrxWIwY1jOFbOlhR0%3D");


            return response;
        }
    }
}
