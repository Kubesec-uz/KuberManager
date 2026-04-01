using Grpc.Core;
using KuberManager.Contracts;
using KuberManager.Service.Application;
using KuberManager.Service.Application.DTOs;
using KuberManager.Service.Infrastructure.Policy;

namespace KuberManager.Service.Grpc;

public sealed class NodeGrpcService : NodeService.NodeServiceBase
{
    private readonly INodeManager _manager;
    private readonly ILogger<NodeGrpcService> _logger;

    public NodeGrpcService(INodeManager manager, ILogger<NodeGrpcService> logger)
    { _manager = manager; _logger = logger; }

    public override async Task<NodeListReply> List(ListNodesRequest request, ServerCallContext context)
    {
        try
        {
            var items = await _manager.ListAsync(request.Cluster, context.CancellationToken);
            var reply = new NodeListReply();
            reply.Items.AddRange(items.Select(Map));
            return reply;
        }
        catch (Exception ex) { _logger.LogError(ex, "List Nodes error"); throw new RpcException(new Status(StatusCode.Internal, "Internal error.")); }
    }

    public override async Task<NodeReply> Get(NodeRef request, ServerCallContext context)
    {
        try { return Map(await _manager.GetAsync(request.Cluster, request.Name, context.CancellationToken)); }
        catch (k8s.Autorest.HttpOperationException ex) when (ex.Response.StatusCode == System.Net.HttpStatusCode.NotFound)
        { throw new RpcException(new Status(StatusCode.NotFound, $"Node {request.Name} not found.")); }
        catch (Exception ex) { _logger.LogError(ex, "Get Node error"); throw new RpcException(new Status(StatusCode.Internal, "Internal error.")); }
    }

    public override async Task<OperationReply> Cordon(CordonNodeRequest request, ServerCallContext context)
    {
        try
        {
            var result = await _manager.CordonAsync(request.Cluster, request.Name, request.RequestedBy, request.Reason, request.CorrelationId, context.CancellationToken);
            return new OperationReply { Success = result.Success, Message = result.Message, CorrelationId = result.CorrelationId };
        }
        catch (Exception ex) { _logger.LogError(ex, "Cordon Node error"); throw new RpcException(new Status(StatusCode.Internal, "Internal error.")); }
    }

    public override async Task<OperationReply> Uncordon(UncordonNodeRequest request, ServerCallContext context)
    {
        try
        {
            var result = await _manager.UncordonAsync(request.Cluster, request.Name, request.RequestedBy, request.Reason, request.CorrelationId, context.CancellationToken);
            return new OperationReply { Success = result.Success, Message = result.Message, CorrelationId = result.CorrelationId };
        }
        catch (Exception ex) { _logger.LogError(ex, "Uncordon Node error"); throw new RpcException(new Status(StatusCode.Internal, "Internal error.")); }
    }

    public override async Task<OperationReply> Drain(DrainNodeRequest request, ServerCallContext context)
    {
        try
        {
            var result = await _manager.DrainAsync(request.Cluster, request.Name, request.Force, request.IgnoreDaemonsets, request.RequestedBy, request.Reason, request.CorrelationId, context.CancellationToken);
            return new OperationReply { Success = result.Success, Message = result.Message, CorrelationId = result.CorrelationId };
        }
        catch (Exception ex) { _logger.LogError(ex, "Drain Node error"); throw new RpcException(new Status(StatusCode.Internal, "Internal error.")); }
    }

    private static NodeReply Map(NodeDto d) => new()
    {
        Name = d.Name, Status = d.Status, Roles = d.Roles, OsImage = d.OsImage,
        KernelVersion = d.KernelVersion, ContainerRuntime = d.ContainerRuntime,
        CpuCapacity = d.CpuCapacity, MemoryCapacity = d.MemoryCapacity,
        Unschedulable = d.Unschedulable, CreatedAt = d.CreatedAt
    };
}

public sealed class PvcGrpcService : PersistentVolumeClaimService.PersistentVolumeClaimServiceBase
{
    private readonly IPvcManager _manager;
    private readonly ILogger<PvcGrpcService> _logger;

    public PvcGrpcService(IPvcManager manager, ILogger<PvcGrpcService> logger)
    { _manager = manager; _logger = logger; }

    public override async Task<PvcListReply> List(ListPvcsRequest request, ServerCallContext context)
    {
        try
        {
            var items = await _manager.ListAsync(request.Cluster, request.Namespace, context.CancellationToken);
            var reply = new PvcListReply();
            reply.Items.AddRange(items.Select(Map));
            return reply;
        }
        catch (PolicyViolationException ex) { throw new RpcException(new Status(StatusCode.PermissionDenied, ex.Message)); }
        catch (Exception ex) { _logger.LogError(ex, "List PVCs error"); throw new RpcException(new Status(StatusCode.Internal, "Internal error.")); }
    }

    public override async Task<PvcReply> Get(PvcRef request, ServerCallContext context)
    {
        try { return Map(await _manager.GetAsync(request.Cluster, request.Namespace, request.Name, context.CancellationToken)); }
        catch (PolicyViolationException ex) { throw new RpcException(new Status(StatusCode.PermissionDenied, ex.Message)); }
        catch (k8s.Autorest.HttpOperationException ex) when (ex.Response.StatusCode == System.Net.HttpStatusCode.NotFound)
        { throw new RpcException(new Status(StatusCode.NotFound, $"PVC {request.Name} not found.")); }
        catch (Exception ex) { _logger.LogError(ex, "Get PVC error"); throw new RpcException(new Status(StatusCode.Internal, "Internal error.")); }
    }

    public override async Task<OperationReply> Create(CreatePvcRequest request, ServerCallContext context)
    {
        try
        {
            var dto = new CreatePvcDto
            {
                Cluster = request.Cluster, Namespace = request.Namespace, Name = request.Name,
                StorageClass = request.StorageClass, Storage = request.Storage,
                AccessModes = request.AccessModes.ToList(),
                RequestedBy = request.RequestedBy, Reason = request.Reason, CorrelationId = request.CorrelationId
            };
            var result = await _manager.CreateAsync(dto, context.CancellationToken);
            return new OperationReply { Success = result.Success, Message = result.Message, CorrelationId = result.CorrelationId };
        }
        catch (PolicyViolationException ex) { throw new RpcException(new Status(StatusCode.PermissionDenied, ex.Message)); }
        catch (Exception ex) { _logger.LogError(ex, "Create PVC error"); throw new RpcException(new Status(StatusCode.Internal, "Internal error.")); }
    }

    public override async Task<OperationReply> Delete(DeletePvcRequest request, ServerCallContext context)
    {
        try
        {
            var result = await _manager.DeleteAsync(request.Cluster, request.Namespace, request.Name, request.RequestedBy, request.Reason, request.CorrelationId, context.CancellationToken);
            return new OperationReply { Success = result.Success, Message = result.Message, CorrelationId = result.CorrelationId };
        }
        catch (PolicyViolationException ex) { throw new RpcException(new Status(StatusCode.PermissionDenied, ex.Message)); }
        catch (Exception ex) { _logger.LogError(ex, "Delete PVC error"); throw new RpcException(new Status(StatusCode.Internal, "Internal error.")); }
    }

    private static PvcReply Map(PvcDto d) => new()
    {
        Name = d.Name, Namespace = d.Namespace, Status = d.Status, StorageClass = d.StorageClass,
        Capacity = d.Capacity, AccessModes = d.AccessModes, VolumeName = d.VolumeName, CreatedAt = d.CreatedAt
    };
}

public sealed class HpaGrpcService : HpaService.HpaServiceBase
{
    private readonly IHpaManager _manager;
    private readonly ILogger<HpaGrpcService> _logger;

    public HpaGrpcService(IHpaManager manager, ILogger<HpaGrpcService> logger)
    { _manager = manager; _logger = logger; }

    public override async Task<HpaListReply> List(ListHpasRequest request, ServerCallContext context)
    {
        try
        {
            var items = await _manager.ListAsync(request.Cluster, request.Namespace, context.CancellationToken);
            var reply = new HpaListReply();
            reply.Items.AddRange(items.Select(Map));
            return reply;
        }
        catch (PolicyViolationException ex) { throw new RpcException(new Status(StatusCode.PermissionDenied, ex.Message)); }
        catch (Exception ex) { _logger.LogError(ex, "List HPAs error"); throw new RpcException(new Status(StatusCode.Internal, "Internal error.")); }
    }

    public override async Task<HpaReply> Get(HpaRef request, ServerCallContext context)
    {
        try { return Map(await _manager.GetAsync(request.Cluster, request.Namespace, request.Name, context.CancellationToken)); }
        catch (PolicyViolationException ex) { throw new RpcException(new Status(StatusCode.PermissionDenied, ex.Message)); }
        catch (k8s.Autorest.HttpOperationException ex) when (ex.Response.StatusCode == System.Net.HttpStatusCode.NotFound)
        { throw new RpcException(new Status(StatusCode.NotFound, $"HPA {request.Name} not found.")); }
        catch (Exception ex) { _logger.LogError(ex, "Get HPA error"); throw new RpcException(new Status(StatusCode.Internal, "Internal error.")); }
    }

    public override async Task<OperationReply> Create(CreateHpaRequest request, ServerCallContext context)
    {
        try
        {
            var dto = new CreateHpaDto
            {
                Cluster = request.Cluster, Namespace = request.Namespace, Name = request.Name,
                TargetKind = request.TargetKind, TargetName = request.TargetName,
                MinReplicas = request.MinReplicas, MaxReplicas = request.MaxReplicas,
                CpuTargetUtilization = request.CpuTargetUtilization,
                RequestedBy = request.RequestedBy, Reason = request.Reason, CorrelationId = request.CorrelationId
            };
            var result = await _manager.CreateAsync(dto, context.CancellationToken);
            return new OperationReply { Success = result.Success, Message = result.Message, CorrelationId = result.CorrelationId };
        }
        catch (PolicyViolationException ex) { throw new RpcException(new Status(StatusCode.PermissionDenied, ex.Message)); }
        catch (Exception ex) { _logger.LogError(ex, "Create HPA error"); throw new RpcException(new Status(StatusCode.Internal, "Internal error.")); }
    }

    public override async Task<OperationReply> Delete(DeleteHpaRequest request, ServerCallContext context)
    {
        try
        {
            var result = await _manager.DeleteAsync(request.Cluster, request.Namespace, request.Name, request.RequestedBy, request.Reason, request.CorrelationId, context.CancellationToken);
            return new OperationReply { Success = result.Success, Message = result.Message, CorrelationId = result.CorrelationId };
        }
        catch (PolicyViolationException ex) { throw new RpcException(new Status(StatusCode.PermissionDenied, ex.Message)); }
        catch (Exception ex) { _logger.LogError(ex, "Delete HPA error"); throw new RpcException(new Status(StatusCode.Internal, "Internal error.")); }
    }

    private static HpaReply Map(HpaDto d) => new()
    {
        Name = d.Name, Namespace = d.Namespace, TargetKind = d.TargetKind, TargetName = d.TargetName,
        MinReplicas = d.MinReplicas, MaxReplicas = d.MaxReplicas, CurrentReplicas = d.CurrentReplicas,
        DesiredReplicas = d.DesiredReplicas, CpuTargetUtilization = d.CpuTargetUtilization, CreatedAt = d.CreatedAt
    };
}
