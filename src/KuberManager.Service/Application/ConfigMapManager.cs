using k8s.Models;
using KuberManager.Service.Application.DTOs;
using KuberManager.Service.Domain;
using KuberManager.Service.Infrastructure.Audit;
using KuberManager.Service.Infrastructure.Kubernetes;
using KuberManager.Service.Infrastructure.Policy;

namespace KuberManager.Service.Application;

public sealed class ConfigMapManager : IConfigMapManager
{
    private readonly IKubernetesFacadeFactory _k8sFactory;
    private readonly IOperationPolicy _policy;
    private readonly IAuditService _audit;
    private readonly ILogger<ConfigMapManager> _logger;

    public ConfigMapManager(IKubernetesFacadeFactory k8sFactory, IOperationPolicy policy, IAuditService audit, ILogger<ConfigMapManager> logger)
    {
        _k8sFactory = k8sFactory;
        _policy = policy;
        _audit = audit;
        _logger = logger;
    }

    public async Task<IReadOnlyList<ConfigMapDto>> ListAsync(string cluster, string ns, CancellationToken ct = default)
    {
        _policy.EnsureNamespaceAllowed(ns);
        var list = await _k8sFactory.For(cluster).ListConfigMapsAsync(ns, ct);
        return list.Select(MapToDto).ToList();
    }

    public async Task<ConfigMapDto> GetAsync(string cluster, string ns, string name, CancellationToken ct = default)
    {
        _policy.EnsureNamespaceAllowed(ns);
        var cm = await _k8sFactory.For(cluster).GetConfigMapAsync(ns, name, ct);
        return MapToDto(cm);
    }

    public async Task<OperationResult> CreateAsync(string cluster, string ns, string name, Dictionary<string, string> data, string requestedBy, string reason, string correlationId, CancellationToken ct = default)
    {
        _policy.EnsureNamespaceAllowed(ns);
        _logger.LogInformation("Creating configmap {Namespace}/{Name} by {RequestedBy} [{CorrelationId}]", ns, name, requestedBy, correlationId);
        try
        {
            var cm = new V1ConfigMap
            {
                Metadata = new V1ObjectMeta { Name = name, NamespaceProperty = ns },
                Data = data
            };
            await _k8sFactory.For(cluster).CreateConfigMapAsync(ns, cm, ct);
            await _audit.RecordAsync(new AuditEntry
            {
                CorrelationId = correlationId, RequestedBy = requestedBy,
                Action = "CreateConfigMap", Cluster = cluster, Namespace = ns,
                ResourceType = "ConfigMap", ResourceName = name, Reason = reason, Success = true
            }, ct);
            return OperationResult.Ok(correlationId);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to create configmap {Namespace}/{Name}", ns, name);
            await _audit.RecordAsync(new AuditEntry
            {
                CorrelationId = correlationId, RequestedBy = requestedBy,
                Action = "CreateConfigMap", Cluster = cluster, Namespace = ns,
                ResourceType = "ConfigMap", ResourceName = name, Reason = reason,
                Success = false, ErrorMessage = ex.Message
            }, ct);
            return OperationResult.Fail(correlationId, ex.Message);
        }
    }

    public async Task<OperationResult> UpdateAsync(string cluster, string ns, string name, Dictionary<string, string> data, string requestedBy, string reason, string correlationId, CancellationToken ct = default)
    {
        _policy.EnsureNamespaceAllowed(ns);
        _logger.LogInformation("Updating configmap {Namespace}/{Name} by {RequestedBy} [{CorrelationId}]", ns, name, requestedBy, correlationId);
        try
        {
            var k8s = _k8sFactory.For(cluster);
            var existing = await k8s.GetConfigMapAsync(ns, name, ct);
            existing.Data = data;
            await k8s.UpdateConfigMapAsync(ns, name, existing, ct);
            await _audit.RecordAsync(new AuditEntry
            {
                CorrelationId = correlationId, RequestedBy = requestedBy,
                Action = "UpdateConfigMap", Cluster = cluster, Namespace = ns,
                ResourceType = "ConfigMap", ResourceName = name, Reason = reason, Success = true
            }, ct);
            return OperationResult.Ok(correlationId);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to update configmap {Namespace}/{Name}", ns, name);
            await _audit.RecordAsync(new AuditEntry
            {
                CorrelationId = correlationId, RequestedBy = requestedBy,
                Action = "UpdateConfigMap", Cluster = cluster, Namespace = ns,
                ResourceType = "ConfigMap", ResourceName = name, Reason = reason,
                Success = false, ErrorMessage = ex.Message
            }, ct);
            return OperationResult.Fail(correlationId, ex.Message);
        }
    }

    public async Task<OperationResult> DeleteAsync(string cluster, string ns, string name, string requestedBy, string reason, string correlationId, CancellationToken ct = default)
    {
        _policy.EnsureNamespaceAllowed(ns);
        _logger.LogInformation("Deleting configmap {Namespace}/{Name} by {RequestedBy} [{CorrelationId}]", ns, name, requestedBy, correlationId);
        try
        {
            await _k8sFactory.For(cluster).DeleteConfigMapAsync(ns, name, ct);
            await _audit.RecordAsync(new AuditEntry
            {
                CorrelationId = correlationId, RequestedBy = requestedBy,
                Action = "DeleteConfigMap", Cluster = cluster, Namespace = ns,
                ResourceType = "ConfigMap", ResourceName = name, Reason = reason, Success = true
            }, ct);
            return OperationResult.Ok(correlationId);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to delete configmap {Namespace}/{Name}", ns, name);
            await _audit.RecordAsync(new AuditEntry
            {
                CorrelationId = correlationId, RequestedBy = requestedBy,
                Action = "DeleteConfigMap", Cluster = cluster, Namespace = ns,
                ResourceType = "ConfigMap", ResourceName = name, Reason = reason,
                Success = false, ErrorMessage = ex.Message
            }, ct);
            return OperationResult.Fail(correlationId, ex.Message);
        }
    }

    private static ConfigMapDto MapToDto(V1ConfigMap cm) => new()
    {
        Name = cm.Metadata.Name,
        Namespace = cm.Metadata.NamespaceProperty,
        Data = cm.Data?.ToDictionary(k => k.Key, v => v.Value) ?? new(),
        CreatedAt = cm.Metadata.CreationTimestamp?.ToString("O") ?? string.Empty
    };
}
