using k8s.Models;
using KuberManager.Service.Application.DTOs;
using KuberManager.Service.Domain;
using KuberManager.Service.Infrastructure.Audit;
using KuberManager.Service.Infrastructure.Kubernetes;
using KuberManager.Service.Infrastructure.Policy;

namespace KuberManager.Service.Application;

public sealed class DeploymentManager : IDeploymentManager
{
    private readonly IKubernetesFacade _k8s;
    private readonly IOperationPolicy _policy;
    private readonly IAuditService _audit;
    private readonly ILogger<DeploymentManager> _logger;

    public DeploymentManager(
        IKubernetesFacade k8s,
        IOperationPolicy policy,
        IAuditService audit,
        ILogger<DeploymentManager> logger)
    {
        _k8s = k8s;
        _policy = policy;
        _audit = audit;
        _logger = logger;
    }

    public async Task<DeploymentStatusDto> GetStatusAsync(string cluster, string ns, string name, CancellationToken ct = default)
    {
        _policy.EnsureNamespaceAllowed(ns);
        var deployment = await _k8s.GetDeploymentAsync(ns, name, ct);
        return MapToDto(deployment);
    }

    public async Task<IReadOnlyList<DeploymentStatusDto>> ListAsync(string cluster, string ns, CancellationToken ct = default)
    {
        _policy.EnsureNamespaceAllowed(ns);
        var deployments = await _k8s.ListDeploymentsAsync(ns, ct);
        return deployments.Select(MapToDto).ToList();
    }

    public async Task<OperationResult> ScaleAsync(
        string cluster, string ns, string name,
        int replicas, string requestedBy, string reason, string correlationId,
        CancellationToken ct = default)
    {
        _policy.EnsureNamespaceAllowed(ns);
        _policy.EnsureDeploymentAllowed(ns, name);
        _policy.EnsureReplicaLimit(replicas);

        _logger.LogInformation(
            "Scaling {Namespace}/{Name} to {Replicas} by {RequestedBy} [{CorrelationId}]",
            ns, name, replicas, requestedBy, correlationId);

        try
        {
            await _k8s.PatchDeploymentReplicasAsync(ns, name, replicas, ct);
            await _audit.RecordAsync(new AuditEntry
            {
                CorrelationId = correlationId,
                RequestedBy = requestedBy,
                Action = "ScaleDeployment",
                Cluster = cluster,
                Namespace = ns,
                ResourceType = "Deployment",
                ResourceName = name,
                Reason = reason,
                Success = true
            });
            return OperationResult.Ok(correlationId);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to scale {Namespace}/{Name}", ns, name);
            await _audit.RecordAsync(new AuditEntry
            {
                CorrelationId = correlationId,
                RequestedBy = requestedBy,
                Action = "ScaleDeployment",
                Cluster = cluster,
                Namespace = ns,
                ResourceType = "Deployment",
                ResourceName = name,
                Reason = reason,
                Success = false,
                ErrorMessage = ex.Message
            });
            return OperationResult.Fail(correlationId, ex.Message);
        }
    }

    public async Task<OperationResult> RestartAsync(
        string cluster, string ns, string name,
        string requestedBy, string reason, string correlationId,
        CancellationToken ct = default)
    {
        _policy.EnsureNamespaceAllowed(ns);
        _policy.EnsureDeploymentAllowed(ns, name);

        _logger.LogInformation(
            "Restarting {Namespace}/{Name} by {RequestedBy} [{CorrelationId}]",
            ns, name, requestedBy, correlationId);

        try
        {
            await _k8s.PatchDeploymentRestartAsync(ns, name, DateTimeOffset.UtcNow, ct);
            await _audit.RecordAsync(new AuditEntry
            {
                CorrelationId = correlationId,
                RequestedBy = requestedBy,
                Action = "RestartDeployment",
                Cluster = cluster,
                Namespace = ns,
                ResourceType = "Deployment",
                ResourceName = name,
                Reason = reason,
                Success = true
            });
            return OperationResult.Ok(correlationId);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to restart {Namespace}/{Name}", ns, name);
            await _audit.RecordAsync(new AuditEntry
            {
                CorrelationId = correlationId,
                RequestedBy = requestedBy,
                Action = "RestartDeployment",
                Cluster = cluster,
                Namespace = ns,
                ResourceType = "Deployment",
                ResourceName = name,
                Reason = reason,
                Success = false,
                ErrorMessage = ex.Message
            });
            return OperationResult.Fail(correlationId, ex.Message);
        }
    }

    public async Task<OperationResult> CreateAsync(string cluster, string ns, CreateDeploymentDto dto, CancellationToken ct = default)
    {
        _policy.EnsureNamespaceAllowed(ns);
        _policy.EnsureReplicaLimit(dto.Replicas);

        _logger.LogInformation("Creating deployment {Namespace}/{Name} by {RequestedBy} [{CorrelationId}]",
            ns, dto.Name, dto.RequestedBy, dto.CorrelationId);

        try
        {
            var labels = dto.Labels.Count > 0 ? dto.Labels : new Dictionary<string, string> { ["app"] = dto.Name };
            var deployment = new V1Deployment
            {
                Metadata = new V1ObjectMeta { Name = dto.Name, NamespaceProperty = ns, Labels = labels },
                Spec = new V1DeploymentSpec
                {
                    Replicas = dto.Replicas,
                    Selector = new V1LabelSelector { MatchLabels = labels },
                    Template = new V1PodTemplateSpec
                    {
                        Metadata = new V1ObjectMeta { Labels = labels },
                        Spec = new V1PodSpec
                        {
                            Containers =
                            [
                                new V1Container
                                {
                                    Name = dto.Name,
                                    Image = dto.Image,
                                    Env = dto.EnvVars.Select(e => new V1EnvVar { Name = e.Key, Value = e.Value }).ToList(),
                                    Ports = dto.Ports.Select(p => new V1ContainerPort
                                    {
                                        Name = p.Name,
                                        ContainerPort = p.ContainerPort,
                                        Protocol = p.Protocol
                                    }).ToList()
                                }
                            ]
                        }
                    }
                }
            };

            await _k8s.CreateDeploymentAsync(ns, deployment, ct);
            await _audit.RecordAsync(new AuditEntry
            {
                CorrelationId = dto.CorrelationId,
                RequestedBy = dto.RequestedBy,
                Action = "CreateDeployment",
                Cluster = cluster,
                Namespace = ns,
                ResourceType = "Deployment",
                ResourceName = dto.Name,
                Reason = dto.Reason,
                Success = true
            }, ct);
            return OperationResult.Ok(dto.CorrelationId);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to create deployment {Namespace}/{Name}", ns, dto.Name);
            await _audit.RecordAsync(new AuditEntry
            {
                CorrelationId = dto.CorrelationId,
                RequestedBy = dto.RequestedBy,
                Action = "CreateDeployment",
                Cluster = cluster,
                Namespace = ns,
                ResourceType = "Deployment",
                ResourceName = dto.Name,
                Reason = dto.Reason,
                Success = false,
                ErrorMessage = ex.Message
            }, ct);
            return OperationResult.Fail(dto.CorrelationId, ex.Message);
        }
    }

    public async Task<OperationResult> DeleteAsync(string cluster, string ns, string name, string requestedBy, string reason, string correlationId, CancellationToken ct = default)
    {
        _policy.EnsureNamespaceAllowed(ns);
        _policy.EnsureDeploymentAllowed(ns, name);

        _logger.LogInformation("Deleting deployment {Namespace}/{Name} by {RequestedBy} [{CorrelationId}]",
            ns, name, requestedBy, correlationId);

        try
        {
            await _k8s.DeleteDeploymentAsync(ns, name, ct);
            await _audit.RecordAsync(new AuditEntry
            {
                CorrelationId = correlationId,
                RequestedBy = requestedBy,
                Action = "DeleteDeployment",
                Cluster = cluster,
                Namespace = ns,
                ResourceType = "Deployment",
                ResourceName = name,
                Reason = reason,
                Success = true
            }, ct);
            return OperationResult.Ok(correlationId);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to delete deployment {Namespace}/{Name}", ns, name);
            await _audit.RecordAsync(new AuditEntry
            {
                CorrelationId = correlationId,
                RequestedBy = requestedBy,
                Action = "DeleteDeployment",
                Cluster = cluster,
                Namespace = ns,
                ResourceType = "Deployment",
                ResourceName = name,
                Reason = reason,
                Success = false,
                ErrorMessage = ex.Message
            }, ct);
            return OperationResult.Fail(correlationId, ex.Message);
        }
    }

    private static DeploymentStatusDto MapToDto(k8s.Models.V1Deployment d) => new()
    {
        Name = d.Metadata.Name,
        Namespace = d.Metadata.NamespaceProperty,
        DesiredReplicas = d.Spec?.Replicas ?? 0,
        ReadyReplicas = d.Status?.ReadyReplicas ?? 0,
        AvailableReplicas = d.Status?.AvailableReplicas ?? 0,
        Strategy = d.Spec?.Strategy?.Type ?? "Unknown",
        Status = d.Status?.Conditions?.FirstOrDefault()?.Type ?? "Unknown"
    };
}
