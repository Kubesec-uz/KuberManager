using Grpc.Core;
using KuberManager.Contracts;
using KuberManager.Service.Application;
using KuberManager.Service.Application.DTOs;
using KuberManager.Service.Infrastructure.Policy;

namespace KuberManager.Service.Grpc;

public class SecretGrpcService : SecretService.SecretServiceBase
{
    private readonly ISecretManager _manager;

    public SecretGrpcService(ISecretManager manager)
    {
        _manager = manager;
    }

    public override async Task<SecretListReply> List(ListSecretsRequest request, ServerCallContext context)
    {
        try
        {
            var list = await _manager.ListAsync(request.Cluster, request.Namespace, context.CancellationToken);
            var reply = new SecretListReply();
            reply.Secrets.AddRange(list.Select(MapToReply));
            return reply;
        }
        catch (PolicyViolationException ex)
        {
            throw new RpcException(new Status(StatusCode.PermissionDenied, ex.Message));
        }
    }

    public override async Task<SecretReply> Get(SecretRef request, ServerCallContext context)
    {
        try
        {
            var secret = await _manager.GetAsync(request.Cluster, request.Namespace, request.Name, context.CancellationToken);
            return MapToReply(secret);
        }
        catch (PolicyViolationException ex)
        {
            throw new RpcException(new Status(StatusCode.PermissionDenied, ex.Message));
        }
        catch (k8s.Autorest.HttpOperationException ex) when (ex.Response.StatusCode == System.Net.HttpStatusCode.NotFound)
        {
            throw new RpcException(new Status(StatusCode.NotFound, $"Secret {request.Name} not found."));
        }
    }

    public override async Task<OperationReply> Create(CreateSecretRequest request, ServerCallContext context)
    {
        try
        {
            var dto = new CreateSecretDto
            {
                Cluster = request.Cluster,
                Namespace = request.Namespace,
                Name = request.Name,
                Type = request.Type,
                StringData = request.StringData.ToDictionary(k => k.Key, v => v.Value),
                RequestedBy = request.RequestedBy,
                Reason = request.Reason,
                CorrelationId = request.CorrelationId
            };

            var result = await _manager.CreateAsync(dto, context.CancellationToken);
            return new OperationReply
            {
                Success = result.Success,
                Message = result.Message ?? string.Empty,
                CorrelationId = result.CorrelationId
            };
        }
        catch (PolicyViolationException ex)
        {
            throw new RpcException(new Status(StatusCode.PermissionDenied, ex.Message));
        }
    }

    public override async Task<OperationReply> Delete(DeleteSecretRequest request, ServerCallContext context)
    {
        try
        {
            var result = await _manager.DeleteAsync(
                request.Cluster, request.Namespace, request.Name,
                request.RequestedBy, request.Reason, request.CorrelationId,
                context.CancellationToken);
                
            return new OperationReply
            {
                Success = result.Success,
                Message = result.Message ?? string.Empty,
                CorrelationId = result.CorrelationId
            };
        }
        catch (PolicyViolationException ex)
        {
            throw new RpcException(new Status(StatusCode.PermissionDenied, ex.Message));
        }
    }

    private static SecretReply MapToReply(SecretDto dto)
    {
        var reply = new SecretReply
        {
            Name = dto.Name,
            Namespace = dto.Namespace,
            Type = dto.Type,
            CreatedAt = dto.CreatedAt
        };
        
        foreach(var kv in dto.Data) {
            reply.Data[kv.Key] = kv.Value;
        }
        
        return reply;
    }
}
