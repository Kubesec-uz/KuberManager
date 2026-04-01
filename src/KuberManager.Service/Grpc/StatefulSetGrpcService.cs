using Grpc.Core;
using KuberManager.Contracts;
using KuberManager.Service.Application;
using KuberManager.Service.Application.DTOs;
using KuberManager.Service.Infrastructure.Policy;

namespace KuberManager.Service.Grpc;

public sealed class StatefulSetGrpcService : StatefulSetService.StatefulSetServiceBase
{
    private readonly IStatefulSetManager _manager;
    private readonly ILogger<StatefulSetGrpcService> _logger;

    public StatefulSetGrpcService(IStatefulSetManager manager, ILogger<StatefulSetGrpcService> logger)
    { _manager = manager; _logger = logger; }

    public override async Task<StatefulSetListReply> List(ListStatefulSetsRequest request, ServerCallContext context)
    {
        try
        {
            var items = await _manager.ListAsync(request.Cluster, request.Namespace, context.CancellationToken);
            var reply = new StatefulSetListReply();
            reply.Items.AddRange(items.Select(Map));
            return reply;
        }
        catch (PolicyViolationException ex) { throw new RpcException(new Status(StatusCode.PermissionDenied, ex.Message)); }
        catch (Exception ex) { _logger.LogError(ex, "List StatefulSets error"); throw new RpcException(new Status(StatusCode.Internal, "Internal error.")); }
    }

    public override async Task<StatefulSetReply> Get(StatefulSetRef request, ServerCallContext context)
    {
        try { return Map(await _manager.GetAsync(request.Cluster, request.Namespace, request.Name, context.CancellationToken)); }
        catch (PolicyViolationException ex) { throw new RpcException(new Status(StatusCode.PermissionDenied, ex.Message)); }
        catch (k8s.Autorest.HttpOperationException ex) when (ex.Response.StatusCode == System.Net.HttpStatusCode.NotFound)
        { throw new RpcException(new Status(StatusCode.NotFound, $"StatefulSet {request.Name} not found.")); }
        catch (Exception ex) { _logger.LogError(ex, "Get StatefulSet error"); throw new RpcException(new Status(StatusCode.Internal, "Internal error.")); }
    }

    public override async Task<OperationReply> Create(CreateStatefulSetRequest request, ServerCallContext context)
    {
        try
        {
            var dto = new CreateStatefulSetDto
            {
                Cluster = request.Cluster, Namespace = request.Namespace, Name = request.Name,
                Image = request.Image, Replicas = request.Replicas, ServiceName = request.ServiceName,
                Labels = request.Labels.ToDictionary(k => k.Key, v => v.Value),
                EnvVars = request.EnvVars.ToDictionary(k => k.Key, v => v.Value),
                Ports = request.Ports.Select(p => new ContainerPortDto { Name = p.Name, ContainerPort = p.ContainerPort_, Protocol = string.IsNullOrEmpty(p.Protocol) ? "TCP" : p.Protocol }).ToList(),
                RequestedBy = request.RequestedBy, Reason = request.Reason, CorrelationId = request.CorrelationId
            };
            var result = await _manager.CreateAsync(dto, context.CancellationToken);
            return new OperationReply { Success = result.Success, Message = result.Message, CorrelationId = result.CorrelationId };
        }
        catch (PolicyViolationException ex) { throw new RpcException(new Status(StatusCode.PermissionDenied, ex.Message)); }
        catch (Exception ex) { _logger.LogError(ex, "Create StatefulSet error"); throw new RpcException(new Status(StatusCode.Internal, "Internal error.")); }
    }

    public override async Task<OperationReply> Delete(DeleteStatefulSetRequest request, ServerCallContext context)
    {
        try
        {
            var result = await _manager.DeleteAsync(request.Cluster, request.Namespace, request.Name, request.RequestedBy, request.Reason, request.CorrelationId, context.CancellationToken);
            return new OperationReply { Success = result.Success, Message = result.Message, CorrelationId = result.CorrelationId };
        }
        catch (PolicyViolationException ex) { throw new RpcException(new Status(StatusCode.PermissionDenied, ex.Message)); }
        catch (Exception ex) { _logger.LogError(ex, "Delete StatefulSet error"); throw new RpcException(new Status(StatusCode.Internal, "Internal error.")); }
    }

    public override async Task<OperationReply> Scale(ScaleStatefulSetRequest request, ServerCallContext context)
    {
        try
        {
            var result = await _manager.ScaleAsync(request.Cluster, request.Namespace, request.Name, request.Replicas, request.RequestedBy, request.Reason, request.CorrelationId, context.CancellationToken);
            return new OperationReply { Success = result.Success, Message = result.Message, CorrelationId = result.CorrelationId };
        }
        catch (PolicyViolationException ex) { throw new RpcException(new Status(StatusCode.PermissionDenied, ex.Message)); }
        catch (Exception ex) { _logger.LogError(ex, "Scale StatefulSet error"); throw new RpcException(new Status(StatusCode.Internal, "Internal error.")); }
    }

    public override async Task<OperationReply> Restart(RestartStatefulSetRequest request, ServerCallContext context)
    {
        try
        {
            var result = await _manager.RestartAsync(request.Cluster, request.Namespace, request.Name, request.RequestedBy, request.Reason, request.CorrelationId, context.CancellationToken);
            return new OperationReply { Success = result.Success, Message = result.Message, CorrelationId = result.CorrelationId };
        }
        catch (PolicyViolationException ex) { throw new RpcException(new Status(StatusCode.PermissionDenied, ex.Message)); }
        catch (Exception ex) { _logger.LogError(ex, "Restart StatefulSet error"); throw new RpcException(new Status(StatusCode.Internal, "Internal error.")); }
    }

    private static StatefulSetReply Map(StatefulSetDto d) => new()
    {
        Name = d.Name, Namespace = d.Namespace, DesiredReplicas = d.DesiredReplicas,
        ReadyReplicas = d.ReadyReplicas, CurrentReplicas = d.CurrentReplicas,
        ServiceName = d.ServiceName, Status = d.Status, CreatedAt = d.CreatedAt
    };
}
