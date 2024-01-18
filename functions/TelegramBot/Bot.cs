using System.Data;
using System.Globalization;
using System.Web;
using Azure.Storage.Blobs;
using Azure.Storage.Blobs.Models;
using Microsoft.Azure.Functions.Worker.Http;
using Microsoft.Extensions.Localization;
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

        private ITelegramBotClient _client;
        private readonly ILogger _logger;

        private readonly IStringLocalizer<Bot> _localizer;

        private CancellationTokenSource? _cts;

        public Bot(ILoggerFactory loggerFactory, IStringLocalizer<Bot> localizer)
        {
            _localizer = localizer;
            _logger = loggerFactory.CreateLogger<Bot>();
            _client = new TelegramBotClient(Environment.GetEnvironmentVariable("TELEGRAM_TOKEN", EnvironmentVariableTarget.Process));
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

        public async Task HandleCommand(Message message, CancellationToken cancellationToken)
        {

            var command = message.Text?.Split(" ")[0];
            switch (command)
            {
                case "/start":
                    await _client.SendTextMessageAsync(
                        chatId: message.Chat.Id,
                        text: _localizer.GetString("GreetingMessage"),
                        parseMode: ParseMode.MarkdownV2,
                        cancellationToken: cancellationToken);
                    break;
                case "/help":
                    await _client.SendTextMessageAsync(
                        chatId: message.Chat.Id,
                        text: _localizer.GetString("Help"),
                        parseMode: ParseMode.MarkdownV2,
                        cancellationToken: cancellationToken);
                    break;
                case "/echo":
                    await _client.SendTextMessageAsync(
                        chatId: message.Chat.Id,
                        text: "Echo",
                        cancellationToken: cancellationToken);
                    break;
                default:
                    await _client.SendTextMessageAsync(
                        chatId: message.Chat.Id,
                        text: "Unknown command",
                        cancellationToken: cancellationToken);
                    break;
            }
            return;

        }

        async Task Save(string containerName, string fileName, Stream stream, CancellationToken cancellationToken, string mimeType = "image/jpeg")
        {
            string? connectionString = Environment.GetEnvironmentVariable("STORAGE_CONNECTION_STRING", EnvironmentVariableTarget.Process);

            BlobContainerClient containerClient = new(connectionString, containerName);

            BlobClient blobClient = containerClient.GetBlobClient(fileName);

            await blobClient.UploadAsync(stream, new BlobHttpHeaders { ContentType = mimeType }, cancellationToken: cancellationToken);
        }

        async Task SendToStorage(Message message, CancellationToken cancellationToken)
        {

            if (message.Photo is { } photos)
            {
                string uid = "";
                string username = HttpUtility.HtmlEncode(message.From?.Username ?? "");

                if (!string.IsNullOrEmpty(username)) username = $"{username}/";

                if (photos.Skip(1).FirstOrDefault() is { } thumb)
                {
                    using MemoryStream stream = new();
                    Telegram.Bot.Types.File file = await _client.GetFileAsync(thumb.FileId, cancellationToken: cancellationToken);
                    uid = file.FileUniqueId;

                    if (file.FilePath != null)
                    {
                        await _client.DownloadFileAsync(file.FilePath, stream, cancellationToken: cancellationToken);
                        stream.Seek(0, SeekOrigin.Begin);

                        await Save("thumbnails", $"{username}{uid}{Path.GetExtension(file.FilePath)}", stream, cancellationToken);
                    }
                }
                if (photos.Last() is { } p)
                {
                    using MemoryStream stream = new();
                    Telegram.Bot.Types.File file = await _client.GetFileAsync(p.FileId, cancellationToken: cancellationToken);

                    if (file.FilePath != null)
                    {
                        await _client.DownloadFileAsync(file.FilePath, stream, cancellationToken: cancellationToken);
                        stream.Seek(0, SeekOrigin.Begin);
                        await Save("pics", $"{username}{uid}{Path.GetExtension(file.FilePath)}", stream, cancellationToken);
                    }
                }




            }
        }

        async Task HandleUpdateAsync(ITelegramBotClient botClient, Update update, CancellationToken cancellationToken)
        {
            _client = botClient;
            // Only process Message updates: https://core.telegram.org/bots/api#message
            if (update.Message is not { } message)
                return;

            var language = update.Message.From?.LanguageCode ?? "en";
            //_localizer.WithCulture(new CultureInfo(language));
            CultureInfo.CurrentCulture = CultureInfo.CurrentUICulture = CultureInfo.GetCultureInfo(language);

            _logger.LogInformation($"Received a '{message.Type}' update from user '{message.Chat.FirstName} {message.Chat.LastName} ({message.Chat.Username})'  chat '{message.Chat.Id}'.");


            if (message.Document is { } document)
            {
                _logger.LogInformation($"Received a document {document.FileName} of size {document.FileSize} in chat {message.Chat.Id}.");

                using (MemoryStream stream = new())
                {
                    //                    Telegram.Bot.Types.File file = await botClient.GetFileAsync(document.FileId, cancellationToken: cancellationToken);

                    // if (file.FilePath != null)
                    // {
                    //     await botClient.DownloadFileAsync(file.FilePath, stream, cancellationToken: cancellationToken);
                    // }
                    await botClient.SendTextMessageAsync(
                        chatId: message.Chat.Id,
                        parseMode: ParseMode.MarkdownV2,
                        text: _localizer.GetString("PictureNotAdded"),
                        cancellationToken: cancellationToken);
                }
            }
            if (message.Photo is { } p)
            {
                _logger.LogInformation($"Received a photo '{message.Caption}' of size {p.LastOrDefault()?.FileSize} in chat {message.Chat.Id}.");
                try
                {
                    await SendToStorage(message, cancellationToken);

                    await botClient.SendTextMessageAsync(
                        chatId: message.Chat.Id,
                        parseMode: ParseMode.MarkdownV2,
                        text: _localizer.GetString("PictureAdded"),
                        cancellationToken: cancellationToken);
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Error sending to storage");
                    await botClient.SendTextMessageAsync(
                        chatId: message.Chat.Id,
                        parseMode: ParseMode.MarkdownV2,
                        text: _localizer.GetString("PictureNotAdded"),
                        cancellationToken: cancellationToken);
                }

            }
            // Only process text messages
            if (message.Text is not { } messageText)
                return;

            if (messageText.StartsWith("/"))
            {
                await HandleCommand(message, cancellationToken);
            }
            else
            {
                var chatId = message.Chat.Id;

                _logger.LogInformation($"Received a '{messageText}' message in chat {chatId}.");

                // Echo received message text
                Message sentMessage = await botClient.SendTextMessageAsync(
                    chatId: chatId,
                    text: _localizer.GetString("EchoMessage", messageText),
                    cancellationToken: cancellationToken);
            }
        }

        Task HandlePollingErrorAsync(ITelegramBotClient botClient, Exception exception, CancellationToken cancellationToken)
        {
            _client = botClient;

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
                _cts?.Dispose();
            }
        }

        ~Bot()
        {
            Dispose(disposing: false);
        }
    }
}