using System.ComponentModel.DataAnnotations;
using System.Globalization;
using System.Net;
using functions.Identity;
using functions.Messaging;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Azure.Functions.Worker.Http;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using Telegram.Bot.Types.ReplyMarkups;

namespace functions
{
    public class BotEndpoint(ILoggerFactory loggerFactory, TelegramBot bot, IChatUserMapper chatUserMapper)
    {
        const string SetUpFunctionName = "Cecinestpasunbotreg";
        const string UpdateFunctionName = "Cecinestpasunbot";
        const string AuthenticateBot = "AuthenticateBot";

        readonly ILogger _logger = loggerFactory.CreateLogger<BotEndpoint>();

        [Function(UpdateFunctionName)]
        public async Task<HttpResponseData> Update([HttpTrigger(AuthorizationLevel.Anonymous, "get", "post")] HttpRequestData req, FunctionContext context)
        {
            _logger.LogInformation("Update webhook called.");

            var response = req.CreateResponse(HttpStatusCode.OK);
            response.Headers.Add("Content-Type", "text/plain; charset=utf-8");
            _logger.LogInformation("Handling update...");
            await bot.UpdateAsync(req, context.CancellationToken);
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
                req.Headers.TryGetValues("x-ms-original-url", out var values);
                //get x-ms-original-url
                var handleUpdateFunctionUrl = values?.FirstOrDefault();
                if (string.IsNullOrEmpty(handleUpdateFunctionUrl))
                {
                    _logger.LogWarning("x-ms-original-url not found, using current url.");
                    _logger.LogTrace("Request: {Values}", JsonConvert.SerializeObject(values));
                    handleUpdateFunctionUrl = req.Url.ToString();
                }
                handleUpdateFunctionUrl = handleUpdateFunctionUrl.Replace(SetUpFunctionName, UpdateFunctionName, ignoreCase: true, culture: CultureInfo.InvariantCulture);
                _logger.LogInformation("Registering bot with url {UpdateFunctionUrl}", handleUpdateFunctionUrl);

                await bot.Register(handleUpdateFunctionUrl);
                _logger.LogInformation("Bot registered to the address {UpdateFunctionUrl}.", handleUpdateFunctionUrl);
                response.WriteString("Bot registered!");
            }
            catch (Exception e)
            {
                _logger.LogError(e, "Failed to register bot.");
                response.WriteString("Failed to register bot.");
            }

            return response;
        }

        [Function(AuthenticateBot)]
        public async Task<HttpResponseData> Authenticate([HttpTrigger(AuthorizationLevel.Anonymous, "post")] HttpRequestData req)
        {
            _logger.LogInformation("Authenticate function called.");

            HttpResponseData response;

            try
            {
                var chatUser = await req.ReadFromJsonAsync<ChatUser>();
                if (string.IsNullOrEmpty(chatUser.ChatId) || string.IsNullOrEmpty(chatUser.UserId))
                {
                    response = req.CreateResponse(HttpStatusCode.BadRequest);
                    response.Headers.Add("Content-Type", "text/plain; charset=utf-8");
                    _logger.LogWarning("Received an empty request.");
                    response.WriteString("Empty request.");
                    return response;
                }
                await chatUserMapper.SaveUserAsync(chatUser);
                response = req.CreateResponse(HttpStatusCode.OK);
                response.Headers.Add("Content-Type", "application/json; charset=utf-8");
                response.WriteString(JsonConvert.SerializeObject(chatUser));
                await bot.SendMessage(long.Parse(chatUser.ChatId), "You have been authenticated!", new ReplyKeyboardRemove());
            }
            catch (Exception e)
            {
                response = req.CreateResponse(HttpStatusCode.BadRequest);
                response.Headers.Add("Content-Type", "text/plain; charset=utf-8");
                _logger.LogError(e, "Failed to register user.");
            }

            return response;
        }
    }
}
