using Grpc.Core;
using KuberManager.Contracts;
using KuberManager.Service.Application;
using KuberManager.Service.Application.DTOs;
using KuberManager.Service.Infrastructure.Policy;

namespace KuberManager.Service.Grpc;

public sealed class DaemonSetGrpcService : DaemonSetService.DaemonSetServiceBase
{
    private readonly IDaemonSetManager _manager;
    private readonly ILogger<DaemonSetGrpcService> _logger;

    public DaemonSetGrpcService(IDaemonSetManager manager, ILogger<DaemonSetGrpcService> logger)
    { _manager = manager; _logger = logger; }

    public override async Task<DaemonSetListReply> List(ListDaemonSetsRequest request, ServerCallContext context)
    {
        try
        {
            var items = await _manager.ListAsync(request.Cluster, request.Namespace, context.CancellationToken);
            var reply = new DaemonSetListReply();
            reply.Items.AddRange(items.Select(Map));
            return reply;
        }
        catch (PolicyViolationException ex) { throw new RpcException(new Status(StatusCode.PermissionDenied, ex.Message)); }
        catch (Exception ex) { _logger.LogError(ex, "List DaemonSets error"); throw new RpcException(new Status(StatusCode.Internal, "Internal error.")); }
    }

    public override async Task<DaemonSetReply> Get(DaemonSetRef request, ServerCallContext context)
    {
        try { return Map(await _manager.GetAsync(request.Cluster, request.Namespace, request.Name, context.CancellationToken)); }
        catch (PolicyViolationException ex) { throw new RpcException(new Status(StatusCode.PermissionDenied, ex.Message)); }
        catch (k8s.Autorest.HttpOperationException ex) when (ex.Response.StatusCode == System.Net.HttpStatusCode.NotFound)
        { throw new RpcException(new Status(StatusCode.NotFound, $"DaemonSet {request.Name} not found.")); }
        catch (Exception ex) { _logger.LogError(ex, "Get DaemonSet error"); throw new RpcException(new Status(StatusCode.Internal, "Internal error.")); }
    }

    public override async Task<OperationReply> Create(CreateDaemonSetRequest request, ServerCallContext context)
    {
        try
        {
            var dto = new CreateDaemonSetDto
            {
                Cluster = request.Cluster, Namespace = request.Namespace, Name = request.Name,
                Image = request.Image,
                Labels = request.Labels.ToDictionary(k => k.Key, v => v.Value),
                EnvVars = request.EnvVars.ToDictionary(k => k.Key, v => v.Value),
                RequestedBy = request.RequestedBy, Reason = request.Reason, CorrelationId = request.CorrelationId
            };
            var result = await _manager.CreateAsync(dto, context.CancellationToken);
            return new OperationReply { Success = result.Success, Message = result.Message, CorrelationId = result.CorrelationId };
        }
        catch (PolicyViolationException ex) { throw new RpcException(new Status(StatusCode.PermissionDenied, ex.Message)); }
        catch (Exception ex) { _logger.LogError(ex, "Create DaemonSet error"); throw new RpcException(new Status(StatusCode.Internal, "Internal error.")); }
    }

    public override async Task<OperationReply> Delete(DeleteDaemonSetRequest request, ServerCallContext context)
    {
        try
        {
            var result = await _manager.DeleteAsync(request.Cluster, request.Namespace, request.Name, request.RequestedBy, request.Reason, request.CorrelationId, context.CancellationToken);
            return new OperationReply { Success = result.Success, Message = result.Message, CorrelationId = result.CorrelationId };
        }
        catch (PolicyViolationException ex) { throw new RpcException(new Status(StatusCode.PermissionDenied, ex.Message)); }
        catch (Exception ex) { _logger.LogError(ex, "Delete DaemonSet error"); throw new RpcException(new Status(StatusCode.Internal, "Internal error.")); }
    }

    public override async Task<OperationReply> Restart(RestartDaemonSetRequest request, ServerCallContext context)
    {
        try
        {
            var result = await _manager.RestartAsync(request.Cluster, request.Namespace, request.Name, request.RequestedBy, request.Reason, request.CorrelationId, context.CancellationToken);
            return new OperationReply { Success = result.Success, Message = result.Message, CorrelationId = result.CorrelationId };
        }
        catch (PolicyViolationException ex) { throw new RpcException(new Status(StatusCode.PermissionDenied, ex.Message)); }
        catch (Exception ex) { _logger.LogError(ex, "Restart DaemonSet error"); throw new RpcException(new Status(StatusCode.Internal, "Internal error.")); }
    }

    private static DaemonSetReply Map(DaemonSetDto d) => new()
    {
        Name = d.Name, Namespace = d.Namespace, DesiredNumberScheduled = d.DesiredNumberScheduled,
        CurrentNumberScheduled = d.CurrentNumberScheduled, NumberReady = d.NumberReady,
        Status = d.Status, CreatedAt = d.CreatedAt
    };
}
