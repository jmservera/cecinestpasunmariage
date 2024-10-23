namespace functions.Identity;

public interface IChatUserMapper
{
    Task<ChatUser?> GetUserAsync(string chatId);
    Task SaveUserAsync(ChatUser user);
}

public struct ChatUser
{
    public string ChatId { get; set; }
    public string UserId { get; set; }
}