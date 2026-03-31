using k8s.Models;
using KuberManager.Service.Application.DTOs;
using KuberManager.Service.Domain;
using KuberManager.Service.Infrastructure.Audit;
using KuberManager.Service.Infrastructure.Kubernetes;

namespace KuberManager.Service.Application;

public sealed class NamespaceManager : INamespaceManager
{
    private readonly IKubernetesFacadeFactory _k8sFactory;
    private readonly IAuditService _audit;
    private readonly ILogger<NamespaceManager> _logger;

    public NamespaceManager(IKubernetesFacadeFactory k8sFactory, IAuditService audit, ILogger<NamespaceManager> logger)
    {
        _k8sFactory = k8sFactory;
        _audit = audit;
        _logger = logger;
    }

    public async Task<IReadOnlyList<NamespaceInfoDto>> ListAsync(string cluster, CancellationToken ct = default)
    {
        var namespaces = await _k8sFactory.For(cluster).ListNamespacesAsync(ct);
        return namespaces.Select(MapToDto).ToList();
    }

    public async Task<NamespaceInfoDto> GetAsync(string cluster, string name, CancellationToken ct = default)
    {
        var ns = await _k8sFactory.For(cluster).GetNamespaceAsync(name, ct);
        return MapToDto(ns);
    }

    public async Task<OperationResult> CreateAsync(string cluster, string name, Dictionary<string, string> labels, string requestedBy, string reason, string correlationId, CancellationToken ct = default)
    {
        _logger.LogInformation("Creating namespace {Name} by {RequestedBy} [{CorrelationId}]", name, requestedBy, correlationId);
        try
        {
            var ns = new V1Namespace
            {
                Metadata = new V1ObjectMeta { Name = name, Labels = labels }
            };
            await _k8sFactory.For(cluster).CreateNamespaceAsync(ns, ct);
            await _audit.RecordAsync(new AuditEntry
            {
                CorrelationId = correlationId, RequestedBy = requestedBy,
                Action = "CreateNamespace", Cluster = cluster,
                Namespace = name, ResourceType = "Namespace", ResourceName = name,
                Reason = reason, Success = true
            }, ct);
            return OperationResult.Ok(correlationId);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to create namespace {Name}", name);
            await _audit.RecordAsync(new AuditEntry
            {
                CorrelationId = correlationId, RequestedBy = requestedBy,
                Action = "CreateNamespace", Cluster = cluster,
                Namespace = name, ResourceType = "Namespace", ResourceName = name,
                Reason = reason, Success = false, ErrorMessage = ex.Message
            }, ct);
            return OperationResult.Fail(correlationId, ex.Message);
        }
    }

    public async Task<OperationResult> DeleteAsync(string cluster, string name, string requestedBy, string reason, string correlationId, CancellationToken ct = default)
    {
        _logger.LogInformation("Deleting namespace {Name} by {RequestedBy} [{CorrelationId}]", name, requestedBy, correlationId);
        try
        {
            await _k8sFactory.For(cluster).DeleteNamespaceAsync(name, ct);
            await _audit.RecordAsync(new AuditEntry
            {
                CorrelationId = correlationId, RequestedBy = requestedBy,
                Action = "DeleteNamespace", Cluster = cluster,
                Namespace = name, ResourceType = "Namespace", ResourceName = name,
                Reason = reason, Success = true
            }, ct);
            return OperationResult.Ok(correlationId);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to delete namespace {Name}", name);
            await _audit.RecordAsync(new AuditEntry
            {
                CorrelationId = correlationId, RequestedBy = requestedBy,
                Action = "DeleteNamespace", Cluster = cluster,
                Namespace = name, ResourceType = "Namespace", ResourceName = name,
                Reason = reason, Success = false, ErrorMessage = ex.Message
            }, ct);
            return OperationResult.Fail(correlationId, ex.Message);
        }
    }

    private static NamespaceInfoDto MapToDto(V1Namespace ns) => new()
    {
        Name = ns.Metadata.Name,
        Status = ns.Status?.Phase ?? "Unknown",
        CreatedAt = ns.Metadata.CreationTimestamp?.ToString("O") ?? string.Empty,
        Labels = ns.Metadata.Labels?.ToDictionary(k => k.Key, v => v.Value) ?? new()
    };
}
