namespace KuberManager.Service.Infrastructure.Audit;

public interface IAuditService
{
    Task RecordAsync(AuditEntry entry, CancellationToken ct = default);
}
