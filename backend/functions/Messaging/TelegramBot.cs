using functions.Identity;
using functions.Storage;
using Microsoft.Azure.Functions.Worker.Http;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Localization;
using Microsoft.Extensions.Logging;
using Telegram.Bot;
using Telegram.Bot.Polling;
using Telegram.Bot.Types.Enums;
using Telegram.Bot.Types.ReplyMarkups;

namespace functions.Messaging
{

    /// <summary>
    /// A bot that can receive and send messages.
    /// </summary>
    /// <param name="logger"> The logger instance. </param>
    /// <param name="localizer"> The localizer instance. </param>
    /// <param name="uploader"> The storage manager instance. </param>
    /// <param name="configuration"> The configuration instance. </param>
    public class TelegramBot(ILogger<TelegramBot> logger, IStringLocalizer<TelegramBot> localizer, IStorageManager uploader,
     IConfiguration configuration, IBotTextHandler chatService, IChatHistoryManager chatHistoryManager,
     IChatUserMapper chatUserMapper
     ) : IDisposable
    {
        private ITelegramBotClient _client = new TelegramBotClient(configuration.GetValue<string>("TELEGRAM_TOKEN") ?? throw new InvalidOperationException("TELEGRAM_TOKEN is not set."));

        private CancellationTokenSource? _cts;

        /// <summary>
        /// Registers the bot with the given URL.
        /// </summary>
        /// <param name="handleUpdateFunctionUrl"> The URL to register the bot with. </param>
        /// <returns> A task that represents the asynchronous operation. </returns>
        public async Task Register(string handleUpdateFunctionUrl)
        {
            await _client.SetWebhookAsync(handleUpdateFunctionUrl);
        }

        /// <summary>
        /// Updates the bot with the given request.
        /// </summary>
        /// <param name="req"> The request to update the bot with. </param>
        /// <param name="cancellationToken"> The cancellation token. </param>
        /// <returns> A task that represents the asynchronous operation. </returns>
        public async Task UpdateAsync(HttpRequestData req, CancellationToken cancellationToken)
        {
            var request = await req.ReadAsStringAsync();
            if (string.IsNullOrEmpty(request))
            {
                logger.LogError("Received an empty request.");
                return;
            }

            logger.LogInformation("Received a request: {Request}", request);

            // Update is defined with Newtonsoft.Json attributes, so we need to deserialize it with Newtonsoft.Json
            var update = Newtonsoft.Json.JsonConvert.DeserializeObject<Telegram.Bot.Types.Update>(request);
            if (update is null)
            {
                logger.LogError("Failed to deserialize the update.");
                return;
            }

            await new TelegramHandler(chatService, chatHistoryManager, configuration, chatUserMapper, uploader, logger, localizer, _client).HandleUpdateAsync(_client, update, cancellationToken);
        }

        public async Task SendMessage(long chatId, string text, IReplyMarkup? replyMarkup = null)
        {
            await _client.SendTextMessageAsync(
                chatId: chatId,
                text: text,
                replyMarkup: replyMarkup
            );
        }

        public async Task RunBot()
        {
            logger.LogInformation("Starting bot.");
            if (_cts is not null)
                throw new InvalidOperationException("Bot is already running.");
            _cts = new();
            // StartReceiving does not block the caller thread. Receiving is done on the ThreadPool.
            ReceiverOptions receiverOptions = new()
            {
                AllowedUpdates = [] // receive all update types except ChatMember related updates
            };

            var handler = new TelegramHandler(chatService, chatHistoryManager, configuration, chatUserMapper, uploader, logger, localizer, _client);

            _client.StartReceiving(
                updateHandler: handler.HandleUpdateAsync,
                pollingErrorHandler: handler.HandlePollingErrorAsync,
                receiverOptions: receiverOptions,
                cancellationToken: _cts.Token
            );

            var me = await _client.GetMeAsync();

            logger.LogInformation("Start listening for {user}", me.Username);
        }

        public void StopBot()
        {
            _cts?.Cancel();
        }

        public void Dispose()
        {
            Dispose(disposing: true);
            GC.SuppressFinalize(this);
        }

        protected void Dispose(bool disposing)
        {
            if (disposing)
            {
                _cts?.Dispose();
            }
        }

        ~TelegramBot()
        {
            Dispose(disposing: false);
        }
    }
}