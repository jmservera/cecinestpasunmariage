namespace functions.Identity;

public interface IChatUserMapper
{
    Task<ChatUser?> GetUserAsync(string chatId);
    Task RemoveUserAsync(string chatId);
    Task SaveUserAsync(ChatUser user);
}

public struct ChatUser
{
    public readonly string id { get { return ChatId; } }
    public string ChatId { get; set; }
    public string UserId { get; set; }
    public string? BotName { get; set; }
}