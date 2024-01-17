using System.ComponentModel.DataAnnotations;
using System.Globalization;
using System.Net;
using functions.TelegramBot;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Azure.Functions.Worker.Http;
using Microsoft.Extensions.Logging;

namespace functions
{
    public class Cecinestpasunbot
    {
        public const string SetUpFunctionName = "Cecinestpasunbotreg";
        public const string UpdateFunctionName = "Cecinestpasunbot";
        private readonly Bot _bot;
        private readonly ILogger _logger;

        public Cecinestpasunbot(ILoggerFactory loggerFactory)
        {
            _logger = loggerFactory.CreateLogger<Cecinestpasunbot>();
            _bot = new Bot(loggerFactory);
        }

        [Function(UpdateFunctionName)]
        public async Task<HttpResponseData> Update([HttpTrigger(AuthorizationLevel.Anonymous, "get", "post")] HttpRequestData req, FunctionContext context,
    CancellationToken cancellationToken)
        {
            _logger.LogInformation("Update webhook called.");

            var response = req.CreateResponse(HttpStatusCode.OK);
            response.Headers.Add("Content-Type", "text/plain; charset=utf-8");
            _logger.LogInformation("Handling update...");
            await _bot.UpdateAsync(req, cancellationToken);
            _logger.LogInformation("Update handled.");
            response.WriteString("OK!");
            return response;
        }

        [Function(SetUpFunctionName)]
        public async Task<HttpResponseData> Register([HttpTrigger(AuthorizationLevel.Anonymous, "get", "post")] HttpRequestData req)
        {

            _logger.LogInformation("Register function called.");

            var response = req.CreateResponse(HttpStatusCode.OK);
            response.Headers.Add("Content-Type", "text/plain; charset=utf-8");

            response.WriteString("Registering...");
            try
            {
                var handleUpdateFunctionUrl = req.Url.ToString().Replace(SetUpFunctionName, UpdateFunctionName,
                                        ignoreCase: true, culture: CultureInfo.InvariantCulture);
                _logger.LogInformation($"Registering bot with url {handleUpdateFunctionUrl}");
                req.Headers.ToList().ForEach(h => _logger.LogInformation($"{h.Key} : {h.Value}"));

                await _bot.Register(handleUpdateFunctionUrl);
                _logger.LogInformation("Bot registered.");
                response.WriteString("OK!");
            }
            catch (Exception e)
            {
                _logger.LogError(e, "Failed to register bot.");
                response.WriteString("Failed to register bot.");
            }

            return response;
        }
    }
}
