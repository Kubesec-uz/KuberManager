using Grpc.Core;
using KuberManager.Contracts;
using KuberManager.Service.Application;
using KuberManager.Service.Infrastructure.Policy;

namespace KuberManager.Service.Grpc;

public sealed class PodGrpcService : PodService.PodServiceBase
{
    private readonly IPodManager _manager;
    private readonly ILogger<PodGrpcService> _logger;

    public PodGrpcService(IPodManager manager, ILogger<PodGrpcService> logger)
    {
        _manager = manager;
        _logger = logger;
    }

    public override async Task<PodListReply> List(ListPodsRequest request, ServerCallContext context)
    {
        try
        {
            var pods = await _manager.ListAsync(request.Cluster, request.Namespace, request.AppLabel, context.CancellationToken);
            var reply = new PodListReply();
            reply.Pods.AddRange(pods.Select(MapToReply));
            return reply;
        }
        catch (PolicyViolationException ex) { throw new RpcException(new Status(StatusCode.PermissionDenied, ex.Message)); }
        catch (Exception ex) { _logger.LogError(ex, "List pods error"); throw new RpcException(new Status(StatusCode.Internal, "Internal error.")); }
    }

    public override async Task<PodInfoReply> Get(PodRef request, ServerCallContext context)
    {
        try
        {
            var pod = await _manager.GetAsync(request.Cluster, request.Namespace, request.PodName, context.CancellationToken);
            return MapToReply(pod);
        }
        catch (PolicyViolationException ex) { throw new RpcException(new Status(StatusCode.PermissionDenied, ex.Message)); }
        catch (k8s.Autorest.HttpOperationException ex) when (ex.Response.StatusCode == System.Net.HttpStatusCode.NotFound)
        { throw new RpcException(new Status(StatusCode.NotFound, $"Pod '{request.PodName}' not found.")); }
        catch (Exception ex) { _logger.LogError(ex, "Get pod error"); throw new RpcException(new Status(StatusCode.Internal, "Internal error.")); }
    }

    public override async Task<PodLogsReply> GetLogs(PodLogsRequest request, ServerCallContext context)
    {
        try
        {
            var logs = await _manager.GetLogsAsync(
                request.Cluster, request.Namespace, request.PodName,
                string.IsNullOrEmpty(request.ContainerName) ? null : request.ContainerName,
                request.TailLines > 0 ? request.TailLines : null,
                context.CancellationToken);
            return new PodLogsReply { PodName = request.PodName, Content = logs };
        }
        catch (PolicyViolationException ex) { throw new RpcException(new Status(StatusCode.PermissionDenied, ex.Message)); }
        catch (Exception ex) { _logger.LogError(ex, "GetLogs error"); throw new RpcException(new Status(StatusCode.Internal, "Internal error.")); }
    }

    public override async Task<OperationReply> Delete(DeletePodRequest request, ServerCallContext context)
    {
        try
        {
            var result = await _manager.DeleteAsync(request.Cluster, request.Namespace, request.PodName, request.RequestedBy, request.Reason, request.CorrelationId, context.CancellationToken);
            return new OperationReply { Success = result.Success, Message = result.Message, CorrelationId = result.CorrelationId };
        }
        catch (PolicyViolationException ex) { throw new RpcException(new Status(StatusCode.PermissionDenied, ex.Message)); }
        catch (Exception ex) { _logger.LogError(ex, "Delete pod error"); throw new RpcException(new Status(StatusCode.Internal, "Internal error.")); }
    }

    public override async Task Exec(ExecRequest request, IServerStreamWriter<ExecReply> responseStream, ServerCallContext context)
    {
        try
        {
            var command = request.Command.ToArray();
            await foreach (var (output, isStderr) in _manager.ExecAsync(
                request.Cluster, request.Namespace, request.PodName,
                request.ContainerName, command,
                request.RequestedBy, request.CorrelationId,
                context.CancellationToken))
            {
                await responseStream.WriteAsync(new ExecReply
                {
                    Output = output,
                    IsStderr = isStderr,
                    IsDone = false
                });
            }
            await responseStream.WriteAsync(new ExecReply { IsDone = true, ExitCode = 0 });
        }
        catch (PolicyViolationException ex) { throw new RpcException(new Status(StatusCode.PermissionDenied, ex.Message)); }
        catch (Exception ex) { _logger.LogError(ex, "Exec error"); throw new RpcException(new Status(StatusCode.Internal, "Internal error.")); }
    }

    private static PodInfoReply MapToReply(Application.DTOs.PodInfoDto dto)
    {
        var reply = new PodInfoReply
        {
            Name = dto.Name, Namespace = dto.Namespace, Phase = dto.Phase,
            NodeName = dto.NodeName, PodIp = dto.PodIp, StartTime = dto.StartTime
        };
        reply.Containers.AddRange(dto.Containers.Select(c => new ContainerStatusReply
        {
            Name = c.Name, Ready = c.Ready, RestartCount = c.RestartCount, State = c.State
        }));
        return reply;
    }
}
