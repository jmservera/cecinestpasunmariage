using Microsoft.Azure.Functions.Worker.Http;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using Telegram.Bot;
using Telegram.Bot.Exceptions;
using Telegram.Bot.Polling;
using Telegram.Bot.Types;
using Telegram.Bot.Types.Enums;

namespace functions.TelegramBot
{
    public class Bot : IDisposable
    {

        private readonly TelegramBotClient _client;
        private readonly ILogger _logger;

        private CancellationTokenSource? _cts;

        public Bot(ILoggerFactory loggerFactory)
        {
            _logger = loggerFactory.CreateLogger<Bot>();
            _client = new TelegramBotClient(System.Environment.GetEnvironmentVariable("TELEGRAM_TOKEN", EnvironmentVariableTarget.Process));

        }

        public async Task Register(string handleUpdateFunctionUrl)
        {
            await _client.SetWebhookAsync(handleUpdateFunctionUrl);
        }

        public async Task UpdateAsync(HttpRequestData req, CancellationToken cancellationToken)
        {
            var request = await req.ReadAsStringAsync();
            if (string.IsNullOrEmpty(request))
                return;

            var update = JsonConvert.DeserializeObject<Telegram.Bot.Types.Update>(request);
            if (update is null)
                return;

            await HandleUpdateAsync(_client, update, cancellationToken);
        }


        async Task HandleUpdateAsync(ITelegramBotClient botClient, Update update, CancellationToken cancellationToken)
        {
            // Only process Message updates: https://core.telegram.org/bots/api#message
            if (update.Message is not { } message)
                return;

            _logger.LogInformation($"Received a '{message.Type}' update from user '{message.Chat.FirstName} {message.Chat.LastName} ({message.Chat.Username})'  chat '{message.Chat.Id}'.");


            if (message.Document is { } document)
            {
                _logger.LogInformation($"Received a document {document.FileName} of size {document.FileSize} in chat {message.Chat.Id}.");

                using (MemoryStream stream = new())
                {
                    Telegram.Bot.Types.File file = await botClient.GetFileAsync(document.FileId, cancellationToken: cancellationToken);

                    if (file.FilePath != null)
                    {
                        await botClient.DownloadFileAsync(file.FilePath, stream, cancellationToken: cancellationToken);
                    }
                }
            }
            if (message.Photo is { } photo)
            {
                var p = photo.Last();
                _logger.LogInformation($"Received a photo '{message.Caption}' of size {p.FileSize} in chat {message.Chat.Id}.");

                await botClient.SendTextMessageAsync(
                    chatId: message.Chat.Id,
                    text: $"Received a photo of size {p.FileSize} in chat {message.Chat.Id}.",
                    cancellationToken: cancellationToken);
                using (MemoryStream stream = new())
                {
                    Telegram.Bot.Types.File file = await botClient.GetFileAsync(p.FileId, cancellationToken: cancellationToken);

                    if (file.FilePath != null)
                    {
                        await botClient.DownloadFileAsync(file.FilePath, stream, cancellationToken: cancellationToken);
                    }
                }

            }
            // Only process text messages
            if (message.Text is not { } messageText)
                return;

            var chatId = message.Chat.Id;

            _logger.LogInformation($"Received a '{messageText}' message in chat {chatId}.");

            // Echo received message text
            Message sentMessage = await botClient.SendTextMessageAsync(
                chatId: chatId,
                text: "You said:\n" + messageText,
                cancellationToken: cancellationToken);
        }

        Task HandlePollingErrorAsync(ITelegramBotClient botClient, Exception exception, CancellationToken cancellationToken)
        {
            var ErrorMessage = exception switch
            {
                ApiRequestException apiRequestException
                    => $"Telegram API Error:\n[{apiRequestException.ErrorCode}]\n{apiRequestException.Message}",
                _ => exception.ToString()
            };

            _logger.LogError(exception, ErrorMessage);
            return Task.CompletedTask;
        }


        public async Task RunBot()
        {
            _logger.LogInformation("Starting bot.");
            if (_cts is not null)
                throw new InvalidOperationException("Bot is already running.");
            _cts = new();
            // StartReceiving does not block the caller thread. Receiving is done on the ThreadPool.
            ReceiverOptions receiverOptions = new()
            {
                AllowedUpdates = Array.Empty<UpdateType>() // receive all update types except ChatMember related updates
            };

            _client.StartReceiving(
                updateHandler: HandleUpdateAsync,
                pollingErrorHandler: HandlePollingErrorAsync,
                receiverOptions: receiverOptions,
                cancellationToken: _cts.Token
            );

            var me = await _client.GetMeAsync();

            _logger.LogInformation($"Start listening for @{me.Username}");
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
                _cts?.Cancel();
                _cts?.Dispose();
            }
        }

        ~Bot()
        {
            Dispose(disposing: false);
        }
    }
}