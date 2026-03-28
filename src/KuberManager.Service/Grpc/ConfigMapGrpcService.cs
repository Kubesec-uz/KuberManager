using Grpc.Core;
using KuberManager.Contracts;
using KuberManager.Service.Application;
using KuberManager.Service.Infrastructure.Policy;

namespace KuberManager.Service.Grpc;

public sealed class ConfigMapGrpcService : ConfigMapService.ConfigMapServiceBase
{
    private readonly IConfigMapManager _manager;
    private readonly ILogger<ConfigMapGrpcService> _logger;

    public ConfigMapGrpcService(IConfigMapManager manager, ILogger<ConfigMapGrpcService> logger)
    {
        _manager = manager;
        _logger = logger;
    }

    public override async Task<ConfigMapListReply> List(ListConfigMapsRequest request, ServerCallContext context)
    {
        try
        {
            var list = await _manager.ListAsync(request.Cluster, request.Namespace, context.CancellationToken);
            var reply = new ConfigMapListReply();
            reply.Configmaps.AddRange(list.Select(MapToReply));
            return reply;
        }
        catch (PolicyViolationException ex) { throw new RpcException(new Status(StatusCode.PermissionDenied, ex.Message)); }
        catch (Exception ex) { _logger.LogError(ex, "List configmaps error"); throw new RpcException(new Status(StatusCode.Internal, "Internal error.")); }
    }

    public override async Task<ConfigMapReply> Get(ConfigMapRef request, ServerCallContext context)
    {
        try
        {
            var cm = await _manager.GetAsync(request.Cluster, request.Namespace, request.Name, context.CancellationToken);
            return MapToReply(cm);
        }
        catch (PolicyViolationException ex) { throw new RpcException(new Status(StatusCode.PermissionDenied, ex.Message)); }
        catch (k8s.Autorest.HttpOperationException ex) when (ex.Response.StatusCode == System.Net.HttpStatusCode.NotFound)
        { throw new RpcException(new Status(StatusCode.NotFound, $"ConfigMap '{request.Name}' not found.")); }
        catch (Exception ex) { _logger.LogError(ex, "Get configmap error"); throw new RpcException(new Status(StatusCode.Internal, "Internal error.")); }
    }

    public override async Task<OperationReply> Create(CreateConfigMapRequest request, ServerCallContext context)
    {
        try
        {
            var result = await _manager.CreateAsync(request.Cluster, request.Namespace, request.Name, request.Data.ToDictionary(k => k.Key, v => v.Value), request.RequestedBy, request.Reason, request.CorrelationId, context.CancellationToken);
            return new OperationReply { Success = result.Success, Message = result.Message, CorrelationId = result.CorrelationId };
        }
        catch (PolicyViolationException ex) { throw new RpcException(new Status(StatusCode.PermissionDenied, ex.Message)); }
        catch (Exception ex) { _logger.LogError(ex, "Create configmap error"); throw new RpcException(new Status(StatusCode.Internal, "Internal error.")); }
    }

    public override async Task<OperationReply> Update(UpdateConfigMapRequest request, ServerCallContext context)
    {
        try
        {
            var result = await _manager.UpdateAsync(request.Cluster, request.Namespace, request.Name, request.Data.ToDictionary(k => k.Key, v => v.Value), request.RequestedBy, request.Reason, request.CorrelationId, context.CancellationToken);
            return new OperationReply { Success = result.Success, Message = result.Message, CorrelationId = result.CorrelationId };
        }
        catch (PolicyViolationException ex) { throw new RpcException(new Status(StatusCode.PermissionDenied, ex.Message)); }
        catch (Exception ex) { _logger.LogError(ex, "Update configmap error"); throw new RpcException(new Status(StatusCode.Internal, "Internal error.")); }
    }

    public override async Task<OperationReply> Delete(DeleteConfigMapRequest request, ServerCallContext context)
    {
        try
        {
            var result = await _manager.DeleteAsync(request.Cluster, request.Namespace, request.Name, request.RequestedBy, request.Reason, request.CorrelationId, context.CancellationToken);
            return new OperationReply { Success = result.Success, Message = result.Message, CorrelationId = result.CorrelationId };
        }
        catch (PolicyViolationException ex) { throw new RpcException(new Status(StatusCode.PermissionDenied, ex.Message)); }
        catch (Exception ex) { _logger.LogError(ex, "Delete configmap error"); throw new RpcException(new Status(StatusCode.Internal, "Internal error.")); }
    }

    private static ConfigMapReply MapToReply(Application.DTOs.ConfigMapDto dto)
    {
        var reply = new ConfigMapReply { Name = dto.Name, Namespace = dto.Namespace, CreatedAt = dto.CreatedAt };
        foreach (var kv in dto.Data) reply.Data[kv.Key] = kv.Value;
        return reply;
    }
}
