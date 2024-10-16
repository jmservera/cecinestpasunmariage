namespace functions.Audit;

/// <summary>
/// Service for auditing operations.
/// </summary>
public interface IAuditService
{
    /// <summary>
    /// Audits an operation performed by a user.
    /// </summary>
    /// <param name="user"> The user who performed the operation. </param>
    /// <param name="operation"> The operation that was performed. </param>
    /// <param name="result"> The result of the operation. </param>
    public void Audit(string user, string operation, string result, string? operationId = null);

    /// <summary>
    /// Initializes the audit service.
    /// </summary>
    /// <returns> A task that represents the asynchronous operation. </returns>
    public Task InitializeAsync();
}