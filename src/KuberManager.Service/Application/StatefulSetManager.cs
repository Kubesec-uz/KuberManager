using k8s.Models;
using KuberManager.Service.Application.DTOs;
using KuberManager.Service.Domain;
using KuberManager.Service.Infrastructure.Audit;
using KuberManager.Service.Infrastructure.Kubernetes;
using KuberManager.Service.Infrastructure.Policy;

namespace KuberManager.Service.Application;

public interface IStatefulSetManager
{
    Task<IReadOnlyList<StatefulSetDto>> ListAsync(string cluster, string ns, CancellationToken ct = default);
    Task<StatefulSetDto> GetAsync(string cluster, string ns, string name, CancellationToken ct = default);
    Task<OperationResult> CreateAsync(CreateStatefulSetDto dto, CancellationToken ct = default);
    Task<OperationResult> DeleteAsync(string cluster, string ns, string name, string requestedBy, string reason, string correlationId, CancellationToken ct = default);
    Task<OperationResult> ScaleAsync(string cluster, string ns, string name, int replicas, string requestedBy, string reason, string correlationId, CancellationToken ct = default);
    Task<OperationResult> RestartAsync(string cluster, string ns, string name, string requestedBy, string reason, string correlationId, CancellationToken ct = default);
}

public sealed class StatefulSetManager : IStatefulSetManager
{
    private readonly IKubernetesFacadeFactory _k8sFactory;
    private readonly IOperationPolicy _policy;
    private readonly IAuditService _audit;
    private readonly ILogger<StatefulSetManager> _logger;

    public StatefulSetManager(IKubernetesFacadeFactory k8s, IOperationPolicy policy, IAuditService audit, ILogger<StatefulSetManager> logger)
    { _k8sFactory = k8s; _policy = policy; _audit = audit; _logger = logger; }

    public async Task<IReadOnlyList<StatefulSetDto>> ListAsync(string cluster, string ns, CancellationToken ct = default)
    {
        _policy.EnsureNamespaceAllowed(ns);
        var list = await _k8sFactory.For(cluster).ListStatefulSetsAsync(ns, ct);
        return list.Select(MapToDto).ToList();
    }

    public async Task<StatefulSetDto> GetAsync(string cluster, string ns, string name, CancellationToken ct = default)
    {
        _policy.EnsureNamespaceAllowed(ns);
        return MapToDto(await _k8sFactory.For(cluster).GetStatefulSetAsync(ns, name, ct));
    }

    public async Task<OperationResult> CreateAsync(CreateStatefulSetDto dto, CancellationToken ct = default)
    {
        _policy.EnsureNamespaceAllowed(dto.Namespace);
        _policy.EnsureReplicaLimit(dto.Replicas);
        _logger.LogInformation("Creating statefulset {Namespace}/{Name}", dto.Namespace, dto.Name);
        try
        {
            var labels = dto.Labels.Count > 0 ? dto.Labels : new Dictionary<string, string> { ["app"] = dto.Name };
            var sts = new V1StatefulSet
            {
                Metadata = new V1ObjectMeta { Name = dto.Name, NamespaceProperty = dto.Namespace, Labels = labels },
                Spec = new V1StatefulSetSpec
                {
                    Replicas = dto.Replicas,
                    ServiceName = dto.ServiceName,
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
                                    Env = dto.EnvVars.Select(e => new V1EnvVar { Name = e.Key, Value = e.Value }).ToList(),
                                    Ports = dto.Ports.Select(p => new V1ContainerPort { Name = p.Name, ContainerPort = p.ContainerPort, Protocol = p.Protocol }).ToList()
                                }
                            }
                        }
                    }
                }
            };
            await _k8sFactory.For(dto.Cluster).CreateStatefulSetAsync(dto.Namespace, sts, ct);
            await _audit.RecordAsync(new AuditEntry { CorrelationId = dto.CorrelationId, RequestedBy = dto.RequestedBy, Action = "CreateStatefulSet", Cluster = dto.Cluster, Namespace = dto.Namespace, ResourceType = "StatefulSet", ResourceName = dto.Name, Reason = dto.Reason, Success = true }, ct);
            return OperationResult.Ok(dto.CorrelationId);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to create statefulset {Namespace}/{Name}", dto.Namespace, dto.Name);
            await _audit.RecordAsync(new AuditEntry { CorrelationId = dto.CorrelationId, RequestedBy = dto.RequestedBy, Action = "CreateStatefulSet", Cluster = dto.Cluster, Namespace = dto.Namespace, ResourceType = "StatefulSet", ResourceName = dto.Name, Reason = dto.Reason, Success = false, ErrorMessage = ex.Message }, ct);
            return OperationResult.Fail(dto.CorrelationId, ex.Message);
        }
    }

    public async Task<OperationResult> DeleteAsync(string cluster, string ns, string name, string requestedBy, string reason, string correlationId, CancellationToken ct = default)
    {
        _policy.EnsureNamespaceAllowed(ns);
        try
        {
            await _k8sFactory.For(cluster).DeleteStatefulSetAsync(ns, name, ct);
            await _audit.RecordAsync(new AuditEntry { CorrelationId = correlationId, RequestedBy = requestedBy, Action = "DeleteStatefulSet", Cluster = cluster, Namespace = ns, ResourceType = "StatefulSet", ResourceName = name, Reason = reason, Success = true }, ct);
            return OperationResult.Ok(correlationId);
        }
        catch (Exception ex)
        {
            await _audit.RecordAsync(new AuditEntry { CorrelationId = correlationId, RequestedBy = requestedBy, Action = "DeleteStatefulSet", Cluster = cluster, Namespace = ns, ResourceType = "StatefulSet", ResourceName = name, Reason = reason, Success = false, ErrorMessage = ex.Message }, ct);
            return OperationResult.Fail(correlationId, ex.Message);
        }
    }

    public async Task<OperationResult> ScaleAsync(string cluster, string ns, string name, int replicas, string requestedBy, string reason, string correlationId, CancellationToken ct = default)
    {
        _policy.EnsureNamespaceAllowed(ns);
        _policy.EnsureReplicaLimit(replicas);
        try
        {
            await _k8sFactory.For(cluster).PatchStatefulSetReplicasAsync(ns, name, replicas, ct);
            await _audit.RecordAsync(new AuditEntry { CorrelationId = correlationId, RequestedBy = requestedBy, Action = "ScaleStatefulSet", Cluster = cluster, Namespace = ns, ResourceType = "StatefulSet", ResourceName = name, Reason = reason, Success = true }, ct);
            return OperationResult.Ok(correlationId);
        }
        catch (Exception ex)
        {
            await _audit.RecordAsync(new AuditEntry { CorrelationId = correlationId, RequestedBy = requestedBy, Action = "ScaleStatefulSet", Cluster = cluster, Namespace = ns, ResourceType = "StatefulSet", ResourceName = name, Reason = reason, Success = false, ErrorMessage = ex.Message }, ct);
            return OperationResult.Fail(correlationId, ex.Message);
        }
    }

    public async Task<OperationResult> RestartAsync(string cluster, string ns, string name, string requestedBy, string reason, string correlationId, CancellationToken ct = default)
    {
        _policy.EnsureNamespaceAllowed(ns);
        try
        {
            await _k8sFactory.For(cluster).PatchStatefulSetRestartAsync(ns, name, DateTimeOffset.UtcNow, ct);
            await _audit.RecordAsync(new AuditEntry { CorrelationId = correlationId, RequestedBy = requestedBy, Action = "RestartStatefulSet", Cluster = cluster, Namespace = ns, ResourceType = "StatefulSet", ResourceName = name, Reason = reason, Success = true }, ct);
            return OperationResult.Ok(correlationId);
        }
        catch (Exception ex)
        {
            await _audit.RecordAsync(new AuditEntry { CorrelationId = correlationId, RequestedBy = requestedBy, Action = "RestartStatefulSet", Cluster = cluster, Namespace = ns, ResourceType = "StatefulSet", ResourceName = name, Reason = reason, Success = false, ErrorMessage = ex.Message }, ct);
            return OperationResult.Fail(correlationId, ex.Message);
        }
    }

    private static StatefulSetDto MapToDto(V1StatefulSet sts) => new()
    {
        Name = sts.Metadata.Name,
        Namespace = sts.Metadata.NamespaceProperty,
        DesiredReplicas = sts.Spec?.Replicas ?? 0,
        ReadyReplicas = sts.Status?.ReadyReplicas ?? 0,
        CurrentReplicas = sts.Status?.CurrentReplicas ?? 0,
        ServiceName = sts.Spec?.ServiceName ?? string.Empty,
        Status = (sts.Status?.ReadyReplicas ?? 0) >= (sts.Spec?.Replicas ?? 0) ? "Ready" : "NotReady",
        CreatedAt = sts.Metadata.CreationTimestamp?.ToString("O") ?? string.Empty
    };
}
