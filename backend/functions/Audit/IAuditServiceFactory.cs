namespace functions.Audit;

/// <summary>
/// Factory to create audit services.
/// </summary>
public interface IAuditServiceFactory
{
    /// <summary>
    /// Gets an audit service for a class.
    /// </summary>
    /// <param name="className">The name of the class being audited.</param>
    /// <returns>An audit service for the class.</returns>
    IAuditService Get(string className);
}