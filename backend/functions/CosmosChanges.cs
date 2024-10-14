using System.Text.Json;
using functions.Audit;
using functions.Messaging;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;

namespace functions;

public class CosmosChanges(ILogger<CosmosChanges> logger, IEmailMessaging emailMessaging, IAuditService<CosmosChanges> auditService, IConfiguration configuration)
{

    readonly string logAdmin = configuration.GetValue<string>("DEFAULT_ADMIN_EMAIL") ?? throw new InvalidOperationException("DEFAULT_ADMIN_EMAIL is not set.");
    readonly string[] hiddenFields = ["id", "createdAt", "updatedAt"];

    [Function(nameof(CosmosChanges))]
    public async Task Run([CosmosDBTrigger(
    databaseName: "%DATABASE_NAME%",
    containerName:"%DATABASE_CONTAINER_NAME%",
    Connection = "DATABASE_CONNECTION_STRING",
    LeaseContainerName = "leases",
    CreateLeaseContainerIfNotExists = true)] IReadOnlyList<JsonElement> items,
        FunctionContext context)
    {
        logger.LogInformation("CosmosDB trigger function executed, items count: {Count}", items.Count);
        if (items is not null && items.Any())
        {
            foreach (var doc in items)
            {
                var createdAt = doc.GetProperty("createdAt").GetDateTime();
                var updatedAt = doc.GetProperty("updatedAt").GetDateTime();
                var email = doc.GetProperty("email").GetString();
                var name = doc.GetProperty("name").GetString();

                var newRecord = (createdAt == updatedAt);
                var operation = newRecord ? "created" : "updated";

                var message = $"Hello {name}!\nYou recently {operation} a record in our system.\n\n";
                var adminMessage = $"Hello admin!\nA record was {operation} in our system.\n\n";
                foreach (var field in doc.EnumerateObject())
                {
                    if (!hiddenFields.Contains(field.Name) && !field.Name.StartsWith('_'))
                    {
                        message += $"{field.Name}: {field.Value}\n";
                    }
                    adminMessage += $"{field.Name}: {field.Value}\n";
                }

                message += "\nThank you for registering!\n We look forward to having lots of fun together.\n You can review your changes at <a href=\"https://cecinestpasunmariage.org/registro/\">cecinestpasunmariage.org</a>";

                var adminResult = await emailMessaging.SendEmailAsync(logAdmin, "Important user update at cecinestpasunmariage.org", adminMessage);
                auditService.Audit("system", "update sent to admin", adminResult);

                if (!string.IsNullOrEmpty(email))
                {
                    var userResult = await emailMessaging.SendEmailAsync(email, $"Important update, you {operation} your user at cecinestpasunmariage.org", message);
                    auditService.Audit("system", "update sent to user", userResult);
                }
            }
        }
    }
}