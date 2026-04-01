using Grpc.Core;
using KuberManager.Contracts;
using KuberManager.Service.Application;
using KuberManager.Service.Application.DTOs;
using KuberManager.Service.Infrastructure.Policy;

namespace KuberManager.Service.Grpc;

public class IngressGrpcService : IngressService.IngressServiceBase
{
    private readonly IIngressManager _manager;

    public IngressGrpcService(IIngressManager manager)
    {
        _manager = manager;
    }

    public override async Task<IngressListReply> List(ListIngressesRequest request, ServerCallContext context)
    {
        try
        {
            var list = await _manager.ListAsync(request.Cluster, request.Namespace, context.CancellationToken);
            var reply = new IngressListReply();
            reply.Ingresses.AddRange(list.Select(MapToReply));
            return reply;
        }
        catch (PolicyViolationException ex)
        {
            throw new RpcException(new Status(StatusCode.PermissionDenied, ex.Message));
        }
    }

    public override async Task<IngressReply> Get(IngressRef request, ServerCallContext context)
    {
        try
        {
            var ingress = await _manager.GetAsync(request.Cluster, request.Namespace, request.Name, context.CancellationToken);
            return MapToReply(ingress);
        }
        catch (PolicyViolationException ex)
        {
            throw new RpcException(new Status(StatusCode.PermissionDenied, ex.Message));
        }
        catch (k8s.Autorest.HttpOperationException ex) when (ex.Response.StatusCode == System.Net.HttpStatusCode.NotFound)
        {
            throw new RpcException(new Status(StatusCode.NotFound, $"Ingress {request.Name} not found."));
        }
    }

    public override async Task<OperationReply> Create(CreateIngressRequest request, ServerCallContext context)
    {
        try
        {
            var dto = new CreateIngressDto
            {
                Cluster = request.Cluster,
                Namespace = request.Namespace,
                Name = request.Name,
                IngressClass = request.IngressClass,
                RequestedBy = request.RequestedBy,
                Reason = request.Reason,
                CorrelationId = request.CorrelationId,
                Rules = request.Rules.Select(r => new IngressRuleDto
                {
                    Host = r.Host,
                    Paths = r.Paths.Select(p => new IngressPathDto
                    {
                        Path = p.Path,
                        PathType = p.PathType,
                        ServiceName = p.ServiceName,
                        ServicePort = p.ServicePort
                    }).ToList()
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

    public override async Task<OperationReply> Delete(DeleteIngressRequest request, ServerCallContext context)
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

    private static IngressReply MapToReply(IngressDto dto)
    {
        var reply = new IngressReply
        {
            Name = dto.Name,
            Namespace = dto.Namespace,
            IngressClass = dto.IngressClass,
            CreatedAt = dto.CreatedAt
        };
        
        reply.Rules.AddRange(dto.Rules.Select(r =>
        {
            var rule = new IngressRuleReply { Host = r.Host };
            rule.Paths.AddRange(r.Paths.Select(p => new IngressPathReply
            {
                Path = p.Path,
                PathType = p.PathType,
                ServiceName = p.ServiceName,
                ServicePort = p.ServicePort
            }));
            return rule;
        }));
        
        return reply;
    }
}
