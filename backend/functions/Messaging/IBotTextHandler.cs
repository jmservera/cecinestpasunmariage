using Telegram.Bot;
using Telegram.Bot.Types;

namespace functions.Messaging;

/// <summary>
/// Defines a handler for processing text messages received by a bot.
/// </summary>
public interface IBotTextHandler
{
    /// <summary>
    /// Handles an incoming chat message.
    /// </summary>
    /// <param name="client">The Telegram bot client instance.</param>
    /// <param name="message">The message received from the chat.</param>
    /// <param name="messageText">The text content of the message.</param>
    /// <param name="cancellationToken">A token to monitor for cancellation requests.</param>
    /// <returns>A task that represents the asynchronous operation.</returns>
    Task HandleChat(ITelegramBotClient client, Message message, string messageText, CancellationToken cancellationToken);
}
