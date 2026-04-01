using k8s.Models;
using KuberManager.Service.Application.DTOs;
using KuberManager.Service.Domain;
using KuberManager.Service.Infrastructure.Audit;
using KuberManager.Service.Infrastructure.Kubernetes;
using KuberManager.Service.Infrastructure.Policy;

namespace KuberManager.Service.Application;

public interface INodeManager
{
    Task<IReadOnlyList<NodeDto>> ListAsync(string cluster, CancellationToken ct = default);
    Task<NodeDto> GetAsync(string cluster, string name, CancellationToken ct = default);
    Task<OperationResult> CordonAsync(string cluster, string name, string requestedBy, string reason, string correlationId, CancellationToken ct = default);
    Task<OperationResult> UncordonAsync(string cluster, string name, string requestedBy, string reason, string correlationId, CancellationToken ct = default);
    Task<OperationResult> DrainAsync(string cluster, string name, bool force, bool ignoreDaemonSets, string requestedBy, string reason, string correlationId, CancellationToken ct = default);
}

public interface IPvcManager
{
    Task<IReadOnlyList<PvcDto>> ListAsync(string cluster, string ns, CancellationToken ct = default);
    Task<PvcDto> GetAsync(string cluster, string ns, string name, CancellationToken ct = default);
    Task<OperationResult> CreateAsync(CreatePvcDto dto, CancellationToken ct = default);
    Task<OperationResult> DeleteAsync(string cluster, string ns, string name, string requestedBy, string reason, string correlationId, CancellationToken ct = default);
}

public interface IHpaManager
{
    Task<IReadOnlyList<HpaDto>> ListAsync(string cluster, string ns, CancellationToken ct = default);
    Task<HpaDto> GetAsync(string cluster, string ns, string name, CancellationToken ct = default);
    Task<OperationResult> CreateAsync(CreateHpaDto dto, CancellationToken ct = default);
    Task<OperationResult> DeleteAsync(string cluster, string ns, string name, string requestedBy, string reason, string correlationId, CancellationToken ct = default);
}

public sealed class NodeManager : INodeManager
{
    private readonly IKubernetesFacadeFactory _k8sFactory;
    private readonly IAuditService _audit;
    private readonly ILogger<NodeManager> _logger;

    public NodeManager(IKubernetesFacadeFactory k8s, IAuditService audit, ILogger<NodeManager> logger)
    { _k8sFactory = k8s; _audit = audit; _logger = logger; }

    public async Task<IReadOnlyList<NodeDto>> ListAsync(string cluster, CancellationToken ct = default)
        => (await _k8sFactory.For(cluster).ListNodesAsync(ct)).Select(MapToDto).ToList();

    public async Task<NodeDto> GetAsync(string cluster, string name, CancellationToken ct = default)
        => MapToDto(await _k8sFactory.For(cluster).GetNodeAsync(name, ct));

    public async Task<OperationResult> CordonAsync(string cluster, string name, string requestedBy, string reason, string correlationId, CancellationToken ct = default)
    {
        try
        {
            await _k8sFactory.For(cluster).PatchNodeUnschedulableAsync(name, true, ct);
            await _audit.RecordAsync(new AuditEntry { CorrelationId = correlationId, RequestedBy = requestedBy, Action = "CordonNode", Cluster = cluster, ResourceType = "Node", ResourceName = name, Reason = reason, Success = true }, ct);
            return OperationResult.Ok(correlationId);
        }
        catch (Exception ex)
        {
            await _audit.RecordAsync(new AuditEntry { CorrelationId = correlationId, RequestedBy = requestedBy, Action = "CordonNode", Cluster = cluster, ResourceType = "Node", ResourceName = name, Reason = reason, Success = false, ErrorMessage = ex.Message }, ct);
            return OperationResult.Fail(correlationId, ex.Message);
        }
    }

    public async Task<OperationResult> UncordonAsync(string cluster, string name, string requestedBy, string reason, string correlationId, CancellationToken ct = default)
    {
        try
        {
            await _k8sFactory.For(cluster).PatchNodeUnschedulableAsync(name, false, ct);
            await _audit.RecordAsync(new AuditEntry { CorrelationId = correlationId, RequestedBy = requestedBy, Action = "UncordonNode", Cluster = cluster, ResourceType = "Node", ResourceName = name, Reason = reason, Success = true }, ct);
            return OperationResult.Ok(correlationId);
        }
        catch (Exception ex)
        {
            await _audit.RecordAsync(new AuditEntry { CorrelationId = correlationId, RequestedBy = requestedBy, Action = "UncordonNode", Cluster = cluster, ResourceType = "Node", ResourceName = name, Reason = reason, Success = false, ErrorMessage = ex.Message }, ct);
            return OperationResult.Fail(correlationId, ex.Message);
        }
    }

    public async Task<OperationResult> DrainAsync(string cluster, string name, bool force, bool ignoreDaemonSets, string requestedBy, string reason, string correlationId, CancellationToken ct = default)
    {
        _logger.LogInformation("Draining node {Name}", name);
        try
        {
            await _k8sFactory.For(cluster).PatchNodeUnschedulableAsync(name, true, ct);
            await _k8sFactory.For(cluster).DeletePodsOnNodeAsync(name, force, ignoreDaemonSets, ct);
            await _audit.RecordAsync(new AuditEntry { CorrelationId = correlationId, RequestedBy = requestedBy, Action = "DrainNode", Cluster = cluster, ResourceType = "Node", ResourceName = name, Reason = reason, Success = true }, ct);
            return OperationResult.Ok(correlationId);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to drain node {Name}", name);
            await _audit.RecordAsync(new AuditEntry { CorrelationId = correlationId, RequestedBy = requestedBy, Action = "DrainNode", Cluster = cluster, ResourceType = "Node", ResourceName = name, Reason = reason, Success = false, ErrorMessage = ex.Message }, ct);
            return OperationResult.Fail(correlationId, ex.Message);
        }
    }

    private static NodeDto MapToDto(V1Node node)
    {
        var ready = node.Status?.Conditions?.FirstOrDefault(c => c.Type == "Ready")?.Status == "True";
        var roles = string.Join(",", node.Metadata.Labels?
            .Where(l => l.Key.StartsWith("node-role.kubernetes.io/"))
            .Select(l => l.Key.Replace("node-role.kubernetes.io/", "")) ?? Enumerable.Empty<string>());

        var capacity = node.Status?.Capacity;
        var cpuStr = capacity != null && capacity.TryGetValue("cpu", out var cpuQ) ? cpuQ.ToString() : string.Empty;
        var memStr = capacity != null && capacity.TryGetValue("memory", out var memQ) ? memQ.ToString() : string.Empty;

        return new NodeDto
        {
            Name = node.Metadata.Name,
            Status = ready ? "Ready" : "NotReady",
            Roles = roles,
            OsImage = node.Status?.NodeInfo?.OsImage ?? string.Empty,
            KernelVersion = node.Status?.NodeInfo?.KernelVersion ?? string.Empty,
            ContainerRuntime = node.Status?.NodeInfo?.ContainerRuntimeVersion ?? string.Empty,
            CpuCapacity = cpuStr,
            MemoryCapacity = memStr,
            Unschedulable = node.Spec?.Unschedulable ?? false,
            CreatedAt = node.Metadata.CreationTimestamp?.ToString("O") ?? string.Empty
        };
    }
}

public sealed class PvcManager : IPvcManager
{
    private readonly IKubernetesFacadeFactory _k8sFactory;
    private readonly IOperationPolicy _policy;
    private readonly IAuditService _audit;
    private readonly ILogger<PvcManager> _logger;

    public PvcManager(IKubernetesFacadeFactory k8s, IOperationPolicy policy, IAuditService audit, ILogger<PvcManager> logger)
    { _k8sFactory = k8s; _policy = policy; _audit = audit; _logger = logger; }

    public async Task<IReadOnlyList<PvcDto>> ListAsync(string cluster, string ns, CancellationToken ct = default)
    {
        _policy.EnsureNamespaceAllowed(ns);
        return (await _k8sFactory.For(cluster).ListPvcsAsync(ns, ct)).Select(MapToDto).ToList();
    }

    public async Task<PvcDto> GetAsync(string cluster, string ns, string name, CancellationToken ct = default)
    {
        _policy.EnsureNamespaceAllowed(ns);
        return MapToDto(await _k8sFactory.For(cluster).GetPvcAsync(ns, name, ct));
    }

    public async Task<OperationResult> CreateAsync(CreatePvcDto dto, CancellationToken ct = default)
    {
        _policy.EnsureNamespaceAllowed(dto.Namespace);
        _logger.LogInformation("Creating PVC {Namespace}/{Name}", dto.Namespace, dto.Name);
        try
        {
            var pvc = new V1PersistentVolumeClaim
            {
                Metadata = new V1ObjectMeta { Name = dto.Name, NamespaceProperty = dto.Namespace },
                Spec = new V1PersistentVolumeClaimSpec
                {
                    StorageClassName = dto.StorageClass,
                    AccessModes = dto.AccessModes,
                    Resources = new V1VolumeResourceRequirements
                    {
                        Requests = new Dictionary<string, ResourceQuantity> { ["storage"] = new ResourceQuantity(dto.Storage) }
                    }
                }
            };
            await _k8sFactory.For(dto.Cluster).CreatePvcAsync(dto.Namespace, pvc, ct);
            await _audit.RecordAsync(new AuditEntry { CorrelationId = dto.CorrelationId, RequestedBy = dto.RequestedBy, Action = "CreatePVC", Cluster = dto.Cluster, Namespace = dto.Namespace, ResourceType = "PersistentVolumeClaim", ResourceName = dto.Name, Reason = dto.Reason, Success = true }, ct);
            return OperationResult.Ok(dto.CorrelationId);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to create PVC {Namespace}/{Name}", dto.Namespace, dto.Name);
            await _audit.RecordAsync(new AuditEntry { CorrelationId = dto.CorrelationId, RequestedBy = dto.RequestedBy, Action = "CreatePVC", Cluster = dto.Cluster, Namespace = dto.Namespace, ResourceType = "PersistentVolumeClaim", ResourceName = dto.Name, Reason = dto.Reason, Success = false, ErrorMessage = ex.Message }, ct);
            return OperationResult.Fail(dto.CorrelationId, ex.Message);
        }
    }

    public async Task<OperationResult> DeleteAsync(string cluster, string ns, string name, string requestedBy, string reason, string correlationId, CancellationToken ct = default)
    {
        _policy.EnsureNamespaceAllowed(ns);
        try
        {
            await _k8sFactory.For(cluster).DeletePvcAsync(ns, name, ct);
            await _audit.RecordAsync(new AuditEntry { CorrelationId = correlationId, RequestedBy = requestedBy, Action = "DeletePVC", Cluster = cluster, Namespace = ns, ResourceType = "PersistentVolumeClaim", ResourceName = name, Reason = reason, Success = true }, ct);
            return OperationResult.Ok(correlationId);
        }
        catch (Exception ex)
        {
            await _audit.RecordAsync(new AuditEntry { CorrelationId = correlationId, RequestedBy = requestedBy, Action = "DeletePVC", Cluster = cluster, Namespace = ns, ResourceType = "PersistentVolumeClaim", ResourceName = name, Reason = reason, Success = false, ErrorMessage = ex.Message }, ct);
            return OperationResult.Fail(correlationId, ex.Message);
        }
    }

    private static PvcDto MapToDto(V1PersistentVolumeClaim pvc) => new()
    {
        Name = pvc.Metadata.Name,
        Namespace = pvc.Metadata.NamespaceProperty,
        Status = pvc.Status?.Phase ?? string.Empty,
        StorageClass = pvc.Spec?.StorageClassName ?? string.Empty,
        Capacity = (pvc.Status?.Capacity != null && pvc.Status.Capacity.TryGetValue("storage", out var storQty)) ? storQty.ToString() : string.Empty,
        AccessModes = string.Join(",", pvc.Spec?.AccessModes ?? new List<string>()),
        VolumeName = pvc.Spec?.VolumeName ?? string.Empty,
        CreatedAt = pvc.Metadata.CreationTimestamp?.ToString("O") ?? string.Empty
    };
}

public sealed class HpaManager : IHpaManager
{
    private readonly IKubernetesFacadeFactory _k8sFactory;
    private readonly IOperationPolicy _policy;
    private readonly IAuditService _audit;
    private readonly ILogger<HpaManager> _logger;

    public HpaManager(IKubernetesFacadeFactory k8s, IOperationPolicy policy, IAuditService audit, ILogger<HpaManager> logger)
    { _k8sFactory = k8s; _policy = policy; _audit = audit; _logger = logger; }

    public async Task<IReadOnlyList<HpaDto>> ListAsync(string cluster, string ns, CancellationToken ct = default)
    {
        _policy.EnsureNamespaceAllowed(ns);
        return (await _k8sFactory.For(cluster).ListHpasAsync(ns, ct)).Select(MapToDto).ToList();
    }

    public async Task<HpaDto> GetAsync(string cluster, string ns, string name, CancellationToken ct = default)
    {
        _policy.EnsureNamespaceAllowed(ns);
        return MapToDto(await _k8sFactory.For(cluster).GetHpaAsync(ns, name, ct));
    }

    public async Task<OperationResult> CreateAsync(CreateHpaDto dto, CancellationToken ct = default)
    {
        _policy.EnsureNamespaceAllowed(dto.Namespace);
        _logger.LogInformation("Creating HPA {Namespace}/{Name}", dto.Namespace, dto.Name);
        try
        {
            var hpa = new V2HorizontalPodAutoscaler
            {
                Metadata = new V1ObjectMeta { Name = dto.Name, NamespaceProperty = dto.Namespace },
                Spec = new V2HorizontalPodAutoscalerSpec
                {
                    ScaleTargetRef = new V2CrossVersionObjectReference { ApiVersion = "apps/v1", Kind = dto.TargetKind, Name = dto.TargetName },
                    MinReplicas = dto.MinReplicas,
                    MaxReplicas = dto.MaxReplicas,
                    Metrics = new List<V2MetricSpec>
                    {
                        new V2MetricSpec
                        {
                            Type = "Resource",
                            Resource = new V2ResourceMetricSource
                            {
                                Name = "cpu",
                                Target = new V2MetricTarget { Type = "Utilization", AverageUtilization = dto.CpuTargetUtilization }
                            }
                        }
                    }
                }
            };
            await _k8sFactory.For(dto.Cluster).CreateHpaAsync(dto.Namespace, hpa, ct);
            await _audit.RecordAsync(new AuditEntry { CorrelationId = dto.CorrelationId, RequestedBy = dto.RequestedBy, Action = "CreateHPA", Cluster = dto.Cluster, Namespace = dto.Namespace, ResourceType = "HorizontalPodAutoscaler", ResourceName = dto.Name, Reason = dto.Reason, Success = true }, ct);
            return OperationResult.Ok(dto.CorrelationId);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to create HPA {Namespace}/{Name}", dto.Namespace, dto.Name);
            await _audit.RecordAsync(new AuditEntry { CorrelationId = dto.CorrelationId, RequestedBy = dto.RequestedBy, Action = "CreateHPA", Cluster = dto.Cluster, Namespace = dto.Namespace, ResourceType = "HorizontalPodAutoscaler", ResourceName = dto.Name, Reason = dto.Reason, Success = false, ErrorMessage = ex.Message }, ct);
            return OperationResult.Fail(dto.CorrelationId, ex.Message);
        }
    }

    public async Task<OperationResult> DeleteAsync(string cluster, string ns, string name, string requestedBy, string reason, string correlationId, CancellationToken ct = default)
    {
        _policy.EnsureNamespaceAllowed(ns);
        try
        {
            await _k8sFactory.For(cluster).DeleteHpaAsync(ns, name, ct);
            await _audit.RecordAsync(new AuditEntry { CorrelationId = correlationId, RequestedBy = requestedBy, Action = "DeleteHPA", Cluster = cluster, Namespace = ns, ResourceType = "HorizontalPodAutoscaler", ResourceName = name, Reason = reason, Success = true }, ct);
            return OperationResult.Ok(correlationId);
        }
        catch (Exception ex)
        {
            await _audit.RecordAsync(new AuditEntry { CorrelationId = correlationId, RequestedBy = requestedBy, Action = "DeleteHPA", Cluster = cluster, Namespace = ns, ResourceType = "HorizontalPodAutoscaler", ResourceName = name, Reason = reason, Success = false, ErrorMessage = ex.Message }, ct);
            return OperationResult.Fail(correlationId, ex.Message);
        }
    }

    private static HpaDto MapToDto(V2HorizontalPodAutoscaler hpa) => new()
    {
        Name = hpa.Metadata.Name,
        Namespace = hpa.Metadata.NamespaceProperty,
        TargetKind = hpa.Spec?.ScaleTargetRef?.Kind ?? string.Empty,
        TargetName = hpa.Spec?.ScaleTargetRef?.Name ?? string.Empty,
        MinReplicas = hpa.Spec?.MinReplicas ?? 1,
        MaxReplicas = hpa.Spec?.MaxReplicas ?? 0,
        CurrentReplicas = hpa.Status?.CurrentReplicas ?? 0,
        DesiredReplicas = hpa.Status?.DesiredReplicas ?? 0,
        CpuTargetUtilization = hpa.Spec?.Metrics?.FirstOrDefault(m => m.Resource?.Name == "cpu")?.Resource?.Target?.AverageUtilization ?? 0,
        CreatedAt = hpa.Metadata.CreationTimestamp?.ToString("O") ?? string.Empty
    };
}
