using Grpc.Core;
using KuberManager.Contracts;
using KuberManager.Service.Application;
using KuberManager.Service.Application.DTOs;
using KuberManager.Service.Infrastructure.Policy;

namespace KuberManager.Service.Grpc;

public sealed class DeploymentGrpcService : DeploymentService.DeploymentServiceBase
{
    private readonly IDeploymentManager _manager;
    private readonly ILogger<DeploymentGrpcService> _logger;

    public DeploymentGrpcService(IDeploymentManager manager, ILogger<DeploymentGrpcService> logger)
    {
        _manager = manager;
        _logger = logger;
    }

    public override async Task<DeploymentStatusReply> GetStatus(DeploymentRef request, ServerCallContext context)
    {
        try
        {
            var dto = await _manager.GetStatusAsync(request.Cluster, request.Namespace, request.Name, context.CancellationToken);
            return MapToReply(dto);
        }
        catch (PolicyViolationException ex) { throw new RpcException(new Status(StatusCode.PermissionDenied, ex.Message)); }
        catch (k8s.Autorest.HttpOperationException ex) when (ex.Response.StatusCode == System.Net.HttpStatusCode.NotFound)
        { throw new RpcException(new Status(StatusCode.NotFound, $"Deployment '{request.Name}' not found.")); }
        catch (Exception ex) { _logger.LogError(ex, "GetStatus error"); throw new RpcException(new Status(StatusCode.Internal, "Internal error.")); }
    }

    public override async Task<DeploymentListReply> List(ListDeploymentsRequest request, ServerCallContext context)
    {
        try
        {
            var dtos = await _manager.ListAsync(request.Cluster, request.Namespace, context.CancellationToken);
            var reply = new DeploymentListReply();
            reply.Deployments.AddRange(dtos.Select(MapToReply));
            return reply;
        }
        catch (PolicyViolationException ex) { throw new RpcException(new Status(StatusCode.PermissionDenied, ex.Message)); }
        catch (Exception ex) { _logger.LogError(ex, "List error"); throw new RpcException(new Status(StatusCode.Internal, "Internal error.")); }
    }

    public override async Task<OperationReply> Create(CreateDeploymentRequest request, ServerCallContext context)
    {
        try
        {
            var dto = new CreateDeploymentDto
            {
                Name = request.Name,
                Image = request.Image,
                Replicas = request.Replicas,
                Labels = request.Labels.ToDictionary(k => k.Key, v => v.Value),
                EnvVars = request.EnvVars.ToDictionary(k => k.Key, v => v.Value),
                Ports = request.Ports.Select(p => new ContainerPortDto
                {
                    Name = p.Name,
                    ContainerPort = p.ContainerPort_,
                    Protocol = string.IsNullOrEmpty(p.Protocol) ? "TCP" : p.Protocol
                }).ToList(),
                RequestedBy = request.RequestedBy,
                Reason = request.Reason,
                CorrelationId = request.CorrelationId
            };
            var result = await _manager.CreateAsync(request.Cluster, request.Namespace, dto, context.CancellationToken);
            return new OperationReply { Success = result.Success, Message = result.Message, CorrelationId = result.CorrelationId };
        }
        catch (PolicyViolationException ex) { throw new RpcException(new Status(StatusCode.PermissionDenied, ex.Message)); }
        catch (Exception ex) { _logger.LogError(ex, "Create error"); throw new RpcException(new Status(StatusCode.Internal, "Internal error.")); }
    }

    public override async Task<OperationReply> Delete(DeleteDeploymentRequest request, ServerCallContext context)
    {
        try
        {
            var result = await _manager.DeleteAsync(request.Cluster, request.Namespace, request.Name, request.RequestedBy, request.Reason, request.CorrelationId, context.CancellationToken);
            return new OperationReply { Success = result.Success, Message = result.Message, CorrelationId = result.CorrelationId };
        }
        catch (PolicyViolationException ex) { throw new RpcException(new Status(StatusCode.PermissionDenied, ex.Message)); }
        catch (Exception ex) { _logger.LogError(ex, "Delete error"); throw new RpcException(new Status(StatusCode.Internal, "Internal error.")); }
    }

    public override async Task<OperationReply> Scale(ScaleDeploymentRequest request, ServerCallContext context)
    {
        try
        {
            var result = await _manager.ScaleAsync(request.Cluster, request.Namespace, request.Name, request.Replicas, request.RequestedBy, request.Reason, request.CorrelationId, context.CancellationToken);
            return new OperationReply { Success = result.Success, Message = result.Message, CorrelationId = result.CorrelationId };
        }
        catch (PolicyViolationException ex) { throw new RpcException(new Status(StatusCode.PermissionDenied, ex.Message)); }
        catch (Exception ex) { _logger.LogError(ex, "Scale error"); throw new RpcException(new Status(StatusCode.Internal, "Internal error.")); }
    }

    public override async Task<OperationReply> Restart(RestartDeploymentRequest request, ServerCallContext context)
    {
        try
        {
            var result = await _manager.RestartAsync(request.Cluster, request.Namespace, request.Name, request.RequestedBy, request.Reason, request.CorrelationId, context.CancellationToken);
            return new OperationReply { Success = result.Success, Message = result.Message, CorrelationId = result.CorrelationId };
        }
        catch (PolicyViolationException ex) { throw new RpcException(new Status(StatusCode.PermissionDenied, ex.Message)); }
        catch (Exception ex) { _logger.LogError(ex, "Restart error"); throw new RpcException(new Status(StatusCode.Internal, "Internal error.")); }
    }

    private static DeploymentStatusReply MapToReply(DeploymentStatusDto dto) => new()
    {
        Name = dto.Name, Namespace = dto.Namespace,
        DesiredReplicas = dto.DesiredReplicas, ReadyReplicas = dto.ReadyReplicas,
        AvailableReplicas = dto.AvailableReplicas, Strategy = dto.Strategy, Status = dto.Status
    };
}
