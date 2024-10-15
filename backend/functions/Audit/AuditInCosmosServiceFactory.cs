using System.Security.Cryptography;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;

namespace functions.Audit;

/// <summary>
/// Factory to create audit services.
/// </summary>
public class AuditInCosmosServiceFactory(IConfiguration configuration, ILoggerFactory loggerFactory) : IAuditServiceFactory
{
    /// <summary>
    /// Gets an audit service for a class.
    /// </summary>
    /// <param name="className">The name of the class being audited.</param>
    /// <returns>An audit service for the class.</returns>
    public IAuditService Get(string className)
    {
        var cosmos = new AuditInCosmosService(configuration, loggerFactory.CreateLogger(className), className);
        _ = cosmos.InitializeAsync();
        return cosmos;
    }
}