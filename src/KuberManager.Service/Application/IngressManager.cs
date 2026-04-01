using k8s.Models;
using KuberManager.Service.Application.DTOs;
using KuberManager.Service.Domain;
using KuberManager.Service.Infrastructure.Audit;
using KuberManager.Service.Infrastructure.Kubernetes;
using KuberManager.Service.Infrastructure.Policy;

namespace KuberManager.Service.Application;

public sealed class IngressManager : IIngressManager
{
    private readonly IKubernetesFacadeFactory _k8sFactory;
    private readonly IOperationPolicy _policy;
    private readonly IAuditService _audit;
    private readonly ILogger<IngressManager> _logger;

    public IngressManager(IKubernetesFacadeFactory k8sFactory, IOperationPolicy policy, IAuditService audit, ILogger<IngressManager> logger)
    {
        _k8sFactory = k8sFactory;
        _policy = policy;
        _audit = audit;
        _logger = logger;
    }

    public async Task<IReadOnlyList<IngressDto>> ListAsync(string cluster, string ns, CancellationToken ct = default)
    {
        _policy.EnsureNamespaceAllowed(ns);
        var list = await _k8sFactory.For(cluster).ListIngressesAsync(ns, ct);
        return list.Select(MapToDto).ToList();
    }

    public async Task<IngressDto> GetAsync(string cluster, string ns, string name, CancellationToken ct = default)
    {
        _policy.EnsureNamespaceAllowed(ns);
        var ingress = await _k8sFactory.For(cluster).GetIngressAsync(ns, name, ct);
        return MapToDto(ingress);
    }

    public async Task<OperationResult> CreateAsync(CreateIngressDto dto, CancellationToken ct = default)
    {
        _policy.EnsureNamespaceAllowed(dto.Namespace);
        _logger.LogInformation("Creating ingress {Namespace}/{Name} by {RequestedBy} [{CorrelationId}]", dto.Namespace, dto.Name, dto.RequestedBy, dto.CorrelationId);
        try
        {
            var ingress = new V1Ingress
            {
                Metadata = new V1ObjectMeta { Name = dto.Name, NamespaceProperty = dto.Namespace },
                Spec = new V1IngressSpec
                {
                    IngressClassName = string.IsNullOrEmpty(dto.IngressClass) ? null : dto.IngressClass,
                    Rules = dto.Rules.Select(r => new V1IngressRule
                    {
                        Host = r.Host,
                        Http = new V1HTTPIngressRuleValue
                        {
                            Paths = r.Paths.Select(p => new V1HTTPIngressPath
                            {
                                Path = p.Path,
                                PathType = p.PathType,
                                Backend = new V1IngressBackend
                                {
                                    Service = new V1IngressServiceBackend
                                    {
                                        Name = p.ServiceName,
                                        Port = new V1ServiceBackendPort { Number = p.ServicePort }
                                    }
                                }
                            }).ToList()
                        }
                    }).ToList()
                }
            };

            await _k8sFactory.For(dto.Cluster).CreateIngressAsync(dto.Namespace, ingress, ct);

            await _audit.RecordAsync(new AuditEntry
            {
                CorrelationId = dto.CorrelationId, RequestedBy = dto.RequestedBy,
                Action = "CreateIngress", Cluster = dto.Cluster, Namespace = dto.Namespace,
                ResourceType = "Ingress", ResourceName = dto.Name, Reason = dto.Reason, Success = true
            }, ct);

            return OperationResult.Ok(dto.CorrelationId);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to create ingress {Namespace}/{Name}", dto.Namespace, dto.Name);
            await _audit.RecordAsync(new AuditEntry
            {
                CorrelationId = dto.CorrelationId, RequestedBy = dto.RequestedBy,
                Action = "CreateIngress", Cluster = dto.Cluster, Namespace = dto.Namespace,
                ResourceType = "Ingress", ResourceName = dto.Name, Reason = dto.Reason,
                Success = false, ErrorMessage = ex.Message
            }, ct);
            return OperationResult.Fail(dto.CorrelationId, ex.Message);
        }
    }

    public async Task<OperationResult> DeleteAsync(string cluster, string ns, string name, string requestedBy, string reason, string correlationId, CancellationToken ct = default)
    {
        _policy.EnsureNamespaceAllowed(ns);
        _logger.LogInformation("Deleting ingress {Namespace}/{Name} by {RequestedBy} [{CorrelationId}]", ns, name, requestedBy, correlationId);
        try
        {
            await _k8sFactory.For(cluster).DeleteIngressAsync(ns, name, ct);

            await _audit.RecordAsync(new AuditEntry
            {
                CorrelationId = correlationId, RequestedBy = requestedBy,
                Action = "DeleteIngress", Cluster = cluster, Namespace = ns,
                ResourceType = "Ingress", ResourceName = name, Reason = reason, Success = true
            }, ct);

            return OperationResult.Ok(correlationId);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to delete ingress {Namespace}/{Name}", ns, name);
            await _audit.RecordAsync(new AuditEntry
            {
                CorrelationId = correlationId, RequestedBy = requestedBy,
                Action = "DeleteIngress", Cluster = cluster, Namespace = ns,
                ResourceType = "Ingress", ResourceName = name, Reason = reason,
                Success = false, ErrorMessage = ex.Message
            }, ct);
            return OperationResult.Fail(correlationId, ex.Message);
        }
    }

    private static IngressDto MapToDto(V1Ingress ingress)
    {
        return new IngressDto
        {
            Name = ingress.Metadata.Name,
            Namespace = ingress.Metadata.NamespaceProperty,
            IngressClass = ingress.Spec?.IngressClassName ?? string.Empty,
            CreatedAt = ingress.Metadata.CreationTimestamp?.ToString("O") ?? string.Empty,
            Rules = ingress.Spec?.Rules?.Select(r => new IngressRuleDto
            {
                Host = r.Host ?? string.Empty,
                Paths = r.Http?.Paths?.Select(p => new IngressPathDto
                {
                    Path = p.Path ?? string.Empty,
                    PathType = p.PathType ?? string.Empty,
                    ServiceName = p.Backend?.Service?.Name ?? string.Empty,
                    ServicePort = p.Backend?.Service?.Port?.Number ?? 0
                }).ToList() ?? new()
            }).ToList() ?? new()
        };
    }
}
