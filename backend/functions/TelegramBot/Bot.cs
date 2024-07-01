using System.Data;
using System.Globalization;
using System.Web;
using Azure.Storage.Blobs;
using Azure.Storage.Blobs.Models;
using functions.Storage;
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

        private readonly IFileUploader _uploader;

        private CancellationTokenSource? _cts;



        public Bot(ILoggerFactory loggerFactory, IStringLocalizer<Bot> localizer, IFileUploader uploader)
        {
            _localizer = localizer;
            _logger = loggerFactory.CreateLogger<Bot>();
            _uploader = uploader;
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

        private async Task UploadFileToBlobAsync(string containerName, Telegram.Bot.Types.File file, string fileName, string username, CancellationToken cancellationToken, string mimeType = "image/jpeg")
        {
            if (file?.FilePath == null) return;

            await using var stream = new MemoryStream();
            await _client.DownloadFileAsync(file.FilePath, stream, cancellationToken);
            stream.Seek(0, SeekOrigin.Begin);
            string fileNameExt = $"{fileName}{Path.GetExtension(file.FilePath)}";
            await _uploader.UploadAsync(username, fileNameExt, containerName, stream, mimeType, file.FilePath, cancellationToken);
        }

        private async Task ProcessMessageAttachmentsAsync(Message message, CancellationToken cancellationToken)
        {
            try
            {
                string username = HttpUtility.HtmlEncode(message.From?.Username ?? "");

                if (message.Photo != null)
                {
                    await ProcessPhotosAsync(message.Photo, username, cancellationToken);
                }
                else if (message.Document != null)
                {
                    await ProcessDocumentAsync(message.Document, username, cancellationToken);
                }

                await _client.SendTextMessageAsync(
                    chatId: message.Chat.Id,
                    parseMode: ParseMode.MarkdownV2,
                    text: _localizer.GetString("PictureAdded"),
                    cancellationToken: cancellationToken);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error sending to storage");
                await _client.SendTextMessageAsync(
                    chatId: message.Chat.Id,
                    parseMode: ParseMode.MarkdownV2,
                    text: _localizer.GetString("PictureNotAdded"),
                    cancellationToken: cancellationToken);
            }
        }

        private async Task ProcessPhotosAsync(IEnumerable<PhotoSize> photos, string username, CancellationToken cancellationToken)
        {
            var thumb = photos.Skip(1).FirstOrDefault();
            var fileName = _uploader.GenerateUniqueName();
            if (thumb != null)
            {
                var file = await _client.GetFileAsync(thumb.FileId, cancellationToken);
                await UploadFileToBlobAsync(GetPhotos.ThumbnailsContainerName, file, fileName, username, cancellationToken);
            }

            var photo = photos.LastOrDefault();
            if (photo != null)
            {
                var file = await _client.GetFileAsync(photo.FileId, cancellationToken);
                await UploadFileToBlobAsync(GetPhotos.PicsContainerName, file, fileName, username, cancellationToken);
            }
        }

        private async Task ProcessDocumentAsync(Document document, string username, CancellationToken cancellationToken)
        {
            if (document.MimeType == null)
            {
                throw new InvalidOperationException("Document has no mime type");
            }

            ValidateMimeType(document.MimeType);

            var fileName = _uploader.GenerateUniqueName();

            var thumbFile = document.Thumbnail != null ? await _client.GetFileAsync(document.Thumbnail.FileId, cancellationToken) : null;
            if (thumbFile != null)
            {
                await UploadFileToBlobAsync(GetPhotos.ThumbnailsContainerName, thumbFile, fileName, username, cancellationToken, document.MimeType);
            }

            var file = await _client.GetFileAsync(document.FileId, cancellationToken);
            await UploadFileToBlobAsync(GetPhotos.PicsContainerName, file, fileName, username, cancellationToken, document.MimeType);
        }

        private static void ValidateMimeType(string mimeType)
        {
            var validMimeTypes = new HashSet<string> { "image/jpeg", "image/png", "image/gif" };
            if (!validMimeTypes.Contains(mimeType))
            {
                throw new InvalidOperationException($"Invalid file type {mimeType}");
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
                await ProcessMessageAttachmentsAsync(message, cancellationToken);
            }
            if (message.Photo is { } p)
            {
                _logger.LogInformation("Received a photo '{Caption}' of size {FileSize} in chat {ChatId}",
                        message.Caption,
                        p.LastOrDefault()?.FileSize,
                        message.Chat.Id);
                await ProcessMessageAttachmentsAsync(message, cancellationToken);
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
                Message sentMessage = await _client.SendTextMessageAsync(
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