using System.Text.Json;

namespace KuberManager.Service.Infrastructure.Audit;

/// <summary>
/// Structured log-based audit. Replace with DB-backed or external sink as needed.
/// </summary>
public sealed class LogAuditService : IAuditService
{
    private readonly ILogger<LogAuditService> _logger;

    public LogAuditService(ILogger<LogAuditService> logger)
    {
        _logger = logger;
    }

    public Task RecordAsync(AuditEntry entry, CancellationToken ct = default)
    {
        _logger.LogInformation(
            "[AUDIT] Id={Id} CorrelationId={CorrelationId} RequestedBy={RequestedBy} " +
            "Action={Action} Cluster={Cluster} Namespace={Namespace} " +
            "ResourceType={ResourceType} ResourceName={ResourceName} " +
            "Reason={Reason} Success={Success} Error={ErrorMessage}",
            entry.Id,
            entry.CorrelationId,
            entry.RequestedBy,
            entry.Action,
            entry.Cluster,
            entry.Namespace,
            entry.ResourceType,
            entry.ResourceName,
            entry.Reason,
            entry.Success,
            entry.ErrorMessage);

        return Task.CompletedTask;
    }
}
