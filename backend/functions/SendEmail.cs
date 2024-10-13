using System;
using System.Collections.Generic;
using System.Net;
using System.Text.Json;
using Azure;
using Azure.AI.OpenAI;
using Azure.Communication.Email;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Azure.Functions.Worker.Http;
using Microsoft.Extensions.Azure;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;

namespace functions
{
    public class SendEmail(ILogger<SendEmail> logger, IConfiguration configuration)
    {

        private readonly ILogger<SendEmail> _logger = logger;
        private readonly IConfiguration _configuration = configuration;

        [Function(nameof(SendEmail))]
        public async Task<HttpResponseData> Run([HttpTrigger(AuthorizationLevel.Anonymous, "post")] HttpRequestData req)
        {
            string requestBody = await new StreamReader(req.Body).ReadToEndAsync();
            var data = JsonSerializer.Deserialize<EmailRequest>(requestBody);

            if (data == null || string.IsNullOrEmpty(data.title) || string.IsNullOrEmpty(data.message) || data.recipients == null || data.recipients.Length == 0)
            {
                var badRequestResponse = req.CreateResponse(HttpStatusCode.BadRequest);
                await badRequestResponse.WriteStringAsync("Invalid request payload.");
                return badRequestResponse;
            }

            string connectionString = _configuration.GetValue<string>("COMMUNICATION_SERVICES_CONNECTION_STRING") ?? throw new InvalidOperationException("COMMUNICATION_SERVICES_CONNECTION_STRING is not set.");
            string sender = _configuration.GetValue<string>("COMMUNICATION_SERVICES_SENDER") ?? throw new InvalidOperationException("COMMUNICATION_SERVICES_SENDER is not set.");

            var emailClient = new EmailClient(connectionString);

            foreach (var recipient in data.recipients)
            {
                var emailMessage = new EmailMessage(
                senderAddress: sender,
                content: new EmailContent(data.title)
                {
                    PlainText = data.message,
                    Html = $"<html><body>{data.message.Replace("\n", "<br/>")}</body></html>"
                },
                recipients: new EmailRecipients([new EmailAddress(recipient)]));
                var emailSendOperation = await emailClient.SendAsync(WaitUntil.Started, emailMessage);
                //todo: handle the emailSendOperation
            }

            return req.CreateResponse(HttpStatusCode.OK);
        }

        private class EmailRequest
        {
            public required string[] recipients { get; set; }
            public required string title { get; set; }
            public required string message { get; set; }
            public string lang { get; set; } = "en";
        }
    }
}