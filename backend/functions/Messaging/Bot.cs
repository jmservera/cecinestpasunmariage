using System.Data;
using System.Globalization;
using System.Text;
using System.Text.Json;
using System.Web;
using functions.Storage;
using Microsoft.Azure.Functions.Worker.Http;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Localization;
using Microsoft.Extensions.Logging;
using Microsoft.SemanticKernel;
using Microsoft.SemanticKernel.ChatCompletion;
using Telegram.Bot;
using Telegram.Bot.Exceptions;
using Telegram.Bot.Polling;
using Telegram.Bot.Types;
using Telegram.Bot.Types.Enums;

namespace functions.Messaging
{
    /// <summary>
    /// A bot that can receive and send messages.
    /// </summary>
    /// <param name="logger"> The logger instance. </param>
    /// <param name="localizer"> The localizer instance. </param>
    /// <param name="uploader"> The storage manager instance. </param>
    /// <param name="configuration"> The configuration instance. </param>
    public class Bot(ILogger<Bot> logger, IStringLocalizer<Bot> localizer, IStorageManager uploader, IConfiguration configuration, IChatCompletionService chatCompletionService) : IDisposable
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

            var update = JsonSerializer.Deserialize<Telegram.Bot.Types.Update>(request);
            if (update is null)
            {
                logger.LogError("Failed to deserialize the update.");
                return;
            }

            await HandleUpdateAsync(_client, update, cancellationToken);
        }

        /// <summary>
        /// Handles a command message.
        /// </summary>
        /// <param name="message"> The message to handle. </param>
        /// <param name="cancellationToken"> The cancellation token. </param>
        /// <returns> A task that represents the asynchronous operation. </returns>
        async Task HandleCommand(Message message, CancellationToken cancellationToken)
        {

            var command = message.Text?.Split(" ")[0];
            switch (command)
            {
                case "/start":
                    await _client.SendTextMessageAsync(
                        chatId: message.Chat.Id,
                        text: localizer.GetString("GreetingMessage"),
                        parseMode: ParseMode.MarkdownV2,
                        cancellationToken: cancellationToken);
                    break;
                case "/help":
                    await _client.SendTextMessageAsync(
                        chatId: message.Chat.Id,
                        text: localizer.GetString("Help"),
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

        /// <summary>
        /// Uploads a file to a blob.
        /// </summary>
        /// <param name="containerName"> The name of the container. </param>
        /// <param name="file"> The file to upload. </param>
        /// <param name="fileName"> The name of the file. </param>
        /// <param name="username"> The username of the user. </param>
        /// <param name="cancellationToken"> The cancellation token. </param>
        /// <param name="mimeType"> The mime type of the file. </param>
        /// <returns> A task that represents the asynchronous operation. </returns>
        private async Task UploadFileToBlobAsync(string containerName, Telegram.Bot.Types.File file, string fileName, string username, CancellationToken cancellationToken, string mimeType = "image/jpeg")
        {
            if (file?.FilePath == null) return;

            await using var stream = new MemoryStream();
            await _client.DownloadFileAsync(file.FilePath, stream, cancellationToken);
            stream.Seek(0, SeekOrigin.Begin);
            string fileNameExt = $"{fileName}{Path.GetExtension(file.FilePath)}";
            await uploader.UploadAsync(username, fileNameExt, containerName, stream, mimeType, file.FilePath, cancellationToken);
        }

        /// <summary>
        /// Processes the attachments of a message.
        /// </summary>
        /// <param name="message"> The message to process. </param>
        /// <param name="cancellationToken"> The cancellation token. </param>
        /// <returns> A task that represents the asynchronous operation. </returns>
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
                else if (message.Video != null)
                {
                    await ProcessVideoAsync(message.Video, username, cancellationToken);
                }
                else
                {
                    throw new InvalidOperationException("Unsupported file type");
                }

                await _client.SendTextMessageAsync(
                    chatId: message.Chat.Id,
                    parseMode: ParseMode.MarkdownV2,
                    text: localizer.GetString("PictureAdded"),
                    cancellationToken: cancellationToken);
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Error sending to storage");
                await _client.SendTextMessageAsync(
                    chatId: message.Chat.Id,
                    parseMode: ParseMode.MarkdownV2,
                    text: localizer.GetString("PictureNotAdded"),
                    cancellationToken: cancellationToken);
            }
        }

        /// <summary>
        /// Processes a video and uploads it to a blob.
        /// </summary>
        /// <param name="video"> The video to process. </param>
        /// <param name="username"> The username of the user. </param>
        /// <param name="cancellationToken"> The cancellation token. </param>
        private async Task ProcessVideoAsync(Video video, string username, CancellationToken cancellationToken)
        {
            if (video.MimeType == null)
            {
                throw new InvalidOperationException("Document has no mime type");
            }

            ValidateMimeType(video.MimeType);

            var fileName = uploader.GenerateUniqueName();
            if (video.Thumbnail != null)
            {
                var file = await _client.GetFileAsync(video.Thumbnail.FileId, cancellationToken);
                await UploadFileToBlobAsync(GetPhotos.ThumbnailsContainerName, file, fileName, username, cancellationToken);
            }

            if (video != null)
            {
                var file = await _client.GetFileAsync(video.FileId, cancellationToken);
                await UploadFileToBlobAsync(GetPhotos.PicsContainerName, file, fileName, username, cancellationToken, video.MimeType);
            }

        }

        /// <summary>
        /// Processes photos and uploads them to a blob.
        /// </summary>
        /// <param name="photos"> The photos to process. </param>
        /// <param name="username"> The username of the user. </param>
        /// <param name="cancellationToken"> The cancellation token. </param>
        /// <returns> A task that represents the asynchronous operation. </returns>
        private async Task ProcessPhotosAsync(IEnumerable<PhotoSize> photos, string username, CancellationToken cancellationToken)
        {
            var thumb = photos.Skip(1).FirstOrDefault();
            var fileName = uploader.GenerateUniqueName();
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

        /// <summary>
        /// Processes a document and uploads it to a blob.
        /// </summary>
        /// <param name="document"> The document to process. </param>
        /// <param name="username"> The username of the user. </param>
        /// <param name="cancellationToken"> The cancellation token. </param>
        /// <returns> A task that represents the asynchronous operation. </returns>
        private async Task ProcessDocumentAsync(Document document, string username, CancellationToken cancellationToken)
        {
            if (document.MimeType == null)
            {
                throw new InvalidOperationException("Document has no mime type");
            }

            ValidateMimeType(document.MimeType);

            var fileName = uploader.GenerateUniqueName();

            var thumbFile = document.Thumbnail != null ? await _client.GetFileAsync(document.Thumbnail.FileId, cancellationToken) : null;
            if (thumbFile != null)
            {
                await UploadFileToBlobAsync(GetPhotos.ThumbnailsContainerName, thumbFile, fileName, username, cancellationToken, document.MimeType);
            }

            var file = await _client.GetFileAsync(document.FileId, cancellationToken);
            await UploadFileToBlobAsync(GetPhotos.PicsContainerName, file, fileName, username, cancellationToken, document.MimeType);
        }

        static readonly HashSet<string> validMimeTypes = ["image/jpeg", "image/png", "image/gif", "video/mp4"];

        /// <summary>
        /// Validates that a mime type is in the list of valid processors.
        /// Usually one of these values: ["image/jpeg", "image/png", "image/gif", "video/mp4"]
        /// </summary>
        /// <param name="mimeType"> The mime type to validate. </param>
        /// <exception cref="InvalidOperationException"> Thrown when the mime type is invalid. </exception>
        private static void ValidateMimeType(string mimeType)
        {
            if (!validMimeTypes.Contains(mimeType))
            {
                throw new InvalidOperationException($"Invalid file type {mimeType}");
            }
        }

        /// <summary>
        /// Handles an update from the bot.
        /// </summary>
        /// <param name="botClient"> The bot client. </param>
        /// <param name="update"> The update to handle. </param>
        /// <param name="cancellationToken"> The cancellation token. </param>
        /// <returns> A task that represents the asynchronous operation. </returns>
        async Task HandleUpdateAsync(ITelegramBotClient botClient, Update update, CancellationToken cancellationToken)
        {
            // TODO: use a strategy pattern to handle different types of updates

            _client = botClient;
            // Only process Message updates: https://core.telegram.org/bots/api#message
            if (update.Message is not { } message)
                return;

            bool somethingWasSent = false;
            var language = update.Message.From?.LanguageCode ?? "en";
            CultureInfo.CurrentCulture = CultureInfo.CurrentUICulture = CultureInfo.GetCultureInfo(language);

            logger.LogInformation("Received a '{messageType}' update from user '{FirstName} {LastName} ({Username})'  chat '{ChatId}'.",
                message.Type,
                message.Chat.FirstName,
                message.Chat.LastName,
                message.Chat.Username,
                message.Chat.Id
                );

            if (message.Document is { } document)
            {
                logger.LogInformation("Received a document {FileName} of size {FileSize} and type {MimeType} in chat {ChatId}",
                    document.FileName,
                    document.FileSize,
                    document.MimeType,
                    message.Chat.Id);
                await ProcessMessageAttachmentsAsync(message, cancellationToken);
                somethingWasSent = true;
            }
            if (message.Photo is { } p)
            {
                logger.LogInformation("Received a photo '{Caption}' of size {FileSize} in chat {ChatId}",
                        message.Caption,
                        p.LastOrDefault()?.FileSize,
                        message.Chat.Id);
                await ProcessMessageAttachmentsAsync(message, cancellationToken);
                somethingWasSent = true;
            }
            if (message.Video is { } v)
            {
                logger.LogInformation("Received a video '{Caption}' of size {FileSize} in chat {ChatId}",
                        message.Caption,
                        v.FileSize,
                        message.Chat.Id);
                await ProcessMessageAttachmentsAsync(message, cancellationToken);
                somethingWasSent = true;
            }

            // Only process text messages
            if (message.Text is not { } messageText)
            {
                if (!somethingWasSent)
                {
                    logger.LogError("Didn't have a handler for a '{MessageType}' message in chat {ChatId}.", message.Type, message.Chat.Id);
                    await _client.SendTextMessageAsync(
                        chatId: message.Chat.Id,
                        parseMode: ParseMode.MarkdownV2,
                        text: localizer.GetString("InvalidFile"),
                        cancellationToken: cancellationToken);
                }
                return;
            }

            if (messageText.StartsWith('/'))
            {
                await HandleCommand(message, cancellationToken);
            }
            else
            {
                string username = HttpUtility.HtmlEncode(message.From?.Username ?? "");
                var chatId = message.Chat.Id;

                logger.LogInformation("Received a '{MessageText}' message in chat {ChatId}.", messageText, chatId);

                var history = await GetHistoryAsync(username, chatId);
#pragma warning disable SKEXP0001 // Type is for evaluation purposes only and is subject to change or removal in future updates. Suppress this diagnostic to proceed.
                ChatMessageContent messageContent = new(AuthorRole.User, messageText)
                {
                    AuthorName = message.From?.Username
                };
#pragma warning restore SKEXP0001 // Type is for evaluation purposes only and is subject to change or removal in future updates. Suppress this diagnostic to proceed.

                history.Add(messageContent);
                try
                {
                    var response = await chatCompletionService.GetChatMessageContentAsync(history, cancellationToken: cancellationToken);
                    // Echo received message text
                    Message sentMessage = await _client.SendTextMessageAsync(
                        chatId: chatId,
                        text: response.Content ?? "I'm sorry, I can't answer that.",
                        cancellationToken: cancellationToken);

                    history.AddAssistantMessage(response.Content ?? "I'm sorry, I can't answer that.");
                    await SaveHistoryAsync(username, chatId, history, cancellationToken);
                }
                catch (Exception ex)
                {
                    logger.LogError(ex, "Error getting chat message content from AOAI.");
                    await _client.SendTextMessageAsync(
                        chatId: chatId,
                        text: "I'm sorry, there was an error, try later.",
                        cancellationToken: cancellationToken);
                }
            }
        }

        const string ChatHistoryContainerName = "chat-history";

        private async Task SaveHistoryAsync(string username, long chatId, ChatHistory history, CancellationToken cancellationToken)
        {
            //remove system message
            history.RemoveRange(0, 1);

            //trim history to 60 messages
            if (history.Count > 60)
            {
                history.RemoveRange(1, 60 - history.Count);
            }
            var value = JsonSerializer.Serialize(history);
            using var stream = new MemoryStream(Encoding.UTF8.GetBytes(value));
            await uploader.UploadAsync(username, $"{chatId}.json", ChatHistoryContainerName, stream, "application/json", null, cancellationToken);
        }

        private async Task<ChatHistory> GetHistoryAsync(string username, long chatId)
        {
            var metaPrompt = localizer.GetString("MetaPrompt") + $" the username is {username}";
            try
            {
                using var stream = await uploader.DownloadAsync(username, $"{chatId}.json", ChatHistoryContainerName);
                using StreamReader reader = new(stream);
                var history = JsonSerializer.Deserialize<ChatHistory>(reader.ReadToEnd());
                if (history is not null)
                {
                    // add system message
                    var historywithsystem = new ChatHistory(metaPrompt);
                    historywithsystem.AddRange(history);
                    return historywithsystem;
                }
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Error getting chat history");
            }
            ChatHistory newHistory = new(metaPrompt);
            return newHistory;
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

            logger.LogError(exception, ErrorMessage);
            return Task.CompletedTask;
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
                AllowedUpdates = Array.Empty<UpdateType>() // receive all update types except ChatMember related updates
            };

            _client.StartReceiving(
                updateHandler: HandleUpdateAsync,
                pollingErrorHandler: HandlePollingErrorAsync,
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

        ~Bot()
        {
            Dispose(disposing: false);
        }
    }
}