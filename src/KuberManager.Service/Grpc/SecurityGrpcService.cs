using Grpc.Core;
using KuberManager.Contracts;
using KuberManager.Service.Application;
using KuberManager.Service.Application.DTOs;
using KuberManager.Service.Infrastructure.Policy;

namespace KuberManager.Service.Grpc;

public sealed class ServiceAccountGrpcService : ServiceAccountService.ServiceAccountServiceBase
{
    private readonly IServiceAccountManager _manager;
    private readonly ILogger<ServiceAccountGrpcService> _logger;

    public ServiceAccountGrpcService(IServiceAccountManager manager, ILogger<ServiceAccountGrpcService> logger)
    { _manager = manager; _logger = logger; }

    public override async Task<ServiceAccountListReply> List(ListServiceAccountsRequest request, ServerCallContext context)
    {
        try
        {
            var items = await _manager.ListAsync(request.Cluster, request.Namespace, context.CancellationToken);
            var reply = new ServiceAccountListReply();
            reply.Items.AddRange(items.Select(Map));
            return reply;
        }
        catch (PolicyViolationException ex) { throw new RpcException(new Status(StatusCode.PermissionDenied, ex.Message)); }
        catch (Exception ex) { _logger.LogError(ex, "List ServiceAccounts error"); throw new RpcException(new Status(StatusCode.Internal, "Internal error.")); }
    }

    public override async Task<ServiceAccountReply> Get(ServiceAccountRef request, ServerCallContext context)
    {
        try { return Map(await _manager.GetAsync(request.Cluster, request.Namespace, request.Name, context.CancellationToken)); }
        catch (PolicyViolationException ex) { throw new RpcException(new Status(StatusCode.PermissionDenied, ex.Message)); }
        catch (k8s.Autorest.HttpOperationException ex) when (ex.Response.StatusCode == System.Net.HttpStatusCode.NotFound)
        { throw new RpcException(new Status(StatusCode.NotFound, $"ServiceAccount {request.Name} not found.")); }
        catch (Exception ex) { _logger.LogError(ex, "Get ServiceAccount error"); throw new RpcException(new Status(StatusCode.Internal, "Internal error.")); }
    }

    public override async Task<OperationReply> Create(CreateServiceAccountRequest request, ServerCallContext context)
    {
        try
        {
            var dto = new CreateServiceAccountDto
            {
                Cluster = request.Cluster, Namespace = request.Namespace, Name = request.Name,
                Annotations = request.Annotations.ToDictionary(k => k.Key, v => v.Value),
                RequestedBy = request.RequestedBy, Reason = request.Reason, CorrelationId = request.CorrelationId
            };
            var result = await _manager.CreateAsync(dto, context.CancellationToken);
            return new OperationReply { Success = result.Success, Message = result.Message, CorrelationId = result.CorrelationId };
        }
        catch (PolicyViolationException ex) { throw new RpcException(new Status(StatusCode.PermissionDenied, ex.Message)); }
        catch (Exception ex) { _logger.LogError(ex, "Create ServiceAccount error"); throw new RpcException(new Status(StatusCode.Internal, "Internal error.")); }
    }

    public override async Task<OperationReply> Delete(DeleteServiceAccountRequest request, ServerCallContext context)
    {
        try
        {
            var result = await _manager.DeleteAsync(request.Cluster, request.Namespace, request.Name, request.RequestedBy, request.Reason, request.CorrelationId, context.CancellationToken);
            return new OperationReply { Success = result.Success, Message = result.Message, CorrelationId = result.CorrelationId };
        }
        catch (PolicyViolationException ex) { throw new RpcException(new Status(StatusCode.PermissionDenied, ex.Message)); }
        catch (Exception ex) { _logger.LogError(ex, "Delete ServiceAccount error"); throw new RpcException(new Status(StatusCode.Internal, "Internal error.")); }
    }

    private static ServiceAccountReply Map(ServiceAccountDto d)
    {
        var reply = new ServiceAccountReply { Name = d.Name, Namespace = d.Namespace, CreatedAt = d.CreatedAt };
        reply.Secrets.AddRange(d.Secrets);
        return reply;
    }
}

public sealed class RbacGrpcService : RbacService.RbacServiceBase
{
    private readonly IRbacManager _manager;
    private readonly ILogger<RbacGrpcService> _logger;

    public RbacGrpcService(IRbacManager manager, ILogger<RbacGrpcService> logger)
    { _manager = manager; _logger = logger; }

    // ─ Roles ─
    public override async Task<RoleListReply> ListRoles(ListRolesRequest request, ServerCallContext context)
    {
        try
        {
            var items = await _manager.ListRolesAsync(request.Cluster, request.Namespace, context.CancellationToken);
            var reply = new RoleListReply();
            reply.Items.AddRange(items.Select(MapRole));
            return reply;
        }
        catch (Exception ex) { _logger.LogError(ex, "List Roles error"); throw new RpcException(new Status(StatusCode.Internal, "Internal error.")); }
    }

    public override async Task<RoleReply> GetRole(RoleRef request, ServerCallContext context)
    {
        try { return MapRole(await _manager.GetRoleAsync(request.Cluster, request.Namespace, request.Name, context.CancellationToken)); }
        catch (k8s.Autorest.HttpOperationException ex) when (ex.Response.StatusCode == System.Net.HttpStatusCode.NotFound)
        { throw new RpcException(new Status(StatusCode.NotFound, $"Role {request.Name} not found.")); }
        catch (Exception ex) { _logger.LogError(ex, "Get Role error"); throw new RpcException(new Status(StatusCode.Internal, "Internal error.")); }
    }

    public override async Task<OperationReply> CreateRole(CreateRoleRequest request, ServerCallContext context)
    {
        try
        {
            var dto = new CreateRoleDto
            {
                Cluster = request.Cluster, Namespace = request.Namespace, Name = request.Name,
                Rules = request.Rules.Select(MapRuleDto).ToList(),
                RequestedBy = request.RequestedBy, Reason = request.Reason, CorrelationId = request.CorrelationId
            };
            var result = await _manager.CreateRoleAsync(dto, context.CancellationToken);
            return new OperationReply { Success = result.Success, Message = result.Message, CorrelationId = result.CorrelationId };
        }
        catch (Exception ex) { _logger.LogError(ex, "Create Role error"); throw new RpcException(new Status(StatusCode.Internal, "Internal error.")); }
    }

    public override async Task<OperationReply> DeleteRole(DeleteRoleRequest request, ServerCallContext context)
    {
        try
        {
            var result = await _manager.DeleteRoleAsync(request.Cluster, request.Namespace, request.Name, request.RequestedBy, request.Reason, request.CorrelationId, context.CancellationToken);
            return new OperationReply { Success = result.Success, Message = result.Message, CorrelationId = result.CorrelationId };
        }
        catch (Exception ex) { _logger.LogError(ex, "Delete Role error"); throw new RpcException(new Status(StatusCode.Internal, "Internal error.")); }
    }

    // ─ ClusterRoles ─
    public override async Task<ClusterRoleListReply> ListClusterRoles(ListClusterRolesRequest request, ServerCallContext context)
    {
        try
        {
            var items = await _manager.ListClusterRolesAsync(request.Cluster, context.CancellationToken);
            var reply = new ClusterRoleListReply();
            reply.Items.AddRange(items.Select(MapClusterRole));
            return reply;
        }
        catch (Exception ex) { _logger.LogError(ex, "List ClusterRoles error"); throw new RpcException(new Status(StatusCode.Internal, "Internal error.")); }
    }

    public override async Task<ClusterRoleReply> GetClusterRole(ClusterRoleRef request, ServerCallContext context)
    {
        try { return MapClusterRole(await _manager.GetClusterRoleAsync(request.Cluster, request.Name, context.CancellationToken)); }
        catch (k8s.Autorest.HttpOperationException ex) when (ex.Response.StatusCode == System.Net.HttpStatusCode.NotFound)
        { throw new RpcException(new Status(StatusCode.NotFound, $"ClusterRole {request.Name} not found.")); }
        catch (Exception ex) { _logger.LogError(ex, "Get ClusterRole error"); throw new RpcException(new Status(StatusCode.Internal, "Internal error.")); }
    }

    public override async Task<OperationReply> CreateClusterRole(CreateClusterRoleRequest request, ServerCallContext context)
    {
        try
        {
            var dto = new CreateClusterRoleDto
            {
                Cluster = request.Cluster, Name = request.Name,
                Rules = request.Rules.Select(MapRuleDto).ToList(),
                RequestedBy = request.RequestedBy, Reason = request.Reason, CorrelationId = request.CorrelationId
            };
            var result = await _manager.CreateClusterRoleAsync(dto, context.CancellationToken);
            return new OperationReply { Success = result.Success, Message = result.Message, CorrelationId = result.CorrelationId };
        }
        catch (Exception ex) { _logger.LogError(ex, "Create ClusterRole error"); throw new RpcException(new Status(StatusCode.Internal, "Internal error.")); }
    }

    public override async Task<OperationReply> DeleteClusterRole(DeleteClusterRoleRequest request, ServerCallContext context)
    {
        try
        {
            var result = await _manager.DeleteClusterRoleAsync(request.Cluster, request.Name, request.RequestedBy, request.Reason, request.CorrelationId, context.CancellationToken);
            return new OperationReply { Success = result.Success, Message = result.Message, CorrelationId = result.CorrelationId };
        }
        catch (Exception ex) { _logger.LogError(ex, "Delete ClusterRole error"); throw new RpcException(new Status(StatusCode.Internal, "Internal error.")); }
    }

    // ─ RoleBindings ─
    public override async Task<RoleBindingListReply> ListRoleBindings(ListRoleBindingsRequest request, ServerCallContext context)
    {
        try
        {
            var items = await _manager.ListRoleBindingsAsync(request.Cluster, request.Namespace, context.CancellationToken);
            var reply = new RoleBindingListReply();
            reply.Items.AddRange(items.Select(MapRoleBinding));
            return reply;
        }
        catch (Exception ex) { _logger.LogError(ex, "List RoleBindings error"); throw new RpcException(new Status(StatusCode.Internal, "Internal error.")); }
    }

    public override async Task<RoleBindingReply> GetRoleBinding(RoleBindingRef request, ServerCallContext context)
    {
        try { return MapRoleBinding(await _manager.GetRoleBindingAsync(request.Cluster, request.Namespace, request.Name, context.CancellationToken)); }
        catch (k8s.Autorest.HttpOperationException ex) when (ex.Response.StatusCode == System.Net.HttpStatusCode.NotFound)
        { throw new RpcException(new Status(StatusCode.NotFound, $"RoleBinding {request.Name} not found.")); }
        catch (Exception ex) { _logger.LogError(ex, "Get RoleBinding error"); throw new RpcException(new Status(StatusCode.Internal, "Internal error.")); }
    }

    public override async Task<OperationReply> CreateRoleBinding(CreateRoleBindingRequest request, ServerCallContext context)
    {
        try
        {
            var dto = new CreateRoleBindingDto
            {
                Cluster = request.Cluster, Namespace = request.Namespace, Name = request.Name,
                RoleKind = request.RoleKind, RoleName = request.RoleName,
                Subjects = request.Subjects.Select(s => new SubjectDto { Kind = s.Kind, Name = s.Name, Namespace = s.Namespace, ApiGroup = s.ApiGroup }).ToList(),
                RequestedBy = request.RequestedBy, Reason = request.Reason, CorrelationId = request.CorrelationId
            };
            var result = await _manager.CreateRoleBindingAsync(dto, context.CancellationToken);
            return new OperationReply { Success = result.Success, Message = result.Message, CorrelationId = result.CorrelationId };
        }
        catch (Exception ex) { _logger.LogError(ex, "Create RoleBinding error"); throw new RpcException(new Status(StatusCode.Internal, "Internal error.")); }
    }

    public override async Task<OperationReply> DeleteRoleBinding(DeleteRoleBindingRequest request, ServerCallContext context)
    {
        try
        {
            var result = await _manager.DeleteRoleBindingAsync(request.Cluster, request.Namespace, request.Name, request.RequestedBy, request.Reason, request.CorrelationId, context.CancellationToken);
            return new OperationReply { Success = result.Success, Message = result.Message, CorrelationId = result.CorrelationId };
        }
        catch (Exception ex) { _logger.LogError(ex, "Delete RoleBinding error"); throw new RpcException(new Status(StatusCode.Internal, "Internal error.")); }
    }

    // ─ ClusterRoleBindings ─
    public override async Task<ClusterRoleBindingListReply> ListClusterRoleBindings(ListClusterRoleBindingsRequest request, ServerCallContext context)
    {
        try
        {
            var items = await _manager.ListClusterRoleBindingsAsync(request.Cluster, context.CancellationToken);
            var reply = new ClusterRoleBindingListReply();
            reply.Items.AddRange(items.Select(MapClusterRoleBinding));
            return reply;
        }
        catch (Exception ex) { _logger.LogError(ex, "List ClusterRoleBindings error"); throw new RpcException(new Status(StatusCode.Internal, "Internal error.")); }
    }

    public override async Task<ClusterRoleBindingReply> GetClusterRoleBinding(ClusterRoleBindingRef request, ServerCallContext context)
    {
        try { return MapClusterRoleBinding(await _manager.GetClusterRoleBindingAsync(request.Cluster, request.Name, context.CancellationToken)); }
        catch (k8s.Autorest.HttpOperationException ex) when (ex.Response.StatusCode == System.Net.HttpStatusCode.NotFound)
        { throw new RpcException(new Status(StatusCode.NotFound, $"ClusterRoleBinding {request.Name} not found.")); }
        catch (Exception ex) { _logger.LogError(ex, "Get ClusterRoleBinding error"); throw new RpcException(new Status(StatusCode.Internal, "Internal error.")); }
    }

    public override async Task<OperationReply> CreateClusterRoleBinding(CreateClusterRoleBindingRequest request, ServerCallContext context)
    {
        try
        {
            var dto = new CreateClusterRoleBindingDto
            {
                Cluster = request.Cluster, Name = request.Name, RoleName = request.RoleName,
                Subjects = request.Subjects.Select(s => new SubjectDto { Kind = s.Kind, Name = s.Name, Namespace = s.Namespace, ApiGroup = s.ApiGroup }).ToList(),
                RequestedBy = request.RequestedBy, Reason = request.Reason, CorrelationId = request.CorrelationId
            };
            var result = await _manager.CreateClusterRoleBindingAsync(dto, context.CancellationToken);
            return new OperationReply { Success = result.Success, Message = result.Message, CorrelationId = result.CorrelationId };
        }
        catch (Exception ex) { _logger.LogError(ex, "Create ClusterRoleBinding error"); throw new RpcException(new Status(StatusCode.Internal, "Internal error.")); }
    }

    public override async Task<OperationReply> DeleteClusterRoleBinding(DeleteClusterRoleBindingRequest request, ServerCallContext context)
    {
        try
        {
            var result = await _manager.DeleteClusterRoleBindingAsync(request.Cluster, request.Name, request.RequestedBy, request.Reason, request.CorrelationId, context.CancellationToken);
            return new OperationReply { Success = result.Success, Message = result.Message, CorrelationId = result.CorrelationId };
        }
        catch (Exception ex) { _logger.LogError(ex, "Delete ClusterRoleBinding error"); throw new RpcException(new Status(StatusCode.Internal, "Internal error.")); }
    }

    // ─ Mappers ─
    private static PolicyRuleDto MapRuleDto(PolicyRule r) => new()
    {
        ApiGroups = r.ApiGroups.ToList(), Resources = r.Resources.ToList(),
        Verbs = r.Verbs.ToList(), ResourceNames = r.ResourceNames.ToList()
    };

    private static PolicyRule MapRule(PolicyRuleDto r)
    {
        var rule = new PolicyRule();
        rule.ApiGroups.AddRange(r.ApiGroups);
        rule.Resources.AddRange(r.Resources);
        rule.Verbs.AddRange(r.Verbs);
        rule.ResourceNames.AddRange(r.ResourceNames);
        return rule;
    }

    private static RoleReply MapRole(RoleDto d)
    {
        var r = new RoleReply { Name = d.Name, Namespace = d.Namespace, CreatedAt = d.CreatedAt };
        r.Rules.AddRange(d.Rules.Select(MapRule));
        return r;
    }

    private static ClusterRoleReply MapClusterRole(ClusterRoleDto d)
    {
        var r = new ClusterRoleReply { Name = d.Name, CreatedAt = d.CreatedAt };
        r.Rules.AddRange(d.Rules.Select(MapRule));
        return r;
    }

    private static Subject MapSubject(SubjectDto s) => new Subject { Kind = s.Kind, Name = s.Name, Namespace = s.Namespace, ApiGroup = s.ApiGroup };

    private static RoleBindingReply MapRoleBinding(RoleBindingDto d)
    {
        var r = new RoleBindingReply { Name = d.Name, Namespace = d.Namespace, RoleKind = d.RoleKind, RoleName = d.RoleName, CreatedAt = d.CreatedAt };
        r.Subjects.AddRange(d.Subjects.Select(MapSubject));
        return r;
    }

    private static ClusterRoleBindingReply MapClusterRoleBinding(ClusterRoleBindingDto d)
    {
        var r = new ClusterRoleBindingReply { Name = d.Name, RoleName = d.RoleName, CreatedAt = d.CreatedAt };
        r.Subjects.AddRange(d.Subjects.Select(MapSubject));
        return r;
    }
}

public sealed class NetworkPolicyGrpcService : NetworkPolicyService.NetworkPolicyServiceBase
{
    private readonly INetworkPolicyManager _manager;
    private readonly ILogger<NetworkPolicyGrpcService> _logger;

    public NetworkPolicyGrpcService(INetworkPolicyManager manager, ILogger<NetworkPolicyGrpcService> logger)
    { _manager = manager; _logger = logger; }

    public override async Task<NetworkPolicyListReply> List(ListNetworkPoliciesRequest request, ServerCallContext context)
    {
        try
        {
            var items = await _manager.ListAsync(request.Cluster, request.Namespace, context.CancellationToken);
            var reply = new NetworkPolicyListReply();
            reply.Items.AddRange(items.Select(Map));
            return reply;
        }
        catch (PolicyViolationException ex) { throw new RpcException(new Status(StatusCode.PermissionDenied, ex.Message)); }
        catch (Exception ex) { _logger.LogError(ex, "List NetworkPolicies error"); throw new RpcException(new Status(StatusCode.Internal, "Internal error.")); }
    }

    public override async Task<NetworkPolicyReply> Get(NetworkPolicyRef request, ServerCallContext context)
    {
        try { return Map(await _manager.GetAsync(request.Cluster, request.Namespace, request.Name, context.CancellationToken)); }
        catch (PolicyViolationException ex) { throw new RpcException(new Status(StatusCode.PermissionDenied, ex.Message)); }
        catch (k8s.Autorest.HttpOperationException ex) when (ex.Response.StatusCode == System.Net.HttpStatusCode.NotFound)
        { throw new RpcException(new Status(StatusCode.NotFound, $"NetworkPolicy {request.Name} not found.")); }
        catch (Exception ex) { _logger.LogError(ex, "Get NetworkPolicy error"); throw new RpcException(new Status(StatusCode.Internal, "Internal error.")); }
    }

    public override async Task<OperationReply> Create(CreateNetworkPolicyRequest request, ServerCallContext context)
    {
        try
        {
            var dto = new CreateNetworkPolicyDto
            {
                Cluster = request.Cluster, Namespace = request.Namespace, Name = request.Name,
                PodSelector = request.PodSelector.ToDictionary(k => k.Key, v => v.Value),
                PolicyTypes = request.PolicyTypes.ToList(),
                DenyAllIngress = request.DenyAllIngress, DenyAllEgress = request.DenyAllEgress,
                AllowIngressFrom = request.AllowIngressFrom.Select(p => new NetworkPolicyPeerDto { NamespaceSelector = p.NamespaceSelector.ToDictionary(k => k.Key, v => v.Value), PodSelector = p.PodSelector.ToDictionary(k => k.Key, v => v.Value), IpBlock = p.IpBlock }).ToList(),
                AllowEgressTo = request.AllowEgressTo.Select(p => new NetworkPolicyPeerDto { NamespaceSelector = p.NamespaceSelector.ToDictionary(k => k.Key, v => v.Value), PodSelector = p.PodSelector.ToDictionary(k => k.Key, v => v.Value), IpBlock = p.IpBlock }).ToList(),
                RequestedBy = request.RequestedBy, Reason = request.Reason, CorrelationId = request.CorrelationId
            };
            var result = await _manager.CreateAsync(dto, context.CancellationToken);
            return new OperationReply { Success = result.Success, Message = result.Message, CorrelationId = result.CorrelationId };
        }
        catch (PolicyViolationException ex) { throw new RpcException(new Status(StatusCode.PermissionDenied, ex.Message)); }
        catch (Exception ex) { _logger.LogError(ex, "Create NetworkPolicy error"); throw new RpcException(new Status(StatusCode.Internal, "Internal error.")); }
    }

    public override async Task<OperationReply> Delete(DeleteNetworkPolicyRequest request, ServerCallContext context)
    {
        try
        {
            var result = await _manager.DeleteAsync(request.Cluster, request.Namespace, request.Name, request.RequestedBy, request.Reason, request.CorrelationId, context.CancellationToken);
            return new OperationReply { Success = result.Success, Message = result.Message, CorrelationId = result.CorrelationId };
        }
        catch (PolicyViolationException ex) { throw new RpcException(new Status(StatusCode.PermissionDenied, ex.Message)); }
        catch (Exception ex) { _logger.LogError(ex, "Delete NetworkPolicy error"); throw new RpcException(new Status(StatusCode.Internal, "Internal error.")); }
    }

    private static NetworkPolicyReply Map(NetworkPolicyDto d)
    {
        var r = new NetworkPolicyReply { Name = d.Name, Namespace = d.Namespace, CreatedAt = d.CreatedAt };
        r.PodSelectorLabels.AddRange(d.PodSelectorLabels);
        r.PolicyTypes.AddRange(d.PolicyTypes);
        return r;
    }
}
