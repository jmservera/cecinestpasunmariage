using System.Data;
using System.Net;
using System.Text;
using System.Web;
using Microsoft.Extensions.Localization;
using Microsoft.Extensions.Logging;
using Microsoft.SemanticKernel;
using Microsoft.SemanticKernel.ChatCompletion;
using Telegram.Bot;
using Telegram.Bot.Exceptions;
using Telegram.Bot.Types;
using Telegram.Bot.Types.Enums;

namespace functions.Messaging;

/// <summary>
/// Handles chat messages using the Azure OpenAI chat completion service.
/// </summary>
/// <param name="chatCompletionService">The chat completion service to use.</param>
/// <param name="chatHistoryManager">The chat history manager to use.</param>
/// <param name="logger">The logger instance for logging errors and information.</param>
/// <param name="localizer">The localizer for localizing strings.</param>
/// <remarks>
/// This class uses the Azure OpenAI chat completion service to generate responses to chat messages.
/// </remarks>
/// <seealso cref="IBotTextHandler" />
public class AzureOpenAIChatHandler(IChatCompletionService chatCompletionService, IChatHistoryManager chatHistoryManager, ILogger<AzureOpenAIChatHandler> logger, IStringLocalizer<TelegramBot> localizer) : IBotTextHandler
{
    const string validMarkdown = @"*bold text*
_italic text_
[inline URL](http://www.example.com/)
[inline mention of a user](tg://user?id=123456789)
`inline fixed-width code`
```
pre-formatted fixed-width code block
```
```python
pre-formatted fixed-width code block written in the Python programming language
```";

    /// <summary>
    /// Handles an incoming chat message.
    /// </summary>
    /// <param name="client">The Telegram bot client instance.</param>
    /// <param name="message">The message received from the chat.</param>
    /// <param name="messageText">The text content of the message.</param>
    /// <param name="cancellationToken">A token to monitor for cancellation requests.</param>
    /// <returns>A task that represents the asynchronous operation.</returns>
    public async Task HandleChat(ITelegramBotClient client, Message message, string messageText, CancellationToken cancellationToken)
    {
        string username = HttpUtility.HtmlEncode(message.From?.Username ?? "");
        string fullName = HttpUtility.HtmlEncode($"{message.From?.FirstName} {message.From?.LastName}");
        var chatId = message.Chat.Id;

        logger.LogInformation("Received a '{MessageText}' message in chat {ChatId}.", messageText, chatId);

        ChatHistory history = await GetHistoryWithSystemPrompt(username, fullName, chatId);

#pragma warning disable SKEXP0001 // Type is for evaluation purposes only and is subject to change or removal in future updates. Suppress this diagnostic to proceed.
        ChatMessageContent messageContent = new(AuthorRole.User, messageText)
        {
            AuthorName = message.From?.Username
        };
#pragma warning restore SKEXP0001 // Type is for evaluation purposes only and is subject to change or removal in future updates. Suppress this diagnostic to proceed.

        history.Add(messageContent);
        try
        {
            await client.SendChatActionAsync(message.Chat.Id, ChatAction.Typing, null, cancellationToken);
            var response = await chatCompletionService.GetChatMessageContentAsync(history, cancellationToken: cancellationToken);
            var msg = response.Content ?? "I'm sorry, I can't answer that.";
            try
            {
                Message sentMessage = await client.SendTextMessageAsync(
                    chatId: chatId,
                    parseMode: ParseMode.Markdown, // this is the most permisive mode v2 is very restrictive
                    text: msg,
                    cancellationToken: cancellationToken);
            }
            catch (ApiRequestException ex)
            {
                logger.LogError(ex, "Error sending message to chat. Sending it again without format: {msg}", msg);
                Message sentMessage = await client.SendTextMessageAsync(
                    chatId: chatId,
                    text: msg,
                    cancellationToken: cancellationToken);
            }

            history.AddAssistantMessage(msg);
            await SaveHistoryWithoutSystemPromptAsync(username, chatId, history, cancellationToken);
        }
        catch (HttpOperationException operationException)
        {
            if (operationException.StatusCode == HttpStatusCode.BadRequest)
            {
                logger.LogError(operationException, "Error getting chat message content from AOAI.");
                await client.SendTextMessageAsync(
                    chatId: chatId,
                    text: $"I'm sorry, there was an error: {operationException.Message}",
                    cancellationToken: cancellationToken);
            }
            else
                throw;
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error getting chat message content from AOAI.");
            await client.SendTextMessageAsync(
                chatId: chatId,
                text: "I'm sorry, there was an error, try later.",
                cancellationToken: cancellationToken);
        }
    }

    private async Task<ChatHistory> GetHistoryWithSystemPrompt(string username, string fullName, long chatId)
    {
        var history = await chatHistoryManager.GetHistoryAsync(username, fullName, chatId);

        var metaPrompt = localizer.GetString("MetaPrompt") +
        $"\nThe person you are talking with has username {username}, and their full name is {fullName}. When you start a new conversation with them, you can use this information to greet them.\n" +
        $"Format the answer as markdown, the only supported markdown is:\n{validMarkdown}\nDo not use any other markdown tags, if you do, escape the tags with a backslash.";
        history.Insert(0, new ChatMessageContent(AuthorRole.System, metaPrompt));
        return history;
    }
    private async Task SaveHistoryWithoutSystemPromptAsync(string username, long chatId, ChatHistory history, CancellationToken cancellationToken)
    {
        //remove system message
        if (history.Count > 0) history.RemoveRange(0, 1);

        await chatHistoryManager.SaveHistoryAsync(username, chatId, history, cancellationToken);
    }
}

