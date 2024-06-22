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
            string? token = Environment.GetEnvironmentVariable("TELEGRAM_TOKEN", EnvironmentVariableTarget.Process);
            _client = token == null ? throw new ArgumentNullException(nameof(token)) : new TelegramBotClient(token);
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

        async Task HandleCommand(Message message, CancellationToken cancellationToken)
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

        private async Task TelegramPictureToBlob(string containerName, Telegram.Bot.Types.File file, string uid, string username, CancellationToken cancellationToken, string mimeType = "image/jpeg")
        {
            if (file.FilePath != null)
            {
                using MemoryStream stream = new();
                await _client.DownloadFileAsync(file.FilePath, stream, cancellationToken: cancellationToken);
                stream.Seek(0, SeekOrigin.Begin);
                await SaveToBlob(containerName, $"{username}{uid}{Path.GetExtension(file.FilePath)}", stream, cancellationToken, mimeType);
            }
        }

        async Task SaveToBlob(string containerName, string fileName, Stream stream, CancellationToken cancellationToken, string mimeType = "image/jpeg")
        {
            string? connectionString = Environment.GetEnvironmentVariable("STORAGE_CONNECTION_STRING", EnvironmentVariableTarget.Process);

            if (string.IsNullOrEmpty(connectionString))
            {
                throw new NoNullAllowedException("Environment value STORAGE_CONNECTION_STRING cannot be null.");
            }
            BlobContainerClient containerClient = new(connectionString, containerName);
            BlobClient blobClient = containerClient.GetBlobClient(fileName);

            _logger.LogInformation("Saving {FileName} to {ContainerName} container with type {MimeType}", fileName, containerName, mimeType);
            await blobClient.UploadAsync(stream, new BlobHttpHeaders { ContentType = mimeType }, cancellationToken: cancellationToken);
            _logger.LogInformation("{FileName} Saved", fileName);
        }

        async Task SendPhotoToStorage(Message message, CancellationToken cancellationToken)
        {
            if (message.Photo is { } photos)
            {
                string uid = "";
                string username = HttpUtility.HtmlEncode(message.From?.Username ?? "");

                if (!string.IsNullOrEmpty(username)) username = $"{username}/";

                if (photos.Skip(1).FirstOrDefault() is { } thumb)
                {
                    Telegram.Bot.Types.File file = await _client.GetFileAsync(thumb.FileId, cancellationToken: cancellationToken);
                    uid = file.FileUniqueId;

                    await TelegramPictureToBlob("thumbnails", file, uid, username, cancellationToken);
                }
                if (photos.Last() is { } p)
                {
                    Telegram.Bot.Types.File file = await _client.GetFileAsync(p.FileId, cancellationToken: cancellationToken);
                    await TelegramPictureToBlob("pics", file, uid, username, cancellationToken);
                }
            }
        }


        async Task SendDocumentToStorage(Message message, CancellationToken cancellationToken)
        {
            if (message.Document is { } doc)
            {
                switch (doc.MimeType)
                {
                    case "image/jpeg":
                        break;
                    case "image/png":
                        break;
                    case "image/gif":
                        break;
                    default:
                        throw new InvalidOperationException($"Invalid file type {message.Document?.MimeType}");
                }
                string uid = "";
                string username = HttpUtility.HtmlEncode(message.From?.Username ?? "");

                if (!string.IsNullOrEmpty(username)) username = $"{username}/";

                if (doc.Thumbnail is { } thumbnail)
                {
                    Telegram.Bot.Types.File thumbFile = await _client.GetFileAsync(thumbnail.FileId, cancellationToken: cancellationToken);
                    uid = thumbFile.FileUniqueId;

                    await TelegramPictureToBlob("thumbnails", thumbFile, uid, username, cancellationToken, doc.MimeType);
                }

                Telegram.Bot.Types.File file = await _client.GetFileAsync(doc.FileId, cancellationToken: cancellationToken);
                await TelegramPictureToBlob("pics", file, uid, username, cancellationToken, doc.MimeType);
            }
        }



        async Task HandleUpdateAsync(ITelegramBotClient botClient, Update update, CancellationToken cancellationToken)
        {
            _client = botClient;
            // Only process Message updates: https://core.telegram.org/bots/api#message
            if (update.Message is not { } message)
                return;

            var language = update.Message.From?.LanguageCode ?? "en";
            CultureInfo.CurrentCulture = CultureInfo.CurrentUICulture = CultureInfo.GetCultureInfo(language);

            _logger.LogInformation("Received a '{messageType}' update from user '{FirstName} {LastName} ({Username})'  chat '{ChatId}'.",
                message.Type,
                message.Chat.FirstName,
                message.Chat.LastName,
                message.Chat.Username,
                message.Chat.Id
                );


            if (message.Document is { } document)
            {
                _logger.LogInformation("Received a document {FileName} of size {FileSize} and type {MimeType} in chat {ChatId}",
                    document.FileName,
                    document.FileSize,
                    document.MimeType,
                    message.Chat.Id);
                try
                {
                    await SendDocumentToStorage(message, cancellationToken);

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
            if (message.Photo is { } p)
            {
                _logger.LogInformation("Received a photo '{Caption}' of size {FileSize} in chat {ChatId}",
                        message.Caption,
                        p.LastOrDefault()?.FileSize,
                        message.Chat.Id);
                try
                {
                    await SendPhotoToStorage(message, cancellationToken);

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

                _logger.LogInformation("Received a '{MessageText}' message in chat {ChatId}.", messageText, chatId);

                // Echo received message text
                Message sentMessage = await botClient.SendTextMessageAsync(
                    chatId: chatId,
                    text: $"{_localizer.GetString("EchoMessage", messageText)}",
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