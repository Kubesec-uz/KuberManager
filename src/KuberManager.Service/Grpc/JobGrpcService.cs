using Grpc.Core;
using KuberManager.Contracts;
using KuberManager.Service.Application;
using KuberManager.Service.Application.DTOs;
using KuberManager.Service.Infrastructure.Policy;

namespace KuberManager.Service.Grpc;

public sealed class JobGrpcService : JobService.JobServiceBase
{
    private readonly IJobManager _manager;
    private readonly ILogger<JobGrpcService> _logger;

    public JobGrpcService(IJobManager manager, ILogger<JobGrpcService> logger)
    { _manager = manager; _logger = logger; }

    public override async Task<JobListReply> List(ListJobsRequest request, ServerCallContext context)
    {
        try
        {
            var items = await _manager.ListAsync(request.Cluster, request.Namespace, context.CancellationToken);
            var reply = new JobListReply();
            reply.Items.AddRange(items.Select(Map));
            return reply;
        }
        catch (PolicyViolationException ex) { throw new RpcException(new Status(StatusCode.PermissionDenied, ex.Message)); }
        catch (Exception ex) { _logger.LogError(ex, "List Jobs error"); throw new RpcException(new Status(StatusCode.Internal, "Internal error.")); }
    }

    public override async Task<JobReply> Get(JobRef request, ServerCallContext context)
    {
        try { return Map(await _manager.GetAsync(request.Cluster, request.Namespace, request.Name, context.CancellationToken)); }
        catch (PolicyViolationException ex) { throw new RpcException(new Status(StatusCode.PermissionDenied, ex.Message)); }
        catch (k8s.Autorest.HttpOperationException ex) when (ex.Response.StatusCode == System.Net.HttpStatusCode.NotFound)
        { throw new RpcException(new Status(StatusCode.NotFound, $"Job {request.Name} not found.")); }
        catch (Exception ex) { _logger.LogError(ex, "Get Job error"); throw new RpcException(new Status(StatusCode.Internal, "Internal error.")); }
    }

    public override async Task<OperationReply> Create(CreateJobRequest request, ServerCallContext context)
    {
        try
        {
            var dto = new CreateJobDto
            {
                Cluster = request.Cluster, Namespace = request.Namespace, Name = request.Name,
                Image = request.Image, Command = request.Command.ToList(),
                EnvVars = request.EnvVars.ToDictionary(k => k.Key, v => v.Value),
                Completions = request.Completions > 0 ? request.Completions : 1,
                Parallelism = request.Parallelism > 0 ? request.Parallelism : 1,
                RequestedBy = request.RequestedBy, Reason = request.Reason, CorrelationId = request.CorrelationId
            };
            var result = await _manager.CreateAsync(dto, context.CancellationToken);
            return new OperationReply { Success = result.Success, Message = result.Message, CorrelationId = result.CorrelationId };
        }
        catch (PolicyViolationException ex) { throw new RpcException(new Status(StatusCode.PermissionDenied, ex.Message)); }
        catch (Exception ex) { _logger.LogError(ex, "Create Job error"); throw new RpcException(new Status(StatusCode.Internal, "Internal error.")); }
    }

    public override async Task<OperationReply> Delete(DeleteJobRequest request, ServerCallContext context)
    {
        try
        {
            var result = await _manager.DeleteAsync(request.Cluster, request.Namespace, request.Name, request.RequestedBy, request.Reason, request.CorrelationId, context.CancellationToken);
            return new OperationReply { Success = result.Success, Message = result.Message, CorrelationId = result.CorrelationId };
        }
        catch (PolicyViolationException ex) { throw new RpcException(new Status(StatusCode.PermissionDenied, ex.Message)); }
        catch (Exception ex) { _logger.LogError(ex, "Delete Job error"); throw new RpcException(new Status(StatusCode.Internal, "Internal error.")); }
    }

    private static JobReply Map(JobDto d) => new()
    {
        Name = d.Name, Namespace = d.Namespace, Completions = d.Completions,
        Succeeded = d.Succeeded, Failed = d.Failed, Active = d.Active, Status = d.Status, CreatedAt = d.CreatedAt
    };
}

public sealed class CronJobGrpcService : CronJobService.CronJobServiceBase
{
    private readonly ICronJobManager _manager;
    private readonly ILogger<CronJobGrpcService> _logger;

    public CronJobGrpcService(ICronJobManager manager, ILogger<CronJobGrpcService> logger)
    { _manager = manager; _logger = logger; }

    public override async Task<CronJobListReply> List(ListCronJobsRequest request, ServerCallContext context)
    {
        try
        {
            var items = await _manager.ListAsync(request.Cluster, request.Namespace, context.CancellationToken);
            var reply = new CronJobListReply();
            reply.Items.AddRange(items.Select(Map));
            return reply;
        }
        catch (PolicyViolationException ex) { throw new RpcException(new Status(StatusCode.PermissionDenied, ex.Message)); }
        catch (Exception ex) { _logger.LogError(ex, "List CronJobs error"); throw new RpcException(new Status(StatusCode.Internal, "Internal error.")); }
    }

    public override async Task<CronJobReply> Get(CronJobRef request, ServerCallContext context)
    {
        try { return Map(await _manager.GetAsync(request.Cluster, request.Namespace, request.Name, context.CancellationToken)); }
        catch (PolicyViolationException ex) { throw new RpcException(new Status(StatusCode.PermissionDenied, ex.Message)); }
        catch (k8s.Autorest.HttpOperationException ex) when (ex.Response.StatusCode == System.Net.HttpStatusCode.NotFound)
        { throw new RpcException(new Status(StatusCode.NotFound, $"CronJob {request.Name} not found.")); }
        catch (Exception ex) { _logger.LogError(ex, "Get CronJob error"); throw new RpcException(new Status(StatusCode.Internal, "Internal error.")); }
    }

    public override async Task<OperationReply> Create(CreateCronJobRequest request, ServerCallContext context)
    {
        try
        {
            var dto = new CreateCronJobDto
            {
                Cluster = request.Cluster, Namespace = request.Namespace, Name = request.Name,
                Schedule = request.Schedule, Image = request.Image, Command = request.Command.ToList(),
                EnvVars = request.EnvVars.ToDictionary(k => k.Key, v => v.Value),
                RequestedBy = request.RequestedBy, Reason = request.Reason, CorrelationId = request.CorrelationId
            };
            var result = await _manager.CreateAsync(dto, context.CancellationToken);
            return new OperationReply { Success = result.Success, Message = result.Message, CorrelationId = result.CorrelationId };
        }
        catch (PolicyViolationException ex) { throw new RpcException(new Status(StatusCode.PermissionDenied, ex.Message)); }
        catch (Exception ex) { _logger.LogError(ex, "Create CronJob error"); throw new RpcException(new Status(StatusCode.Internal, "Internal error.")); }
    }

    public override async Task<OperationReply> Delete(DeleteCronJobRequest request, ServerCallContext context)
    {
        try
        {
            var result = await _manager.DeleteAsync(request.Cluster, request.Namespace, request.Name, request.RequestedBy, request.Reason, request.CorrelationId, context.CancellationToken);
            return new OperationReply { Success = result.Success, Message = result.Message, CorrelationId = result.CorrelationId };
        }
        catch (PolicyViolationException ex) { throw new RpcException(new Status(StatusCode.PermissionDenied, ex.Message)); }
        catch (Exception ex) { _logger.LogError(ex, "Delete CronJob error"); throw new RpcException(new Status(StatusCode.Internal, "Internal error.")); }
    }

    public override async Task<OperationReply> Suspend(SuspendCronJobRequest request, ServerCallContext context)
    {
        try
        {
            var result = await _manager.SuspendAsync(request.Cluster, request.Namespace, request.Name, true, request.RequestedBy, request.Reason, request.CorrelationId, context.CancellationToken);
            return new OperationReply { Success = result.Success, Message = result.Message, CorrelationId = result.CorrelationId };
        }
        catch (PolicyViolationException ex) { throw new RpcException(new Status(StatusCode.PermissionDenied, ex.Message)); }
        catch (Exception ex) { _logger.LogError(ex, "Suspend CronJob error"); throw new RpcException(new Status(StatusCode.Internal, "Internal error.")); }
    }

    public override async Task<OperationReply> Resume(ResumeCronJobRequest request, ServerCallContext context)
    {
        try
        {
            var result = await _manager.SuspendAsync(request.Cluster, request.Namespace, request.Name, false, request.RequestedBy, request.Reason, request.CorrelationId, context.CancellationToken);
            return new OperationReply { Success = result.Success, Message = result.Message, CorrelationId = result.CorrelationId };
        }
        catch (PolicyViolationException ex) { throw new RpcException(new Status(StatusCode.PermissionDenied, ex.Message)); }
        catch (Exception ex) { _logger.LogError(ex, "Resume CronJob error"); throw new RpcException(new Status(StatusCode.Internal, "Internal error.")); }
    }

    private static CronJobReply Map(CronJobDto d) => new()
    {
        Name = d.Name, Namespace = d.Namespace, Schedule = d.Schedule,
        Suspended = d.Suspended, LastScheduleTime = d.LastScheduleTime, Status = d.Status, CreatedAt = d.CreatedAt
    };
}
