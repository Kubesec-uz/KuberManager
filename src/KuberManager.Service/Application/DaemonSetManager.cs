using k8s.Models;
using KuberManager.Service.Application.DTOs;
using KuberManager.Service.Domain;
using KuberManager.Service.Infrastructure.Audit;
using KuberManager.Service.Infrastructure.Kubernetes;
using KuberManager.Service.Infrastructure.Policy;

namespace KuberManager.Service.Application;

public interface IDaemonSetManager
{
    Task<IReadOnlyList<DaemonSetDto>> ListAsync(string cluster, string ns, CancellationToken ct = default);
    Task<DaemonSetDto> GetAsync(string cluster, string ns, string name, CancellationToken ct = default);
    Task<OperationResult> CreateAsync(CreateDaemonSetDto dto, CancellationToken ct = default);
    Task<OperationResult> DeleteAsync(string cluster, string ns, string name, string requestedBy, string reason, string correlationId, CancellationToken ct = default);
    Task<OperationResult> RestartAsync(string cluster, string ns, string name, string requestedBy, string reason, string correlationId, CancellationToken ct = default);
}

public sealed class DaemonSetManager : IDaemonSetManager
{
    private readonly IKubernetesFacadeFactory _k8sFactory;
    private readonly IOperationPolicy _policy;
    private readonly IAuditService _audit;
    private readonly ILogger<DaemonSetManager> _logger;

    public DaemonSetManager(IKubernetesFacadeFactory k8s, IOperationPolicy policy, IAuditService audit, ILogger<DaemonSetManager> logger)
    { _k8sFactory = k8s; _policy = policy; _audit = audit; _logger = logger; }

    public async Task<IReadOnlyList<DaemonSetDto>> ListAsync(string cluster, string ns, CancellationToken ct = default)
    {
        _policy.EnsureNamespaceAllowed(ns);
        return (await _k8sFactory.For(cluster).ListDaemonSetsAsync(ns, ct)).Select(MapToDto).ToList();
    }

    public async Task<DaemonSetDto> GetAsync(string cluster, string ns, string name, CancellationToken ct = default)
    {
        _policy.EnsureNamespaceAllowed(ns);
        return MapToDto(await _k8sFactory.For(cluster).GetDaemonSetAsync(ns, name, ct));
    }

    public async Task<OperationResult> CreateAsync(CreateDaemonSetDto dto, CancellationToken ct = default)
    {
        _policy.EnsureNamespaceAllowed(dto.Namespace);
        _logger.LogInformation("Creating daemonset {Namespace}/{Name}", dto.Namespace, dto.Name);
        try
        {
            var labels = dto.Labels.Count > 0 ? dto.Labels : new Dictionary<string, string> { ["app"] = dto.Name };
            var ds = new V1DaemonSet
            {
                Metadata = new V1ObjectMeta { Name = dto.Name, NamespaceProperty = dto.Namespace, Labels = labels },
                Spec = new V1DaemonSetSpec
                {
                    Selector = new V1LabelSelector { MatchLabels = labels },
                    Template = new V1PodTemplateSpec
                    {
                        Metadata = new V1ObjectMeta { Labels = labels },
                        Spec = new V1PodSpec
                        {
                            Containers = new List<V1Container>
                            {
                                new V1Container
                                {
                                    Name = dto.Name,
                                    Image = dto.Image,
                                    Env = dto.EnvVars.Select(e => new V1EnvVar { Name = e.Key, Value = e.Value }).ToList()
                                }
                            }
                        }
                    }
                }
            };
            await _k8sFactory.For(dto.Cluster).CreateDaemonSetAsync(dto.Namespace, ds, ct);
            await _audit.RecordAsync(new AuditEntry { CorrelationId = dto.CorrelationId, RequestedBy = dto.RequestedBy, Action = "CreateDaemonSet", Cluster = dto.Cluster, Namespace = dto.Namespace, ResourceType = "DaemonSet", ResourceName = dto.Name, Reason = dto.Reason, Success = true }, ct);
            return OperationResult.Ok(dto.CorrelationId);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to create daemonset {Namespace}/{Name}", dto.Namespace, dto.Name);
            await _audit.RecordAsync(new AuditEntry { CorrelationId = dto.CorrelationId, RequestedBy = dto.RequestedBy, Action = "CreateDaemonSet", Cluster = dto.Cluster, Namespace = dto.Namespace, ResourceType = "DaemonSet", ResourceName = dto.Name, Reason = dto.Reason, Success = false, ErrorMessage = ex.Message }, ct);
            return OperationResult.Fail(dto.CorrelationId, ex.Message);
        }
    }

    public async Task<OperationResult> DeleteAsync(string cluster, string ns, string name, string requestedBy, string reason, string correlationId, CancellationToken ct = default)
    {
        _policy.EnsureNamespaceAllowed(ns);
        try
        {
            await _k8sFactory.For(cluster).DeleteDaemonSetAsync(ns, name, ct);
            await _audit.RecordAsync(new AuditEntry { CorrelationId = correlationId, RequestedBy = requestedBy, Action = "DeleteDaemonSet", Cluster = cluster, Namespace = ns, ResourceType = "DaemonSet", ResourceName = name, Reason = reason, Success = true }, ct);
            return OperationResult.Ok(correlationId);
        }
        catch (Exception ex)
        {
            await _audit.RecordAsync(new AuditEntry { CorrelationId = correlationId, RequestedBy = requestedBy, Action = "DeleteDaemonSet", Cluster = cluster, Namespace = ns, ResourceType = "DaemonSet", ResourceName = name, Reason = reason, Success = false, ErrorMessage = ex.Message }, ct);
            return OperationResult.Fail(correlationId, ex.Message);
        }
    }

    public async Task<OperationResult> RestartAsync(string cluster, string ns, string name, string requestedBy, string reason, string correlationId, CancellationToken ct = default)
    {
        _policy.EnsureNamespaceAllowed(ns);
        try
        {
            await _k8sFactory.For(cluster).PatchDaemonSetRestartAsync(ns, name, DateTimeOffset.UtcNow, ct);
            await _audit.RecordAsync(new AuditEntry { CorrelationId = correlationId, RequestedBy = requestedBy, Action = "RestartDaemonSet", Cluster = cluster, Namespace = ns, ResourceType = "DaemonSet", ResourceName = name, Reason = reason, Success = true }, ct);
            return OperationResult.Ok(correlationId);
        }
        catch (Exception ex)
        {
            await _audit.RecordAsync(new AuditEntry { CorrelationId = correlationId, RequestedBy = requestedBy, Action = "RestartDaemonSet", Cluster = cluster, Namespace = ns, ResourceType = "DaemonSet", ResourceName = name, Reason = reason, Success = false, ErrorMessage = ex.Message }, ct);
            return OperationResult.Fail(correlationId, ex.Message);
        }
    }

    private static DaemonSetDto MapToDto(V1DaemonSet ds) => new()
    {
        Name = ds.Metadata.Name,
        Namespace = ds.Metadata.NamespaceProperty,
        DesiredNumberScheduled = ds.Status?.DesiredNumberScheduled ?? 0,
        CurrentNumberScheduled = ds.Status?.CurrentNumberScheduled ?? 0,
        NumberReady = ds.Status?.NumberReady ?? 0,
        Status = (ds.Status?.NumberReady ?? 0) >= (ds.Status?.DesiredNumberScheduled ?? 0) ? "Ready" : "NotReady",
        CreatedAt = ds.Metadata.CreationTimestamp?.ToString("O") ?? string.Empty
    };
}
