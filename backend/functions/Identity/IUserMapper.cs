using System.Security.Cryptography;
using System.Text;
using System.Text.Unicode;
using Microsoft.Identity.Client;

namespace functions.Identity;

public interface IChatUserMapper
{
    Task<ChatUser?> GetUserAsync(string chatId);
    Task RemoveUserAsync(string chatId);
    Task SaveUserAsync(ChatUser user);
}