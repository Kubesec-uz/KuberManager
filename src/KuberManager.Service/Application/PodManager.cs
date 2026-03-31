using KuberManager.Service.Application.DTOs;
using KuberManager.Service.Domain;
using KuberManager.Service.Infrastructure.Audit;
using KuberManager.Service.Infrastructure.Kubernetes;
using KuberManager.Service.Infrastructure.Policy;

namespace KuberManager.Service.Application;

public sealed class PodManager : IPodManager
{
    private readonly IKubernetesFacadeFactory _k8sFactory;
    private readonly IOperationPolicy _policy;
    private readonly IAuditService _audit;
    private readonly ILogger<PodManager> _logger;

    public PodManager(
        IKubernetesFacadeFactory k8sFactory,
        IOperationPolicy policy,
        IAuditService audit,
        ILogger<PodManager> logger)
    {
        _k8sFactory = k8sFactory;
        _policy = policy;
        _audit = audit;
        _logger = logger;
    }

    public async Task<IReadOnlyList<PodInfoDto>> ListAsync(string cluster, string ns, string? appLabel = null, CancellationToken ct = default)
    {
        _policy.EnsureNamespaceAllowed(ns);
        var labelSelector = appLabel is not null ? $"app={appLabel}" : null;
        var pods = await _k8sFactory.For(cluster).ListPodsAsync(ns, labelSelector, ct);
        return pods.Select(MapToDto).ToList();
    }

    public async Task<PodInfoDto> GetAsync(string cluster, string ns, string podName, CancellationToken ct = default)
    {
        _policy.EnsureNamespaceAllowed(ns);
        var pod = await _k8sFactory.For(cluster).GetPodAsync(ns, podName, ct);
        return MapToDto(pod);
    }

    public async Task<string> GetLogsAsync(string cluster, string ns, string podName, string? container = null, int? tailLines = null, CancellationToken ct = default)
    {
        _policy.EnsureNamespaceAllowed(ns);
        return await _k8sFactory.For(cluster).GetPodLogsAsync(ns, podName, container, tailLines, ct);
    }

    public async Task<OperationResult> DeleteAsync(
        string cluster, string ns, string podName,
        string requestedBy, string reason, string correlationId,
        CancellationToken ct = default)
    {
        _policy.EnsureNamespaceAllowed(ns);

        _logger.LogInformation(
            "Deleting pod {Namespace}/{PodName} by {RequestedBy} [{CorrelationId}]",
            ns, podName, requestedBy, correlationId);

        try
        {
            await _k8sFactory.For(cluster).DeletePodAsync(ns, podName, ct);
            await _audit.RecordAsync(new AuditEntry
            {
                CorrelationId = correlationId,
                RequestedBy = requestedBy,
                Action = "DeletePod",
                Cluster = cluster,
                Namespace = ns,
                ResourceType = "Pod",
                ResourceName = podName,
                Reason = reason,
                Success = true
            });
            return OperationResult.Ok(correlationId);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to delete pod {Namespace}/{PodName}", ns, podName);
            await _audit.RecordAsync(new AuditEntry
            {
                CorrelationId = correlationId,
                RequestedBy = requestedBy,
                Action = "DeletePod",
                Cluster = cluster,
                Namespace = ns,
                ResourceType = "Pod",
                ResourceName = podName,
                Reason = reason,
                Success = false,
                ErrorMessage = ex.Message
            });
            return OperationResult.Fail(correlationId, ex.Message);
        }
    }

    public async IAsyncEnumerable<(string output, bool isStderr)> ExecAsync(
        string cluster, string ns, string podName, string container, string[] command,
        string requestedBy, string correlationId,
        [System.Runtime.CompilerServices.EnumeratorCancellation] CancellationToken ct = default)
    {
        _policy.EnsureNamespaceAllowed(ns);
        _logger.LogInformation("Exec in {Namespace}/{PodName} by {RequestedBy} [{CorrelationId}]",
            ns, podName, requestedBy, correlationId);

        await _audit.RecordAsync(new AuditEntry
        {
            CorrelationId = correlationId,
            RequestedBy = requestedBy,
            Action = "ExecPod",
            Cluster = cluster,
            Namespace = ns,
            ResourceType = "Pod",
            ResourceName = podName,
            Reason = string.Join(" ", command),
            Success = true
        }, ct);

        await foreach (var item in _k8sFactory.For(cluster).ExecInPodAsync(ns, podName, container, command, ct))
            yield return item;
    }

    private static PodInfoDto MapToDto(k8s.Models.V1Pod pod) => new()
    {
        Name = pod.Metadata.Name,
        Namespace = pod.Metadata.NamespaceProperty,
        Phase = pod.Status?.Phase ?? "Unknown",
        NodeName = pod.Spec?.NodeName ?? string.Empty,
        PodIp = pod.Status?.PodIP ?? string.Empty,
        StartTime = pod.Status?.StartTime?.ToString("O") ?? string.Empty,
        Containers = pod.Status?.ContainerStatuses?.Select(cs => new ContainerStatusDto
        {
            Name = cs.Name,
            Ready = cs.Ready,
            RestartCount = cs.RestartCount,
            State = cs.State?.Running is not null ? "Running"
                  : cs.State?.Waiting is not null ? "Waiting"
                  : cs.State?.Terminated is not null ? "Terminated"
                  : "Unknown"
        }).ToList() ?? []
    };
}
