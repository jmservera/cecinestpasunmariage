using System;
using System.Collections.Generic;
using System.Net;
using System.Security.Claims;
using System.Security.Cryptography;
using System.Text.Json;
using Azure;
using Azure.AI.OpenAI;
using Azure.Communication.Email;
using functions.Audit;
using functions.Claims;
using functions.Messaging;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Azure.Functions.Worker.Http;
using Microsoft.Extensions.Azure;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;

namespace functions
{
    public class SendEmail(ILogger<SendEmail> logger, IEmailMessaging emailMessaging, IAuditService<SendEmail> auditService)
    {

        [Function(nameof(SendEmail))]
        public async Task<HttpResponseData> Run([HttpTrigger(AuthorizationLevel.Function, "post")] HttpRequestData req)
        {
            logger.LogInformation("Sending mail");
            var principal = ClaimsPrincipalParser.Parse(req);
            string requestBody = await new StreamReader(req.Body).ReadToEndAsync();
            var data = JsonSerializer.Deserialize<EmailRequest>(requestBody);

            if (data == null || string.IsNullOrEmpty(data.title) || string.IsNullOrEmpty(data.message) || data.recipients == null || data.recipients.Length == 0)
            {
                var badRequestResponse = req.CreateResponse(HttpStatusCode.BadRequest);
                await badRequestResponse.WriteStringAsync("Invalid request payload.");
                return badRequestResponse;
            }

            string response = "";

            foreach (var recipient in data.recipients)
            {
                var emailSendOperation = await emailMessaging.SendEmailAsync(recipient, data.title, data.message);
                if (emailSendOperation != "Succeeded")
                {
                    response += $"Failed to send email to {recipient}: {emailSendOperation}." + Environment.NewLine;
                }
                auditService.Audit(principal.Identity?.Name ?? "system", "email", $"Sent email to {recipient} with result {emailSendOperation}");
            }

            if (!string.IsNullOrEmpty(response))
            {
                var reqResponse = req.CreateResponse(HttpStatusCode.InternalServerError);
                await reqResponse.WriteStringAsync(response);
                return reqResponse;
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