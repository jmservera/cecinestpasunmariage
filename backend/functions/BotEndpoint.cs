using System.ComponentModel.DataAnnotations;
using System.Globalization;
using System.Net;
using System.Security.Claims;
using functions.Claims;
using functions.Identity;
using functions.Messaging;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Azure.Functions.Worker.Http;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Localization;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using Telegram.Bot.Types.ReplyMarkups;

namespace functions
{
    public class BotEndpoint(ILoggerFactory loggerFactory, TelegramBot bot, IStringLocalizer<TelegramBot> localizer, IChatUserMapper chatUserMapper, IConfiguration configuration)
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

            var principal = ClaimsPrincipalParser.Parse(req);
            var userId = principal.Claims.FirstOrDefault(c => c.Type == ClaimTypes.NameIdentifier)?.Value; //get user id            

            try
            {
                var chatUser = await req.ReadFromJsonAsync<ChatUser>();
                var language = chatUser.Language ?? "en";
                CultureInfo.CurrentCulture = CultureInfo.CurrentUICulture = CultureInfo.GetCultureInfo(language);
                if (string.IsNullOrEmpty(chatUser.ChatId))
                {
                    response = req.CreateResponse(HttpStatusCode.BadRequest);
                    response.Headers.Add("Content-Type", "text/plain; charset=utf-8");
                    _logger.LogWarning("Received an empty request:{ChatUser}", JsonConvert.SerializeObject(chatUser));
                    response.WriteString("Empty request.");
                    return response;
                }
                var key = configuration.GetValue<string>("TELEGRAM_TOKEN") ?? throw new InvalidOperationException("TELEGRAM_TOKEN is not set.");
                chatUser.CheckSealValidity(ChatUser.GetValidKey(key));
                chatUser.UserAuthId = userId;
                chatUser.UserDetails = principal.Identity?.Name;
                await chatUserMapper.SaveUserAsync(chatUser);
                response = req.CreateResponse(HttpStatusCode.OK);
                response.Headers.Add("Content-Type", "application/json; charset=utf-8");
                response.WriteString(JsonConvert.SerializeObject(chatUser));
                await bot.SendMessage(long.Parse(chatUser.ChatId), localizer.GetString("BotAuthenticated"), new ReplyKeyboardRemove());
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
