namespace functions.Audit;

/// <summary>
/// Service for auditing operations in Cosmos DB.
/// </summary>
/// <typeparam name="T">The type of the entity being audited.</typeparam>
/// <param name="auditServiceFactory">Factory to create audit services.</param>
public class AuditInCosmosService<T>(IAuditServiceFactory auditServiceFactory) : IAuditService<T>
{
    /// <summary>
    /// The audit service for the entity.
    /// </summary>
    private readonly IAuditService _auditService = auditServiceFactory.Get(typeof(T).Name);

    /// <summary>
    /// Audits an operation performed by a user.
    /// </summary>
    /// <param name="user">The user who performed the operation.</param>
    /// <param name="operation">The operation that was performed.</param>
    /// <param name="result">The result of the operation.</param>
    public void Audit(string user, string operation, string result)
    {
        _auditService.Audit(user, operation, result);
    }
}