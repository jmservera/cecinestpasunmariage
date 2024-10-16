using Azure;
using Azure.Communication.Email;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using System.Security.Cryptography;

namespace functions.Messaging;

public class EmailMessagingACS(IConfiguration configuration, ILogger<EmailMessagingACS> logger) : IEmailMessaging
{
    public async Task<string> SendEmailAsync(string to, string subject, string body)
    {
        string connectionString = configuration.GetValue<string>("COMMUNICATION_SERVICES_CONNECTION_STRING") ?? throw new InvalidOperationException("COMMUNICATION_SERVICES_CONNECTION_STRING is not set.");
        string sender = configuration.GetValue<string>("COMMUNICATION_SERVICES_SENDER") ?? throw new InvalidOperationException("COMMUNICATION_SERVICES_SENDER is not set.");

        var emailClient = new EmailClient(connectionString);

        var emailMessage = new EmailMessage(
        senderAddress: sender,
        content: new EmailContent(subject)
        {
            PlainText = body,
            Html = $"<html><body>{body.Replace("\n", "<br/>")}</body></html>"
        },
        recipients: new EmailRecipients([new EmailAddress(to)]));
        var emailSendOperation = await emailClient.SendAsync(WaitUntil.Completed, emailMessage);
        string hashedEmail = HashEmail(to);
        if (emailSendOperation.Value.Status == EmailSendStatus.Failed)
        {
            logger.LogError("Failed to send email to {hashedEmail} with subject {subject} and result {result}", hashedEmail, subject, emailSendOperation.Value);
        }
        else
        {
            logger.LogInformation("Email sent to {hashedEmail} with subject {subject} and result {result}", hashedEmail, subject, emailSendOperation.Value.Status);
        }

        return emailSendOperation.Value.Status.ToString();
    }

    private string HashEmail(string email)
    {
        using (var sha256 = System.Security.Cryptography.SHA256.Create())
        {
            var hashedBytes = sha256.ComputeHash(System.Text.Encoding.UTF8.GetBytes(email));
            return BitConverter.ToString(hashedBytes).Replace("-", "").ToLower();
        }
    }
}