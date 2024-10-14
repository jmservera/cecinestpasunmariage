namespace functions.Audit;

public interface IAuditService
{
    public void Audit(string user, string operation, string result);
    public Task InitializeAsync();
}