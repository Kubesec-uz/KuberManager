using Grpc.Core;
using KuberManager.Contracts;
using KuberManager.Service.Application;

namespace KuberManager.Service.Grpc;

public sealed class NamespaceGrpcService : NamespaceService.NamespaceServiceBase
{
    private readonly INamespaceManager _manager;
    private readonly ILogger<NamespaceGrpcService> _logger;

    public NamespaceGrpcService(INamespaceManager manager, ILogger<NamespaceGrpcService> logger)
    {
        _manager = manager;
        _logger = logger;
    }

    public override async Task<NamespaceListReply> List(ListNamespacesRequest request, ServerCallContext context)
    {
        try
        {
            var namespaces = await _manager.ListAsync(request.Cluster, context.CancellationToken);
            var reply = new NamespaceListReply();
            reply.Namespaces.AddRange(namespaces.Select(n => new NamespaceInfoReply
            {
                Name = n.Name, Status = n.Status, CreatedAt = n.CreatedAt,
                Labels = { n.Labels }
            }));
            return reply;
        }
        catch (Exception ex) { _logger.LogError(ex, "List namespaces error"); throw new RpcException(new Status(StatusCode.Internal, "Internal error.")); }
    }

    public override async Task<NamespaceInfoReply> Get(NamespaceRef request, ServerCallContext context)
    {
        try
        {
            var ns = await _manager.GetAsync(request.Cluster, request.Name, context.CancellationToken);
            return new NamespaceInfoReply { Name = ns.Name, Status = ns.Status, CreatedAt = ns.CreatedAt, Labels = { ns.Labels } };
        }
        catch (k8s.Autorest.HttpOperationException ex) when (ex.Response.StatusCode == System.Net.HttpStatusCode.NotFound)
        { throw new RpcException(new Status(StatusCode.NotFound, $"Namespace '{request.Name}' not found.")); }
        catch (Exception ex) { _logger.LogError(ex, "Get namespace error"); throw new RpcException(new Status(StatusCode.Internal, "Internal error.")); }
    }

    public override async Task<OperationReply> Create(CreateNamespaceRequest request, ServerCallContext context)
    {
        try
        {
            var result = await _manager.CreateAsync(request.Cluster, request.Name, request.Labels.ToDictionary(k => k.Key, v => v.Value), request.RequestedBy, request.Reason, request.CorrelationId, context.CancellationToken);
            return new OperationReply { Success = result.Success, Message = result.Message, CorrelationId = result.CorrelationId };
        }
        catch (Exception ex) { _logger.LogError(ex, "Create namespace error"); throw new RpcException(new Status(StatusCode.Internal, "Internal error.")); }
    }

    public override async Task<OperationReply> Delete(DeleteNamespaceRequest request, ServerCallContext context)
    {
        try
        {
            var result = await _manager.DeleteAsync(request.Cluster, request.Name, request.RequestedBy, request.Reason, request.CorrelationId, context.CancellationToken);
            return new OperationReply { Success = result.Success, Message = result.Message, CorrelationId = result.CorrelationId };
        }
        catch (Exception ex) { _logger.LogError(ex, "Delete namespace error"); throw new RpcException(new Status(StatusCode.Internal, "Internal error.")); }
    }
}
