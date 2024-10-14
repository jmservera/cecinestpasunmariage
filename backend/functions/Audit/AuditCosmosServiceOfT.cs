namespace functions.Audit;

public class AuditInCosmosService<T>(IAuditServiceFactory auditServiceFactory) : IAuditService<T>
{
    private readonly IAuditService _auditService = auditServiceFactory.Get(typeof(T).Name);

    public void Audit(string user, string operation, string result)
    {
        _auditService.Audit(user, operation, result);
    }
}