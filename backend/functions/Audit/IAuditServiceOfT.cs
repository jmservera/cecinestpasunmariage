namespace functions.Audit;
public interface IAuditService<out T>
{
    public void Audit(string user, string operation, string result);
}