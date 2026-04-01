using k8s.Models;
using KuberManager.Service.Application.DTOs;
using KuberManager.Service.Domain;
using KuberManager.Service.Infrastructure.Audit;
using KuberManager.Service.Infrastructure.Kubernetes;
using KuberManager.Service.Infrastructure.Policy;

namespace KuberManager.Service.Application;

public sealed class ServiceManager : IServiceManager
{
    private readonly IKubernetesFacadeFactory _k8sFactory;
    private readonly IOperationPolicy _policy;
    private readonly IAuditService _audit;
    private readonly ILogger<ServiceManager> _logger;

    public ServiceManager(IKubernetesFacadeFactory k8sFactory, IOperationPolicy policy, IAuditService audit, ILogger<ServiceManager> logger)
    {
        _k8sFactory = k8sFactory;
        _policy = policy;
        _audit = audit;
        _logger = logger;
    }

    public async Task<IReadOnlyList<ServiceDto>> ListAsync(string cluster, string ns, CancellationToken ct = default)
    {
        _policy.EnsureNamespaceAllowed(ns);
        var list = await _k8sFactory.For(cluster).ListServicesAsync(ns, ct);
        return list.Select(MapToDto).ToList();
    }

    public async Task<ServiceDto> GetAsync(string cluster, string ns, string name, CancellationToken ct = default)
    {
        _policy.EnsureNamespaceAllowed(ns);
        var svc = await _k8sFactory.For(cluster).GetServiceAsync(ns, name, ct);
        return MapToDto(svc);
    }

    public async Task<OperationResult> CreateAsync(CreateServiceDto dto, CancellationToken ct = default)
    {
        _policy.EnsureNamespaceAllowed(dto.Namespace);
        _logger.LogInformation("Creating service {Namespace}/{Name} by {RequestedBy} [{CorrelationId}]", dto.Namespace, dto.Name, dto.RequestedBy, dto.CorrelationId);
        try
        {
            var svc = new V1Service
            {
                Metadata = new V1ObjectMeta { Name = dto.Name, NamespaceProperty = dto.Namespace },
                Spec = new V1ServiceSpec
                {
                    Type = dto.Type,
                    Selector = dto.Selector,
                    Ports = dto.Ports.Select(p => new V1ServicePort
                    {
                        Name = p.Name,
                        Port = p.Port,
                        TargetPort = p.TargetPort,
                        Protocol = p.Protocol,
                        NodePort = p.NodePort > 0 ? p.NodePort : null
                    }).ToList()
                }
            };

            await _k8sFactory.For(dto.Cluster).CreateServiceAsync(dto.Namespace, svc, ct);
            
            await _audit.RecordAsync(new AuditEntry
            {
                CorrelationId = dto.CorrelationId, RequestedBy = dto.RequestedBy,
                Action = "CreateService", Cluster = dto.Cluster, Namespace = dto.Namespace,
                ResourceType = "Service", ResourceName = dto.Name, Reason = dto.Reason, Success = true
            }, ct);
            
            return OperationResult.Ok(dto.CorrelationId);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to create service {Namespace}/{Name}", dto.Namespace, dto.Name);
            await _audit.RecordAsync(new AuditEntry
            {
                CorrelationId = dto.CorrelationId, RequestedBy = dto.RequestedBy,
                Action = "CreateService", Cluster = dto.Cluster, Namespace = dto.Namespace,
                ResourceType = "Service", ResourceName = dto.Name, Reason = dto.Reason,
                Success = false, ErrorMessage = ex.Message
            }, ct);
            return OperationResult.Fail(dto.CorrelationId, ex.Message);
        }
    }

    public async Task<OperationResult> DeleteAsync(string cluster, string ns, string name, string requestedBy, string reason, string correlationId, CancellationToken ct = default)
    {
        _policy.EnsureNamespaceAllowed(ns);
        _logger.LogInformation("Deleting service {Namespace}/{Name} by {RequestedBy} [{CorrelationId}]", ns, name, requestedBy, correlationId);
        try
        {
            await _k8sFactory.For(cluster).DeleteServiceAsync(ns, name, ct);
            
            await _audit.RecordAsync(new AuditEntry
            {
                CorrelationId = correlationId, RequestedBy = requestedBy,
                Action = "DeleteService", Cluster = cluster, Namespace = ns,
                ResourceType = "Service", ResourceName = name, Reason = reason, Success = true
            }, ct);
            
            return OperationResult.Ok(correlationId);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to delete service {Namespace}/{Name}", ns, name);
            await _audit.RecordAsync(new AuditEntry
            {
                CorrelationId = correlationId, RequestedBy = requestedBy,
                Action = "DeleteService", Cluster = cluster, Namespace = ns,
                ResourceType = "Service", ResourceName = name, Reason = reason,
                Success = false, ErrorMessage = ex.Message
            }, ct);
            return OperationResult.Fail(correlationId, ex.Message);
        }
    }

    private static ServiceDto MapToDto(V1Service svc) => new()
    {
        Name = svc.Metadata.Name,
        Namespace = svc.Metadata.NamespaceProperty,
        Type = svc.Spec?.Type ?? string.Empty,
        ClusterIp = svc.Spec?.ClusterIP ?? string.Empty,
        CreatedAt = svc.Metadata.CreationTimestamp?.ToString("O") ?? string.Empty,
        Ports = svc.Spec?.Ports?.Select(p => new ServicePortDto
        {
            Name = p.Name ?? string.Empty,
            Port = p.Port,
            TargetPort = int.TryParse(p.TargetPort?.Value, out var nStr) ? nStr : 0,
            Protocol = p.Protocol ?? "TCP",
            NodePort = p.NodePort ?? 0
        }).ToList() ?? new()
    };
}
