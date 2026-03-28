namespace KuberManager.Service.Infrastructure.Audit;

public sealed class AuditEntry
{
    public Guid Id { get; init; } = Guid.NewGuid();
    public DateTimeOffset Timestamp { get; init; } = DateTimeOffset.UtcNow;
    public string CorrelationId { get; init; } = string.Empty;
    public string RequestedBy { get; init; } = string.Empty;
    public string Action { get; init; } = string.Empty;
    public string Cluster { get; init; } = string.Empty;
    public string Namespace { get; init; } = string.Empty;
    public string ResourceType { get; init; } = string.Empty;
    public string ResourceName { get; init; } = string.Empty;
    public string Reason { get; init; } = string.Empty;
    public bool Success { get; init; }
    public string? ErrorMessage { get; init; }
}
