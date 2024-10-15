using Microsoft.Azure.Cosmos;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;

namespace functions.Audit;

/// <summary>
/// Service for auditing operations in CosmosDB.
/// </summary>
public class AuditInCosmosService : IAuditService
{
    private readonly IConfiguration _configuration;
    private readonly CosmosClient _cosmosClient;
    private readonly Container _container;
    private readonly ILogger _logger;

    private readonly string _className;

    private static bool _initialized = false;

    /// <summary>
    /// Initializes a new instance of the <see cref="AuditInCosmosService"/> class.
    /// </summary>
    /// <param name="configuration"> The configuration instance. </param>
    /// <param name="logger"> The logger instance. </param>
    /// <param name="className"> The name of the class being audited. </param>
    /// <exception cref="InvalidOperationException"> Thrown when the DATABASE_CONNECTION_STRING is not set. </exception>
    public AuditInCosmosService(IConfiguration configuration, ILogger logger, string className)
    {
        _configuration = configuration;
        var cosmosConnectionString = _configuration.GetValue<string>("DATABASE_CONNECTION_STRING") ?? throw new InvalidOperationException("DATABASE_CONNECTION_STRING is not set.");

        _cosmosClient = new CosmosClient(cosmosConnectionString);
        _container = _cosmosClient.GetDatabase("Audits").GetContainer("AuditLogs");
        _className = className;
        _logger = logger;
    }

    /// <summary>
    /// Initializes the audit service.
    /// </summary>
    /// <returns> A task that represents the asynchronous operation. </returns>
    public async Task InitializeAsync()
    {
        try
        {
            await _cosmosClient.CreateDatabaseIfNotExistsAsync("Audits");
            await _cosmosClient.GetDatabase("Audits").CreateContainerIfNotExistsAsync("AuditLogs", "/user");
            _initialized = true;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error initializing CosmosDB");
            throw;
        }
    }

    /// <summary>
    /// Audits an operation performed by a user.
    /// </summary>
    /// <param name="user"> The user who performed the operation. </param>
    /// <param name="operation"> The operation that was performed. </param>
    /// <param name="result"> The result of the operation. </param>
    public void Audit(string user, string operation, string result)
    {
        //TODO: use a queue instead of fire and forget...
        _ = AuditAsync(user, operation, result);
    }

    /// <summary>
    /// Audits an operation performed by a user.
    /// </summary>
    /// <param name="user"> The user who performed the operation. </param>
    /// <param name="operation"> The operation that was performed. </param>
    /// <param name="result"> The result of the operation. </param>
    /// <returns> A task that represents the asynchronous operation. </returns>
    private async Task AuditAsync(string user, string operation, string result)
    {
        try
        {
            if (!_initialized)
            {
                await InitializeAsync();
            }
            _logger.LogInformation("Auditing: {user}, {classname}, {operation}, {result}", user, _className, operation, result);

            var auditLog = new
            {
                id = Guid.NewGuid().ToString(),
                user,
                _className,
                operation,
                result,
                timestamp = DateTime.UtcNow
            };

            await _container.CreateItemAsync(auditLog, new PartitionKey(user));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error auditing");
            throw;
        }
    }
}

