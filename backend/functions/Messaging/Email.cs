using Azure;
using Azure.Communication.Email;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;

namespace functions.Messaging;

public class EmailMessagingACS(IConfiguration configuration, ILogger<EmailMessagingACS> logger) : IEmailMessaging
{
    public async Task<string> SendEmailAsync(string to, string subject, string body, string operationId)
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
        if (emailSendOperation.Value.Status == EmailSendStatus.Failed)
        {
            logger.LogError("Failed to send email to {uid} with subject {subject} and result {result}", operationId, subject, emailSendOperation.Value);
        }
        else
        {
            logger.LogInformation("Email sent to {uid} with subject {subject} and result {result}", operationId, subject, emailSendOperation.Value.Status);
        }

        return emailSendOperation.Value.Status.ToString();
    }
}