using System.Security.Cryptography;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;

namespace functions.Audit;
public class AuditInCosmosServiceFactory(IConfiguration configuration, ILoggerFactory loggerFactory) : IAuditServiceFactory
{
    public IAuditService Get(string className)
    {
        var cosmos = new AuditInCosmosService(configuration, loggerFactory.CreateLogger(className), className);
        _ = cosmos.InitializeAsync();
        return cosmos;
    }
}