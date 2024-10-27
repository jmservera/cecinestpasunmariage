using System.Security.Cryptography;
using System.Text;

namespace functions.Identity;

public struct ChatUser
{
    public readonly string id { get { return ChatId; } }
    public string ChatId { get; set; }
    public string UserId { get; set; }
    public string Hash { get; set; }
    public string IV { get; set; }
    public string? UserDetails { get; set; }

    public string? Language { get; set; }

    public string? UserAuthId { get; set; }

    const PaddingMode padding = PaddingMode.PKCS7;
    const CipherMode cipher = CipherMode.CFB;

    public static ChatUser TimeSeal(ChatUser user, string key)
    {
        var date = DateTime.UtcNow.ToString("yyyy-MM-ddTHH:mm:ssZ");
        string userSTr = $"{user.ChatId}{user.UserId}{date}";
        var algorithm = Aes.Create();
        algorithm.GenerateIV();
        algorithm.Key = Encoding.UTF8.GetBytes(key);
        algorithm.Padding = padding;
        algorithm.Mode = cipher;

        var encryptor = algorithm.CreateEncryptor(algorithm.Key, algorithm.IV);
        using var ms = new MemoryStream();
        using var cs = new CryptoStream(ms, encryptor, CryptoStreamMode.Write);
        using var sw = new StreamWriter(cs);
        sw.Write(userSTr);
        sw.Flush();
        cs.FlushFinalBlock();
        user.Hash = Convert.ToBase64String(ms.ToArray());
        user.IV = Convert.ToBase64String(algorithm.IV);
        return user;
    }

    public readonly void CheckSealValidity(string key, int minutes = -10)
    {
        var algorithm = Aes.Create();
        algorithm.Key = Encoding.UTF8.GetBytes(key);
        algorithm.IV = Convert.FromBase64String(IV);
        algorithm.Padding = padding;
        algorithm.Mode = cipher;

        var decryptor = algorithm.CreateDecryptor(algorithm.Key, algorithm.IV);
        using var ms = new MemoryStream(Convert.FromBase64String(Hash));
        using var cs = new CryptoStream(ms, decryptor, CryptoStreamMode.Read);
        using var sr = new StreamReader(cs);
        var decrypted = sr.ReadToEnd();
        var date = decrypted[(ChatId.Length + UserId.Length)..];

        // parse a date in the format "yyyy-MM-ddTHH:mm:ssZ"
        if (!DateTime.TryParse(date, out var parsedDate))
        {
            throw new InvalidOperationException("Invalid date format");
        }

        if (parsedDate < DateTime.UtcNow.AddMinutes(minutes))
        {
            throw new InvalidOperationException("Expired");
        }
    }

    public static string GetValidKey(string key)
    {
        if (key.Length < 16)
        {
            throw new InvalidOperationException("Key is too short.");
        }
        if (key.Length < 32)
        {
            return key.PadRight(32, '0');
        }
        else
        {
            return key[..32];
        }
    }
}