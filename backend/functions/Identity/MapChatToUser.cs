using Microsoft.Azure.Cosmos;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;

namespace functions.Identity;

/// <summary>
/// Service for auditing operations in CosmosDB.
/// </summary>
public class MapChatToUser : IChatUserMapper
{
    private static bool _initialized = false;
    private readonly IConfiguration _configuration;
    private readonly CosmosClient _cosmosClient;
    private readonly Container _container;
    private readonly ILogger _logger;
    private readonly string _dbName;

    private const string _containerName = "ChatToUserMapping";

    /// <summary>
    /// Initializes a new instance of the <see cref="MapChatToUser"/> class.
    /// </summary>
    /// <param name="configuration"> The configuration instance. </param>
    /// <param name="logger"> The logger instance. </param>
    /// <exception cref="InvalidOperationException"> Thrown when the DATABASE_CONNECTION_STRING is not set. </exception>
    public MapChatToUser(IConfiguration configuration, ILogger<MapChatToUser> logger)
    {
        _configuration = configuration;
        var cosmosConnectionString = _configuration.GetValue<string>("DATABASE_CONNECTION_STRING") ?? throw new InvalidOperationException("DATABASE_CONNECTION_STRING is not set.");
        _dbName = _configuration.GetValue<string>("DATABASE_NAME") ?? throw new InvalidOperationException("DATABASE_NAME is not set.");

        _cosmosClient = new CosmosClient(cosmosConnectionString);

        _container = _cosmosClient.GetDatabase(_dbName).GetContainer("ChatToUserMapping");
        _logger = logger;
    }

    private async Task EnsureInitializedAsync()
    {
        if (!_initialized)
        {
            // todo: thread safety
            _logger.LogInformation("Initializing CosmosDB");
            var db = await _cosmosClient.CreateDatabaseIfNotExistsAsync(_dbName);
            await db.Database.CreateContainerIfNotExistsAsync(_containerName, "/id");
            _initialized = true;
        }
    }

    public async Task<ChatUser?> GetUserAsync(string chatId)
    {
        await EnsureInitializedAsync();
        try
        {
            var user = await _container.ReadItemAsync<ChatUser>(chatId, new PartitionKey(chatId));
            return user?.Resource;
        }
        catch (CosmosException ex)
        {
            if (ex.StatusCode == System.Net.HttpStatusCode.NotFound)
            {
                return null;
            }
            else
            {
                _logger.LogError(ex, "Error reading user from CosmosDB.");
                return null;
            }
        }
    }

    public async Task SaveUserAsync(ChatUser user)
    {
        if (user.UserAuthId == null)
        {
            throw new InvalidOperationException($"{nameof(user.UserAuthId)} is not set.");
        }
        await EnsureInitializedAsync();
        await _container.UpsertItemAsync(user);
    }

    public async Task RemoveUserAsync(string chatId)
    {
        await _container.DeleteItemAsync<ChatUser>(chatId, new PartitionKey(chatId));
    }
}