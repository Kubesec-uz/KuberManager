using Grpc.Core;
using KuberManager.Contracts;
using KuberManager.Service.Application;
using KuberManager.Service.Application.DTOs;
using KuberManager.Service.Infrastructure.Policy;

namespace KuberManager.Service.Grpc;

public class ServiceGrpcService : ServiceResourceService.ServiceResourceServiceBase
{
    private readonly IServiceManager _manager;

    public ServiceGrpcService(IServiceManager manager)
    {
        _manager = manager;
    }

    public override async Task<ServiceListReply> List(ListServicesRequest request, ServerCallContext context)
    {
        try
        {
            var list = await _manager.ListAsync(request.Cluster, request.Namespace, context.CancellationToken);
            var reply = new ServiceListReply();
            reply.Services.AddRange(list.Select(MapToReply));
            return reply;
        }
        catch (PolicyViolationException ex)
        {
            throw new RpcException(new Status(StatusCode.PermissionDenied, ex.Message));
        }
    }

    public override async Task<ServiceReply> Get(ServiceRef request, ServerCallContext context)
    {
        try
        {
            var svc = await _manager.GetAsync(request.Cluster, request.Namespace, request.Name, context.CancellationToken);
            return MapToReply(svc);
        }
        catch (PolicyViolationException ex)
        {
            throw new RpcException(new Status(StatusCode.PermissionDenied, ex.Message));
        }
        catch (k8s.Autorest.HttpOperationException ex) when (ex.Response.StatusCode == System.Net.HttpStatusCode.NotFound)
        {
            throw new RpcException(new Status(StatusCode.NotFound, $"Service {request.Name} not found."));
        }
    }

    public override async Task<OperationReply> Create(CreateServiceRequest request, ServerCallContext context)
    {
        try
        {
            var dto = new CreateServiceDto
            {
                Cluster = request.Cluster,
                Namespace = request.Namespace,
                Name = request.Name,
                Type = request.Type,
                Selector = request.Selector.ToDictionary(k => k.Key, v => v.Value),
                RequestedBy = request.RequestedBy,
                Reason = request.Reason,
                CorrelationId = request.CorrelationId,
                Ports = request.Ports.Select(p => new ServicePortDto
                {
                    Name = p.Name,
                    Port = p.Port,
                    TargetPort = p.TargetPort,
                    NodePort = p.NodePort,
                    Protocol = p.Protocol
                }).ToList()
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

    public override async Task<OperationReply> Delete(DeleteServiceRequest request, ServerCallContext context)
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

    private static ServiceReply MapToReply(ServiceDto dto)
    {
        var reply = new ServiceReply
        {
            Name = dto.Name,
            Namespace = dto.Namespace,
            Type = dto.Type,
            ClusterIp = dto.ClusterIp,
            CreatedAt = dto.CreatedAt
        };
        
        reply.Ports.AddRange(dto.Ports.Select(p => new KuberManager.Contracts.ServicePort
        {
            Name = p.Name,
            Port = p.Port,
            TargetPort = p.TargetPort,
            NodePort = p.NodePort,
            Protocol = p.Protocol
        }));
        
        return reply;
    }
}
