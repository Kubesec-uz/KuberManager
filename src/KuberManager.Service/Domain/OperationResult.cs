namespace KuberManager.Service.Domain;

public sealed class OperationResult
{
    public bool Success { get; private init; }
    public string Message { get; private init; }
    public string CorrelationId { get; private init; }

    private OperationResult(bool success, string message, string correlationId)
    {
        Success = success;
        Message = message;
        CorrelationId = correlationId;
    }

    public static OperationResult Ok(string correlationId, string message = "Operation completed successfully.")
        => new(true, message, correlationId);

    public static OperationResult Fail(string correlationId, string message)
        => new(false, message, correlationId);
}
