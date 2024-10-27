using System.Data;
using System.Globalization;
using System.Net;
using System.Text;
using System.Web;
using functions.Identity;
using functions.Storage;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Localization;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using Telegram.Bot;
using Telegram.Bot.Exceptions;
using Telegram.Bot.Types;
using Telegram.Bot.Types.Enums;
using Telegram.Bot.Types.ReplyMarkups;

namespace functions.Messaging
{
    public class TelegramHandler(IBotTextHandler chatService, IChatHistoryManager chatHistoryManager,
                                 IConfiguration config,
                                 IChatUserMapper chatUserMapper, IStorageManager uploader,
                                 ILogger<TelegramBot> logger, IStringLocalizer<TelegramBot> localizer, ITelegramBotClient client)
    {

        /// <summary>
        /// Handles an update from the bot.
        /// </summary>
        /// <param name="botClient"> The bot client. </param>
        /// <param name="update"> The update to handle. </param>
        /// <param name="cancellationToken"> The cancellation token. </param>
        /// <returns> A task that represents the asynchronous operation. </returns>
        public async Task HandleUpdateAsync(ITelegramBotClient botClient, Update update, CancellationToken cancellationToken)
        {
            client = botClient;
            // Only process Message updates: https://core.telegram.org/bots/api#message
            if (update.Message is not { } message)
            {
                logger.LogWarning("Received an update that is not a message.");
                return;
            }
            var language = update.Message.From?.LanguageCode ?? "en";
            CultureInfo.CurrentCulture = CultureInfo.CurrentUICulture = CultureInfo.GetCultureInfo(language);

            var user = await chatUserMapper.GetUserAsync(message.Chat.Id.ToString());

            if (user is null)
            {
                logger.LogWarning("Received a message from an unknown user {ChatId} ({User}). Auth loop.", message.Chat.Id, message.Chat.Username);
                var botname = await botClient.GetMeAsync(cancellationToken);
                ChatUser chatUser = new()
                {
                    ChatId = message.Chat.Id.ToString(),
                    UserId = message.Chat.Username ?? "",
                    Language = language
                };

                var key = config.GetValue<string>("TELEGRAM_TOKEN") ?? throw new InvalidOperationException("TELEGRAM_TOKEN is not set.");
                chatUser = ChatUser.TimeSeal(chatUser, ChatUser.GetValidKey(key));
                var usr = JsonConvert.SerializeObject(chatUser);
                // encode user as a base64string
                var userEncoded = Convert.ToBase64String(Encoding.UTF8.GetBytes(usr));

                var redirect = WebUtility.UrlEncode($"/telegram/?id={userEncoded}");
                var baseUri = config.GetValue<string>("BASE_URI");
                if (string.IsNullOrEmpty(baseUri))
                {
                    throw new InvalidOperationException("BASE_URI is not set.");
                }
                UriBuilder uriBuilder = new(baseUri)
                {
                    Path = "/sso",
                    Query = $"post_login_redirect_uri={redirect}"
                };

                var button = KeyboardButton.WithWebApp(localizer.GetString("LoginButton"),
                 new WebAppInfo() { Url = uriBuilder.Uri.ToString() });
                var replyMarkup = new ReplyKeyboardMarkup(button) { ResizeKeyboard = true };

                var loginTxt = localizer.GetString("ClickLogin");

                await client.SendTextMessageAsync(message.Chat.Id, $"{loginTxt}👇\n[Login]({uriBuilder.Uri})", parseMode: ParseMode.Markdown, replyMarkup: replyMarkup, cancellationToken: cancellationToken);
            }
            else
            {

                bool somethingWasSent = false;

                logger.LogInformation("Received a '{messageType}' update from user: '{FirstName} {LastName} ({Username}) - {UserDetails}'  chatId: '{ChatId}'.",
                    message.Type,
                    message.Chat.FirstName,
                    message.Chat.LastName,
                    message.Chat.Username,
                    user.Value.UserDetails,
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
                        await client.SendTextMessageAsync(
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
                    await chatService.HandleChat(client, message, messageText, cancellationToken);
                }
            }
        }

        public Task HandlePollingErrorAsync(ITelegramBotClient botClient, Exception exception, CancellationToken cancellationToken)
        {
            client = botClient;

            var ErrorMessage = exception switch
            {
                ApiRequestException apiRequestException
                    => $"Telegram API Error:\n[{apiRequestException.ErrorCode}]\n{apiRequestException.Message}",
                _ => exception.ToString()
            };

            logger.LogError(exception, ErrorMessage);
            return Task.CompletedTask;
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
                    await client.SendTextMessageAsync(
                        chatId: message.Chat.Id,
                        text: localizer.GetString("GreetingMessage"),
                        parseMode: ParseMode.MarkdownV2,
                        cancellationToken: cancellationToken);
                    break;
                case "/help":
                    await client.SendTextMessageAsync(
                        chatId: message.Chat.Id,
                        text: localizer.GetString("Help"),
                        parseMode: ParseMode.MarkdownV2,
                        cancellationToken: cancellationToken);
                    break;
                case "/echo":
                    await client.SendTextMessageAsync(
                        chatId: message.Chat.Id,
                        text: "Echo",
                        cancellationToken: cancellationToken);
                    break;
                case "/clear":
                    await chatHistoryManager.ClearHistoryAsync(message.From?.Username ?? "", message.Chat.Id, cancellationToken);
                    await client.SendTextMessageAsync(
                        chatId: message.Chat.Id,
                        text: "History cleared",
                        cancellationToken: cancellationToken);
                    break;
                case "/logout":
                    await chatUserMapper.RemoveUserAsync(message.Chat.Id.ToString());
                    await client.SendTextMessageAsync(
                        chatId: message.Chat.Id,
                        text: "Logged out",
                        cancellationToken: cancellationToken);
                    break;
                default:
                    await client.SendTextMessageAsync(
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
            await client.DownloadFileAsync(file.FilePath, stream, cancellationToken);
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
                    await client.SendChatActionAsync(message.Chat.Id, ChatAction.UploadPhoto, null, cancellationToken);
                    await ProcessPhotosAsync(message.Photo, username, cancellationToken);
                }
                else if (message.Document != null)
                {
                    await client.SendChatActionAsync(message.Chat.Id, ChatAction.UploadDocument, null, cancellationToken);
                    await ProcessDocumentAsync(message.Document, username, cancellationToken);
                }
                else if (message.Video != null)
                {
                    await client.SendChatActionAsync(message.Chat.Id, ChatAction.UploadVideo, null, cancellationToken);
                    await ProcessVideoAsync(message.Video, username, cancellationToken);
                }
                else
                {
                    throw new InvalidOperationException("Unsupported file type");
                }

                await client.SendTextMessageAsync(
                    chatId: message.Chat.Id,
                    parseMode: ParseMode.MarkdownV2,
                    text: localizer.GetString("PictureAdded"),
                    cancellationToken: cancellationToken);
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Error sending to storage");
                await client.SendTextMessageAsync(
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
                var file = await client.GetFileAsync(video.Thumbnail.FileId, cancellationToken);
                await UploadFileToBlobAsync(GetPhotos.ThumbnailsContainerName, file, fileName, username, cancellationToken);
            }

            if (video != null)
            {
                var file = await client.GetFileAsync(video.FileId, cancellationToken);
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
                var file = await client.GetFileAsync(thumb.FileId, cancellationToken);
                await UploadFileToBlobAsync(GetPhotos.ThumbnailsContainerName, file, fileName, username, cancellationToken);
            }

            var photo = photos.LastOrDefault();
            if (photo != null)
            {
                var file = await client.GetFileAsync(photo.FileId, cancellationToken);
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

            var thumbFile = document.Thumbnail != null ? await client.GetFileAsync(document.Thumbnail.FileId, cancellationToken) : null;
            if (thumbFile != null)
            {
                await UploadFileToBlobAsync(GetPhotos.ThumbnailsContainerName, thumbFile, fileName, username, cancellationToken, document.MimeType);
            }

            var file = await client.GetFileAsync(document.FileId, cancellationToken);
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
    }
}