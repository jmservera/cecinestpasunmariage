namespace functions.Audit;

/// <summary>
/// Service for auditing operations.
/// </summary>
/// <typeparam name="T">The type of the class being audited.</typeparam>
public interface IAuditService<out T>
{
    /// <summary>
    /// Audits an operation performed by a user.
    /// </summary>
    /// <param name="user"> The user who performed the operation. </param>
    /// <param name="operation"> The operation that was performed. </param>
    /// <param name="result"> The result of the operation. </param>
    public void Audit(string user, string operation, string result);
}