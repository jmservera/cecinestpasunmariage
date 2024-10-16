namespace functions.Messaging;

public interface IEmailMessaging
{
    Task<string> SendEmailAsync(string to, string subject, string body, string operationId);
}