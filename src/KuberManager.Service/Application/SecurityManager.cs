using k8s.Models;
using KuberManager.Service.Application.DTOs;
using KuberManager.Service.Domain;
using KuberManager.Service.Infrastructure.Audit;
using KuberManager.Service.Infrastructure.Kubernetes;
using KuberManager.Service.Infrastructure.Policy;

namespace KuberManager.Service.Application;

// ──── Interfaces ─────────────────────────────────────────────────────────────

public interface IServiceAccountManager
{
    Task<IReadOnlyList<ServiceAccountDto>> ListAsync(string cluster, string ns, CancellationToken ct = default);
    Task<ServiceAccountDto> GetAsync(string cluster, string ns, string name, CancellationToken ct = default);
    Task<OperationResult> CreateAsync(CreateServiceAccountDto dto, CancellationToken ct = default);
    Task<OperationResult> DeleteAsync(string cluster, string ns, string name, string requestedBy, string reason, string correlationId, CancellationToken ct = default);
}

public interface IRbacManager
{
    // Roles
    Task<IReadOnlyList<RoleDto>> ListRolesAsync(string cluster, string ns, CancellationToken ct = default);
    Task<RoleDto> GetRoleAsync(string cluster, string ns, string name, CancellationToken ct = default);
    Task<OperationResult> CreateRoleAsync(CreateRoleDto dto, CancellationToken ct = default);
    Task<OperationResult> DeleteRoleAsync(string cluster, string ns, string name, string requestedBy, string reason, string correlationId, CancellationToken ct = default);

    // ClusterRoles
    Task<IReadOnlyList<ClusterRoleDto>> ListClusterRolesAsync(string cluster, CancellationToken ct = default);
    Task<ClusterRoleDto> GetClusterRoleAsync(string cluster, string name, CancellationToken ct = default);
    Task<OperationResult> CreateClusterRoleAsync(CreateClusterRoleDto dto, CancellationToken ct = default);
    Task<OperationResult> DeleteClusterRoleAsync(string cluster, string name, string requestedBy, string reason, string correlationId, CancellationToken ct = default);

    // RoleBindings
    Task<IReadOnlyList<RoleBindingDto>> ListRoleBindingsAsync(string cluster, string ns, CancellationToken ct = default);
    Task<RoleBindingDto> GetRoleBindingAsync(string cluster, string ns, string name, CancellationToken ct = default);
    Task<OperationResult> CreateRoleBindingAsync(CreateRoleBindingDto dto, CancellationToken ct = default);
    Task<OperationResult> DeleteRoleBindingAsync(string cluster, string ns, string name, string requestedBy, string reason, string correlationId, CancellationToken ct = default);

    // ClusterRoleBindings
    Task<IReadOnlyList<ClusterRoleBindingDto>> ListClusterRoleBindingsAsync(string cluster, CancellationToken ct = default);
    Task<ClusterRoleBindingDto> GetClusterRoleBindingAsync(string cluster, string name, CancellationToken ct = default);
    Task<OperationResult> CreateClusterRoleBindingAsync(CreateClusterRoleBindingDto dto, CancellationToken ct = default);
    Task<OperationResult> DeleteClusterRoleBindingAsync(string cluster, string name, string requestedBy, string reason, string correlationId, CancellationToken ct = default);
}

public interface INetworkPolicyManager
{
    Task<IReadOnlyList<NetworkPolicyDto>> ListAsync(string cluster, string ns, CancellationToken ct = default);
    Task<NetworkPolicyDto> GetAsync(string cluster, string ns, string name, CancellationToken ct = default);
    Task<OperationResult> CreateAsync(CreateNetworkPolicyDto dto, CancellationToken ct = default);
    Task<OperationResult> DeleteAsync(string cluster, string ns, string name, string requestedBy, string reason, string correlationId, CancellationToken ct = default);
}

// ──── ServiceAccountManager ──────────────────────────────────────────────────

public sealed class ServiceAccountManager : IServiceAccountManager
{
    private readonly IKubernetesFacadeFactory _k8s;
    private readonly IOperationPolicy _policy;
    private readonly IAuditService _audit;
    private readonly ILogger<ServiceAccountManager> _logger;

    public ServiceAccountManager(IKubernetesFacadeFactory k8s, IOperationPolicy policy, IAuditService audit, ILogger<ServiceAccountManager> logger)
    { _k8s = k8s; _policy = policy; _audit = audit; _logger = logger; }

    public async Task<IReadOnlyList<ServiceAccountDto>> ListAsync(string cluster, string ns, CancellationToken ct = default)
    {
        _policy.EnsureNamespaceAllowed(ns);
        return (await _k8s.For(cluster).ListServiceAccountsAsync(ns, ct)).Select(MapToDto).ToList();
    }

    public async Task<ServiceAccountDto> GetAsync(string cluster, string ns, string name, CancellationToken ct = default)
    {
        _policy.EnsureNamespaceAllowed(ns);
        return MapToDto(await _k8s.For(cluster).GetServiceAccountAsync(ns, name, ct));
    }

    public async Task<OperationResult> CreateAsync(CreateServiceAccountDto dto, CancellationToken ct = default)
    {
        _policy.EnsureNamespaceAllowed(dto.Namespace);
        _logger.LogInformation("Creating serviceaccount {Namespace}/{Name}", dto.Namespace, dto.Name);
        try
        {
            var sa = new V1ServiceAccount
            {
                Metadata = new V1ObjectMeta
                {
                    Name = dto.Name,
                    NamespaceProperty = dto.Namespace,
                    Annotations = dto.Annotations.Count > 0 ? dto.Annotations : null
                }
            };
            await _k8s.For(dto.Cluster).CreateServiceAccountAsync(dto.Namespace, sa, ct);
            await _audit.RecordAsync(new AuditEntry { CorrelationId = dto.CorrelationId, RequestedBy = dto.RequestedBy, Action = "CreateServiceAccount", Cluster = dto.Cluster, Namespace = dto.Namespace, ResourceType = "ServiceAccount", ResourceName = dto.Name, Reason = dto.Reason, Success = true }, ct);
            return OperationResult.Ok(dto.CorrelationId);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to create serviceaccount {Namespace}/{Name}", dto.Namespace, dto.Name);
            await _audit.RecordAsync(new AuditEntry { CorrelationId = dto.CorrelationId, RequestedBy = dto.RequestedBy, Action = "CreateServiceAccount", Cluster = dto.Cluster, Namespace = dto.Namespace, ResourceType = "ServiceAccount", ResourceName = dto.Name, Reason = dto.Reason, Success = false, ErrorMessage = ex.Message }, ct);
            return OperationResult.Fail(dto.CorrelationId, ex.Message);
        }
    }

    public async Task<OperationResult> DeleteAsync(string cluster, string ns, string name, string requestedBy, string reason, string correlationId, CancellationToken ct = default)
    {
        _policy.EnsureNamespaceAllowed(ns);
        try
        {
            await _k8s.For(cluster).DeleteServiceAccountAsync(ns, name, ct);
            await _audit.RecordAsync(new AuditEntry { CorrelationId = correlationId, RequestedBy = requestedBy, Action = "DeleteServiceAccount", Cluster = cluster, Namespace = ns, ResourceType = "ServiceAccount", ResourceName = name, Reason = reason, Success = true }, ct);
            return OperationResult.Ok(correlationId);
        }
        catch (Exception ex)
        {
            await _audit.RecordAsync(new AuditEntry { CorrelationId = correlationId, RequestedBy = requestedBy, Action = "DeleteServiceAccount", Cluster = cluster, Namespace = ns, ResourceType = "ServiceAccount", ResourceName = name, Reason = reason, Success = false, ErrorMessage = ex.Message }, ct);
            return OperationResult.Fail(correlationId, ex.Message);
        }
    }

    private static ServiceAccountDto MapToDto(V1ServiceAccount sa) => new()
    {
        Name = sa.Metadata.Name,
        Namespace = sa.Metadata.NamespaceProperty,
        Secrets = sa.Secrets?.Select(s => s.Name ?? string.Empty).ToList() ?? new(),
        CreatedAt = sa.Metadata.CreationTimestamp?.ToString("O") ?? string.Empty
    };
}

// ──── RbacManager ────────────────────────────────────────────────────────────

public sealed class RbacManager : IRbacManager
{
    private readonly IKubernetesFacadeFactory _k8s;
    private readonly IAuditService _audit;
    private readonly ILogger<RbacManager> _logger;

    public RbacManager(IKubernetesFacadeFactory k8s, IAuditService audit, ILogger<RbacManager> logger)
    { _k8s = k8s; _audit = audit; _logger = logger; }

    // ─ Roles ─
    public async Task<IReadOnlyList<RoleDto>> ListRolesAsync(string cluster, string ns, CancellationToken ct = default)
        => (await _k8s.For(cluster).ListRolesAsync(ns, ct)).Select(MapRole).ToList();

    public async Task<RoleDto> GetRoleAsync(string cluster, string ns, string name, CancellationToken ct = default)
        => MapRole(await _k8s.For(cluster).GetRoleAsync(ns, name, ct));

    public async Task<OperationResult> CreateRoleAsync(CreateRoleDto dto, CancellationToken ct = default)
    {
        _logger.LogInformation("Creating role {Namespace}/{Name}", dto.Namespace, dto.Name);
        try
        {
            var role = new V1Role
            {
                Metadata = new V1ObjectMeta { Name = dto.Name, NamespaceProperty = dto.Namespace },
                Rules = dto.Rules.Select(r => new V1PolicyRule
                {
                    ApiGroups = r.ApiGroups,
                    Resources = r.Resources,
                    Verbs = r.Verbs,
                    ResourceNames = r.ResourceNames.Count > 0 ? r.ResourceNames : null
                }).ToList()
            };
            await _k8s.For(dto.Cluster).CreateRoleAsync(dto.Namespace, role, ct);
            await _audit.RecordAsync(new AuditEntry { CorrelationId = dto.CorrelationId, RequestedBy = dto.RequestedBy, Action = "CreateRole", Cluster = dto.Cluster, Namespace = dto.Namespace, ResourceType = "Role", ResourceName = dto.Name, Reason = dto.Reason, Success = true }, ct);
            return OperationResult.Ok(dto.CorrelationId);
        }
        catch (Exception ex)
        {
            await _audit.RecordAsync(new AuditEntry { CorrelationId = dto.CorrelationId, RequestedBy = dto.RequestedBy, Action = "CreateRole", Cluster = dto.Cluster, Namespace = dto.Namespace, ResourceType = "Role", ResourceName = dto.Name, Reason = dto.Reason, Success = false, ErrorMessage = ex.Message }, ct);
            return OperationResult.Fail(dto.CorrelationId, ex.Message);
        }
    }

    public async Task<OperationResult> DeleteRoleAsync(string cluster, string ns, string name, string requestedBy, string reason, string correlationId, CancellationToken ct = default)
    {
        try
        {
            await _k8s.For(cluster).DeleteRoleAsync(ns, name, ct);
            await _audit.RecordAsync(new AuditEntry { CorrelationId = correlationId, RequestedBy = requestedBy, Action = "DeleteRole", Cluster = cluster, Namespace = ns, ResourceType = "Role", ResourceName = name, Reason = reason, Success = true }, ct);
            return OperationResult.Ok(correlationId);
        }
        catch (Exception ex)
        {
            await _audit.RecordAsync(new AuditEntry { CorrelationId = correlationId, RequestedBy = requestedBy, Action = "DeleteRole", Cluster = cluster, Namespace = ns, ResourceType = "Role", ResourceName = name, Reason = reason, Success = false, ErrorMessage = ex.Message }, ct);
            return OperationResult.Fail(correlationId, ex.Message);
        }
    }

    // ─ ClusterRoles ─
    public async Task<IReadOnlyList<ClusterRoleDto>> ListClusterRolesAsync(string cluster, CancellationToken ct = default)
        => (await _k8s.For(cluster).ListClusterRolesAsync(ct)).Select(MapClusterRole).ToList();

    public async Task<ClusterRoleDto> GetClusterRoleAsync(string cluster, string name, CancellationToken ct = default)
        => MapClusterRole(await _k8s.For(cluster).GetClusterRoleAsync(name, ct));

    public async Task<OperationResult> CreateClusterRoleAsync(CreateClusterRoleDto dto, CancellationToken ct = default)
    {
        _logger.LogInformation("Creating clusterrole {Name}", dto.Name);
        try
        {
            var cr = new V1ClusterRole
            {
                Metadata = new V1ObjectMeta { Name = dto.Name },
                Rules = dto.Rules.Select(r => new V1PolicyRule
                {
                    ApiGroups = r.ApiGroups,
                    Resources = r.Resources,
                    Verbs = r.Verbs,
                    ResourceNames = r.ResourceNames.Count > 0 ? r.ResourceNames : null
                }).ToList()
            };
            await _k8s.For(dto.Cluster).CreateClusterRoleAsync(cr, ct);
            await _audit.RecordAsync(new AuditEntry { CorrelationId = dto.CorrelationId, RequestedBy = dto.RequestedBy, Action = "CreateClusterRole", Cluster = dto.Cluster, ResourceType = "ClusterRole", ResourceName = dto.Name, Reason = dto.Reason, Success = true }, ct);
            return OperationResult.Ok(dto.CorrelationId);
        }
        catch (Exception ex)
        {
            await _audit.RecordAsync(new AuditEntry { CorrelationId = dto.CorrelationId, RequestedBy = dto.RequestedBy, Action = "CreateClusterRole", Cluster = dto.Cluster, ResourceType = "ClusterRole", ResourceName = dto.Name, Reason = dto.Reason, Success = false, ErrorMessage = ex.Message }, ct);
            return OperationResult.Fail(dto.CorrelationId, ex.Message);
        }
    }

    public async Task<OperationResult> DeleteClusterRoleAsync(string cluster, string name, string requestedBy, string reason, string correlationId, CancellationToken ct = default)
    {
        try
        {
            await _k8s.For(cluster).DeleteClusterRoleAsync(name, ct);
            await _audit.RecordAsync(new AuditEntry { CorrelationId = correlationId, RequestedBy = requestedBy, Action = "DeleteClusterRole", Cluster = cluster, ResourceType = "ClusterRole", ResourceName = name, Reason = reason, Success = true }, ct);
            return OperationResult.Ok(correlationId);
        }
        catch (Exception ex)
        {
            await _audit.RecordAsync(new AuditEntry { CorrelationId = correlationId, RequestedBy = requestedBy, Action = "DeleteClusterRole", Cluster = cluster, ResourceType = "ClusterRole", ResourceName = name, Reason = reason, Success = false, ErrorMessage = ex.Message }, ct);
            return OperationResult.Fail(correlationId, ex.Message);
        }
    }

    // ─ RoleBindings ─
    public async Task<IReadOnlyList<RoleBindingDto>> ListRoleBindingsAsync(string cluster, string ns, CancellationToken ct = default)
        => (await _k8s.For(cluster).ListRoleBindingsAsync(ns, ct)).Select(MapRoleBinding).ToList();

    public async Task<RoleBindingDto> GetRoleBindingAsync(string cluster, string ns, string name, CancellationToken ct = default)
        => MapRoleBinding(await _k8s.For(cluster).GetRoleBindingAsync(ns, name, ct));

    public async Task<OperationResult> CreateRoleBindingAsync(CreateRoleBindingDto dto, CancellationToken ct = default)
    {
        _logger.LogInformation("Creating rolebinding {Namespace}/{Name}", dto.Namespace, dto.Name);
        try
        {
            var binding = new V1RoleBinding
            {
                Metadata = new V1ObjectMeta { Name = dto.Name, NamespaceProperty = dto.Namespace },
                RoleRef = new V1RoleRef { ApiGroup = "rbac.authorization.k8s.io", Kind = dto.RoleKind, Name = dto.RoleName },
                Subjects = dto.Subjects.Select(s => new Rbacv1Subject { Kind = s.Kind, Name = s.Name, NamespaceProperty = string.IsNullOrEmpty(s.Namespace) ? null : s.Namespace, ApiGroup = string.IsNullOrEmpty(s.ApiGroup) ? null : s.ApiGroup }).ToList()
            };
            await _k8s.For(dto.Cluster).CreateRoleBindingAsync(dto.Namespace, binding, ct);
            await _audit.RecordAsync(new AuditEntry { CorrelationId = dto.CorrelationId, RequestedBy = dto.RequestedBy, Action = "CreateRoleBinding", Cluster = dto.Cluster, Namespace = dto.Namespace, ResourceType = "RoleBinding", ResourceName = dto.Name, Reason = dto.Reason, Success = true }, ct);
            return OperationResult.Ok(dto.CorrelationId);
        }
        catch (Exception ex)
        {
            await _audit.RecordAsync(new AuditEntry { CorrelationId = dto.CorrelationId, RequestedBy = dto.RequestedBy, Action = "CreateRoleBinding", Cluster = dto.Cluster, Namespace = dto.Namespace, ResourceType = "RoleBinding", ResourceName = dto.Name, Reason = dto.Reason, Success = false, ErrorMessage = ex.Message }, ct);
            return OperationResult.Fail(dto.CorrelationId, ex.Message);
        }
    }

    public async Task<OperationResult> DeleteRoleBindingAsync(string cluster, string ns, string name, string requestedBy, string reason, string correlationId, CancellationToken ct = default)
    {
        try
        {
            await _k8s.For(cluster).DeleteRoleBindingAsync(ns, name, ct);
            await _audit.RecordAsync(new AuditEntry { CorrelationId = correlationId, RequestedBy = requestedBy, Action = "DeleteRoleBinding", Cluster = cluster, Namespace = ns, ResourceType = "RoleBinding", ResourceName = name, Reason = reason, Success = true }, ct);
            return OperationResult.Ok(correlationId);
        }
        catch (Exception ex)
        {
            await _audit.RecordAsync(new AuditEntry { CorrelationId = correlationId, RequestedBy = requestedBy, Action = "DeleteRoleBinding", Cluster = cluster, Namespace = ns, ResourceType = "RoleBinding", ResourceName = name, Reason = reason, Success = false, ErrorMessage = ex.Message }, ct);
            return OperationResult.Fail(correlationId, ex.Message);
        }
    }

    // ─ ClusterRoleBindings ─
    public async Task<IReadOnlyList<ClusterRoleBindingDto>> ListClusterRoleBindingsAsync(string cluster, CancellationToken ct = default)
        => (await _k8s.For(cluster).ListClusterRoleBindingsAsync(ct)).Select(MapClusterRoleBinding).ToList();

    public async Task<ClusterRoleBindingDto> GetClusterRoleBindingAsync(string cluster, string name, CancellationToken ct = default)
        => MapClusterRoleBinding(await _k8s.For(cluster).GetClusterRoleBindingAsync(name, ct));

    public async Task<OperationResult> CreateClusterRoleBindingAsync(CreateClusterRoleBindingDto dto, CancellationToken ct = default)
    {
        _logger.LogInformation("Creating clusterrolebinding {Name}", dto.Name);
        try
        {
            var binding = new V1ClusterRoleBinding
            {
                Metadata = new V1ObjectMeta { Name = dto.Name },
                RoleRef = new V1RoleRef { ApiGroup = "rbac.authorization.k8s.io", Kind = "ClusterRole", Name = dto.RoleName },
                Subjects = dto.Subjects.Select(s => new Rbacv1Subject { Kind = s.Kind, Name = s.Name, NamespaceProperty = string.IsNullOrEmpty(s.Namespace) ? null : s.Namespace, ApiGroup = string.IsNullOrEmpty(s.ApiGroup) ? null : s.ApiGroup }).ToList()
            };
            await _k8s.For(dto.Cluster).CreateClusterRoleBindingAsync(binding, ct);
            await _audit.RecordAsync(new AuditEntry { CorrelationId = dto.CorrelationId, RequestedBy = dto.RequestedBy, Action = "CreateClusterRoleBinding", Cluster = dto.Cluster, ResourceType = "ClusterRoleBinding", ResourceName = dto.Name, Reason = dto.Reason, Success = true }, ct);
            return OperationResult.Ok(dto.CorrelationId);
        }
        catch (Exception ex)
        {
            await _audit.RecordAsync(new AuditEntry { CorrelationId = dto.CorrelationId, RequestedBy = dto.RequestedBy, Action = "CreateClusterRoleBinding", Cluster = dto.Cluster, ResourceType = "ClusterRoleBinding", ResourceName = dto.Name, Reason = dto.Reason, Success = false, ErrorMessage = ex.Message }, ct);
            return OperationResult.Fail(dto.CorrelationId, ex.Message);
        }
    }

    public async Task<OperationResult> DeleteClusterRoleBindingAsync(string cluster, string name, string requestedBy, string reason, string correlationId, CancellationToken ct = default)
    {
        try
        {
            await _k8s.For(cluster).DeleteClusterRoleBindingAsync(name, ct);
            await _audit.RecordAsync(new AuditEntry { CorrelationId = correlationId, RequestedBy = requestedBy, Action = "DeleteClusterRoleBinding", Cluster = cluster, ResourceType = "ClusterRoleBinding", ResourceName = name, Reason = reason, Success = true }, ct);
            return OperationResult.Ok(correlationId);
        }
        catch (Exception ex)
        {
            await _audit.RecordAsync(new AuditEntry { CorrelationId = correlationId, RequestedBy = requestedBy, Action = "DeleteClusterRoleBinding", Cluster = cluster, ResourceType = "ClusterRoleBinding", ResourceName = name, Reason = reason, Success = false, ErrorMessage = ex.Message }, ct);
            return OperationResult.Fail(correlationId, ex.Message);
        }
    }

    // ─ Mappers ─
    private static PolicyRuleDto MapRule(V1PolicyRule r) => new()
    {
        ApiGroups = r.ApiGroups?.ToList() ?? new(),
        Resources = r.Resources?.ToList() ?? new(),
        Verbs = r.Verbs?.ToList() ?? new(),
        ResourceNames = r.ResourceNames?.ToList() ?? new()
    };

    private static RoleDto MapRole(V1Role r) => new()
    {
        Name = r.Metadata.Name, Namespace = r.Metadata.NamespaceProperty,
        Rules = r.Rules?.Select(MapRule).ToList() ?? new(),
        CreatedAt = r.Metadata.CreationTimestamp?.ToString("O") ?? string.Empty
    };

    private static ClusterRoleDto MapClusterRole(V1ClusterRole cr) => new()
    {
        Name = cr.Metadata.Name,
        Rules = cr.Rules?.Select(MapRule).ToList() ?? new(),
        CreatedAt = cr.Metadata.CreationTimestamp?.ToString("O") ?? string.Empty
    };

    private static SubjectDto MapSubject(Rbacv1Subject s) => new()
    {
        Kind = s.Kind, Name = s.Name,
        Namespace = s.NamespaceProperty ?? string.Empty,
        ApiGroup = s.ApiGroup ?? string.Empty
    };

    private static RoleBindingDto MapRoleBinding(V1RoleBinding rb) => new()
    {
        Name = rb.Metadata.Name, Namespace = rb.Metadata.NamespaceProperty,
        RoleKind = rb.RoleRef?.Kind ?? string.Empty,
        RoleName = rb.RoleRef?.Name ?? string.Empty,
        Subjects = rb.Subjects?.Select(MapSubject).ToList() ?? new(),
        CreatedAt = rb.Metadata.CreationTimestamp?.ToString("O") ?? string.Empty
    };

    private static ClusterRoleBindingDto MapClusterRoleBinding(V1ClusterRoleBinding crb) => new()
    {
        Name = crb.Metadata.Name,
        RoleName = crb.RoleRef?.Name ?? string.Empty,
        Subjects = crb.Subjects?.Select(MapSubject).ToList() ?? new(),
        CreatedAt = crb.Metadata.CreationTimestamp?.ToString("O") ?? string.Empty
    };
}

// ──── NetworkPolicyManager ───────────────────────────────────────────────────

public sealed class NetworkPolicyManager : INetworkPolicyManager
{
    private readonly IKubernetesFacadeFactory _k8s;
    private readonly IOperationPolicy _policy;
    private readonly IAuditService _audit;
    private readonly ILogger<NetworkPolicyManager> _logger;

    public NetworkPolicyManager(IKubernetesFacadeFactory k8s, IOperationPolicy policy, IAuditService audit, ILogger<NetworkPolicyManager> logger)
    { _k8s = k8s; _policy = policy; _audit = audit; _logger = logger; }

    public async Task<IReadOnlyList<NetworkPolicyDto>> ListAsync(string cluster, string ns, CancellationToken ct = default)
    {
        _policy.EnsureNamespaceAllowed(ns);
        return (await _k8s.For(cluster).ListNetworkPoliciesAsync(ns, ct)).Select(MapToDto).ToList();
    }

    public async Task<NetworkPolicyDto> GetAsync(string cluster, string ns, string name, CancellationToken ct = default)
    {
        _policy.EnsureNamespaceAllowed(ns);
        return MapToDto(await _k8s.For(cluster).GetNetworkPolicyAsync(ns, name, ct));
    }

    public async Task<OperationResult> CreateAsync(CreateNetworkPolicyDto dto, CancellationToken ct = default)
    {
        _policy.EnsureNamespaceAllowed(dto.Namespace);
        _logger.LogInformation("Creating networkpolicy {Namespace}/{Name}", dto.Namespace, dto.Name);
        try
        {
            var ingressRules = BuildIngressRules(dto);
            var egressRules = BuildEgressRules(dto);

            var np = new V1NetworkPolicy
            {
                Metadata = new V1ObjectMeta { Name = dto.Name, NamespaceProperty = dto.Namespace },
                Spec = new V1NetworkPolicySpec
                {
                    PodSelector = new V1LabelSelector { MatchLabels = dto.PodSelector.Count > 0 ? dto.PodSelector : null },
                    PolicyTypes = dto.PolicyTypes,
                    Ingress = ingressRules.Count > 0 ? ingressRules : null,
                    Egress = egressRules.Count > 0 ? egressRules : null
                }
            };

            await _k8s.For(dto.Cluster).CreateNetworkPolicyAsync(dto.Namespace, np, ct);
            await _audit.RecordAsync(new AuditEntry { CorrelationId = dto.CorrelationId, RequestedBy = dto.RequestedBy, Action = "CreateNetworkPolicy", Cluster = dto.Cluster, Namespace = dto.Namespace, ResourceType = "NetworkPolicy", ResourceName = dto.Name, Reason = dto.Reason, Success = true }, ct);
            return OperationResult.Ok(dto.CorrelationId);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to create networkpolicy {Namespace}/{Name}", dto.Namespace, dto.Name);
            await _audit.RecordAsync(new AuditEntry { CorrelationId = dto.CorrelationId, RequestedBy = dto.RequestedBy, Action = "CreateNetworkPolicy", Cluster = dto.Cluster, Namespace = dto.Namespace, ResourceType = "NetworkPolicy", ResourceName = dto.Name, Reason = dto.Reason, Success = false, ErrorMessage = ex.Message }, ct);
            return OperationResult.Fail(dto.CorrelationId, ex.Message);
        }
    }

    public async Task<OperationResult> DeleteAsync(string cluster, string ns, string name, string requestedBy, string reason, string correlationId, CancellationToken ct = default)
    {
        _policy.EnsureNamespaceAllowed(ns);
        try
        {
            await _k8s.For(cluster).DeleteNetworkPolicyAsync(ns, name, ct);
            await _audit.RecordAsync(new AuditEntry { CorrelationId = correlationId, RequestedBy = requestedBy, Action = "DeleteNetworkPolicy", Cluster = cluster, Namespace = ns, ResourceType = "NetworkPolicy", ResourceName = name, Reason = reason, Success = true }, ct);
            return OperationResult.Ok(correlationId);
        }
        catch (Exception ex)
        {
            await _audit.RecordAsync(new AuditEntry { CorrelationId = correlationId, RequestedBy = requestedBy, Action = "DeleteNetworkPolicy", Cluster = cluster, Namespace = ns, ResourceType = "NetworkPolicy", ResourceName = name, Reason = reason, Success = false, ErrorMessage = ex.Message }, ct);
            return OperationResult.Fail(correlationId, ex.Message);
        }
    }

    private static List<V1NetworkPolicyIngressRule> BuildIngressRules(CreateNetworkPolicyDto dto)
    {
        if (dto.DenyAllIngress) return new List<V1NetworkPolicyIngressRule>();
        if (dto.AllowIngressFrom.Count == 0) return new List<V1NetworkPolicyIngressRule>();

        return new List<V1NetworkPolicyIngressRule>
        {
            new V1NetworkPolicyIngressRule
            {
                FromProperty = dto.AllowIngressFrom.Select(peer => new V1NetworkPolicyPeer
                {
                    NamespaceSelector = peer.NamespaceSelector.Count > 0 ? new V1LabelSelector { MatchLabels = peer.NamespaceSelector } : null,
                    PodSelector = peer.PodSelector.Count > 0 ? new V1LabelSelector { MatchLabels = peer.PodSelector } : null,
                    IpBlock = !string.IsNullOrEmpty(peer.IpBlock) ? new V1IPBlock { Cidr = peer.IpBlock } : null
                }).ToList()
            }
        };
    }

    private static List<V1NetworkPolicyEgressRule> BuildEgressRules(CreateNetworkPolicyDto dto)
    {
        if (dto.DenyAllEgress) return new List<V1NetworkPolicyEgressRule>();
        if (dto.AllowEgressTo.Count == 0) return new List<V1NetworkPolicyEgressRule>();

        return new List<V1NetworkPolicyEgressRule>
        {
            new V1NetworkPolicyEgressRule
            {
                To = dto.AllowEgressTo.Select(peer => new V1NetworkPolicyPeer
                {
                    NamespaceSelector = peer.NamespaceSelector.Count > 0 ? new V1LabelSelector { MatchLabels = peer.NamespaceSelector } : null,
                    PodSelector = peer.PodSelector.Count > 0 ? new V1LabelSelector { MatchLabels = peer.PodSelector } : null,
                    IpBlock = !string.IsNullOrEmpty(peer.IpBlock) ? new V1IPBlock { Cidr = peer.IpBlock } : null
                }).ToList()
            }
        };
    }

    private static NetworkPolicyDto MapToDto(V1NetworkPolicy np) => new()
    {
        Name = np.Metadata.Name,
        Namespace = np.Metadata.NamespaceProperty,
        PodSelectorLabels = np.Spec?.PodSelector?.MatchLabels?.Select(kv => $"{kv.Key}={kv.Value}").ToList() ?? new(),
        PolicyTypes = np.Spec?.PolicyTypes?.ToList() ?? new(),
        CreatedAt = np.Metadata.CreationTimestamp?.ToString("O") ?? string.Empty
    };
}
