namespace functions.Audit;

public interface IAuditServiceFactory
{
    IAuditService Get(string className);
}