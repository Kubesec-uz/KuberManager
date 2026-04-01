using k8s.Models;
using KuberManager.Service.Application.DTOs;
using KuberManager.Service.Domain;
using KuberManager.Service.Infrastructure.Audit;
using KuberManager.Service.Infrastructure.Kubernetes;
using KuberManager.Service.Infrastructure.Policy;

namespace KuberManager.Service.Application;

public interface IJobManager
{
    Task<IReadOnlyList<JobDto>> ListAsync(string cluster, string ns, CancellationToken ct = default);
    Task<JobDto> GetAsync(string cluster, string ns, string name, CancellationToken ct = default);
    Task<OperationResult> CreateAsync(CreateJobDto dto, CancellationToken ct = default);
    Task<OperationResult> DeleteAsync(string cluster, string ns, string name, string requestedBy, string reason, string correlationId, CancellationToken ct = default);
}

public interface ICronJobManager
{
    Task<IReadOnlyList<CronJobDto>> ListAsync(string cluster, string ns, CancellationToken ct = default);
    Task<CronJobDto> GetAsync(string cluster, string ns, string name, CancellationToken ct = default);
    Task<OperationResult> CreateAsync(CreateCronJobDto dto, CancellationToken ct = default);
    Task<OperationResult> DeleteAsync(string cluster, string ns, string name, string requestedBy, string reason, string correlationId, CancellationToken ct = default);
    Task<OperationResult> SuspendAsync(string cluster, string ns, string name, bool suspend, string requestedBy, string reason, string correlationId, CancellationToken ct = default);
}

public sealed class JobManager : IJobManager
{
    private readonly IKubernetesFacadeFactory _k8sFactory;
    private readonly IOperationPolicy _policy;
    private readonly IAuditService _audit;
    private readonly ILogger<JobManager> _logger;

    public JobManager(IKubernetesFacadeFactory k8s, IOperationPolicy policy, IAuditService audit, ILogger<JobManager> logger)
    { _k8sFactory = k8s; _policy = policy; _audit = audit; _logger = logger; }

    public async Task<IReadOnlyList<JobDto>> ListAsync(string cluster, string ns, CancellationToken ct = default)
    {
        _policy.EnsureNamespaceAllowed(ns);
        return (await _k8sFactory.For(cluster).ListJobsAsync(ns, ct)).Select(MapToDto).ToList();
    }

    public async Task<JobDto> GetAsync(string cluster, string ns, string name, CancellationToken ct = default)
    {
        _policy.EnsureNamespaceAllowed(ns);
        return MapToDto(await _k8sFactory.For(cluster).GetJobAsync(ns, name, ct));
    }

    public async Task<OperationResult> CreateAsync(CreateJobDto dto, CancellationToken ct = default)
    {
        _policy.EnsureNamespaceAllowed(dto.Namespace);
        _logger.LogInformation("Creating job {Namespace}/{Name}", dto.Namespace, dto.Name);
        try
        {
            var labels = new Dictionary<string, string> { ["job-name"] = dto.Name };
            var job = new V1Job
            {
                Metadata = new V1ObjectMeta { Name = dto.Name, NamespaceProperty = dto.Namespace },
                Spec = new V1JobSpec
                {
                    Completions = dto.Completions,
                    Parallelism = dto.Parallelism,
                    Template = new V1PodTemplateSpec
                    {
                        Metadata = new V1ObjectMeta { Labels = labels },
                        Spec = new V1PodSpec
                        {
                            RestartPolicy = "Never",
                            Containers = new List<V1Container>
                            {
                                new V1Container
                                {
                                    Name = dto.Name,
                                    Image = dto.Image,
                                    Command = dto.Command.Count > 0 ? dto.Command : null,
                                    Env = dto.EnvVars.Select(e => new V1EnvVar { Name = e.Key, Value = e.Value }).ToList()
                                }
                            }
                        }
                    }
                }
            };
            await _k8sFactory.For(dto.Cluster).CreateJobAsync(dto.Namespace, job, ct);
            await _audit.RecordAsync(new AuditEntry { CorrelationId = dto.CorrelationId, RequestedBy = dto.RequestedBy, Action = "CreateJob", Cluster = dto.Cluster, Namespace = dto.Namespace, ResourceType = "Job", ResourceName = dto.Name, Reason = dto.Reason, Success = true }, ct);
            return OperationResult.Ok(dto.CorrelationId);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to create job {Namespace}/{Name}", dto.Namespace, dto.Name);
            await _audit.RecordAsync(new AuditEntry { CorrelationId = dto.CorrelationId, RequestedBy = dto.RequestedBy, Action = "CreateJob", Cluster = dto.Cluster, Namespace = dto.Namespace, ResourceType = "Job", ResourceName = dto.Name, Reason = dto.Reason, Success = false, ErrorMessage = ex.Message }, ct);
            return OperationResult.Fail(dto.CorrelationId, ex.Message);
        }
    }

    public async Task<OperationResult> DeleteAsync(string cluster, string ns, string name, string requestedBy, string reason, string correlationId, CancellationToken ct = default)
    {
        _policy.EnsureNamespaceAllowed(ns);
        try
        {
            await _k8sFactory.For(cluster).DeleteJobAsync(ns, name, ct);
            await _audit.RecordAsync(new AuditEntry { CorrelationId = correlationId, RequestedBy = requestedBy, Action = "DeleteJob", Cluster = cluster, Namespace = ns, ResourceType = "Job", ResourceName = name, Reason = reason, Success = true }, ct);
            return OperationResult.Ok(correlationId);
        }
        catch (Exception ex)
        {
            await _audit.RecordAsync(new AuditEntry { CorrelationId = correlationId, RequestedBy = requestedBy, Action = "DeleteJob", Cluster = cluster, Namespace = ns, ResourceType = "Job", ResourceName = name, Reason = reason, Success = false, ErrorMessage = ex.Message }, ct);
            return OperationResult.Fail(correlationId, ex.Message);
        }
    }

    private static JobDto MapToDto(V1Job j) => new()
    {
        Name = j.Metadata.Name,
        Namespace = j.Metadata.NamespaceProperty,
        Completions = j.Spec?.Completions ?? 1,
        Succeeded = j.Status?.Succeeded ?? 0,
        Failed = j.Status?.Failed ?? 0,
        Active = (j.Status?.Active ?? 0) > 0,
        Status = (j.Status?.Succeeded ?? 0) > 0 ? "Complete" : (j.Status?.Active ?? 0) > 0 ? "Running" : "Pending",
        CreatedAt = j.Metadata.CreationTimestamp?.ToString("O") ?? string.Empty
    };
}

public sealed class CronJobManager : ICronJobManager
{
    private readonly IKubernetesFacadeFactory _k8sFactory;
    private readonly IOperationPolicy _policy;
    private readonly IAuditService _audit;
    private readonly ILogger<CronJobManager> _logger;

    public CronJobManager(IKubernetesFacadeFactory k8s, IOperationPolicy policy, IAuditService audit, ILogger<CronJobManager> logger)
    { _k8sFactory = k8s; _policy = policy; _audit = audit; _logger = logger; }

    public async Task<IReadOnlyList<CronJobDto>> ListAsync(string cluster, string ns, CancellationToken ct = default)
    {
        _policy.EnsureNamespaceAllowed(ns);
        return (await _k8sFactory.For(cluster).ListCronJobsAsync(ns, ct)).Select(MapToDto).ToList();
    }

    public async Task<CronJobDto> GetAsync(string cluster, string ns, string name, CancellationToken ct = default)
    {
        _policy.EnsureNamespaceAllowed(ns);
        return MapToDto(await _k8sFactory.For(cluster).GetCronJobAsync(ns, name, ct));
    }

    public async Task<OperationResult> CreateAsync(CreateCronJobDto dto, CancellationToken ct = default)
    {
        _policy.EnsureNamespaceAllowed(dto.Namespace);
        _logger.LogInformation("Creating cronjob {Namespace}/{Name}", dto.Namespace, dto.Name);
        try
        {
            var labels = new Dictionary<string, string> { ["app"] = dto.Name };
            var cj = new V1CronJob
            {
                Metadata = new V1ObjectMeta { Name = dto.Name, NamespaceProperty = dto.Namespace },
                Spec = new V1CronJobSpec
                {
                    Schedule = dto.Schedule,
                    JobTemplate = new V1JobTemplateSpec
                    {
                        Spec = new V1JobSpec
                        {
                            Template = new V1PodTemplateSpec
                            {
                                Metadata = new V1ObjectMeta { Labels = labels },
                                Spec = new V1PodSpec
                                {
                                    RestartPolicy = "Never",
                                    Containers = new List<V1Container>
                                    {
                                        new V1Container
                                        {
                                            Name = dto.Name,
                                            Image = dto.Image,
                                            Command = dto.Command.Count > 0 ? dto.Command : null,
                                            Env = dto.EnvVars.Select(e => new V1EnvVar { Name = e.Key, Value = e.Value }).ToList()
                                        }
                                    }
                                }
                            }
                        }
                    }
                }
            };
            await _k8sFactory.For(dto.Cluster).CreateCronJobAsync(dto.Namespace, cj, ct);
            await _audit.RecordAsync(new AuditEntry { CorrelationId = dto.CorrelationId, RequestedBy = dto.RequestedBy, Action = "CreateCronJob", Cluster = dto.Cluster, Namespace = dto.Namespace, ResourceType = "CronJob", ResourceName = dto.Name, Reason = dto.Reason, Success = true }, ct);
            return OperationResult.Ok(dto.CorrelationId);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to create cronjob {Namespace}/{Name}", dto.Namespace, dto.Name);
            await _audit.RecordAsync(new AuditEntry { CorrelationId = dto.CorrelationId, RequestedBy = dto.RequestedBy, Action = "CreateCronJob", Cluster = dto.Cluster, Namespace = dto.Namespace, ResourceType = "CronJob", ResourceName = dto.Name, Reason = dto.Reason, Success = false, ErrorMessage = ex.Message }, ct);
            return OperationResult.Fail(dto.CorrelationId, ex.Message);
        }
    }

    public async Task<OperationResult> DeleteAsync(string cluster, string ns, string name, string requestedBy, string reason, string correlationId, CancellationToken ct = default)
    {
        _policy.EnsureNamespaceAllowed(ns);
        try
        {
            await _k8sFactory.For(cluster).DeleteCronJobAsync(ns, name, ct);
            await _audit.RecordAsync(new AuditEntry { CorrelationId = correlationId, RequestedBy = requestedBy, Action = "DeleteCronJob", Cluster = cluster, Namespace = ns, ResourceType = "CronJob", ResourceName = name, Reason = reason, Success = true }, ct);
            return OperationResult.Ok(correlationId);
        }
        catch (Exception ex)
        {
            await _audit.RecordAsync(new AuditEntry { CorrelationId = correlationId, RequestedBy = requestedBy, Action = "DeleteCronJob", Cluster = cluster, Namespace = ns, ResourceType = "CronJob", ResourceName = name, Reason = reason, Success = false, ErrorMessage = ex.Message }, ct);
            return OperationResult.Fail(correlationId, ex.Message);
        }
    }

    public async Task<OperationResult> SuspendAsync(string cluster, string ns, string name, bool suspend, string requestedBy, string reason, string correlationId, CancellationToken ct = default)
    {
        _policy.EnsureNamespaceAllowed(ns);
        try
        {
            await _k8sFactory.For(cluster).PatchCronJobSuspendAsync(ns, name, suspend, ct);
            var action = suspend ? "SuspendCronJob" : "ResumeCronJob";
            await _audit.RecordAsync(new AuditEntry { CorrelationId = correlationId, RequestedBy = requestedBy, Action = action, Cluster = cluster, Namespace = ns, ResourceType = "CronJob", ResourceName = name, Reason = reason, Success = true }, ct);
            return OperationResult.Ok(correlationId);
        }
        catch (Exception ex)
        {
            await _audit.RecordAsync(new AuditEntry { CorrelationId = correlationId, RequestedBy = requestedBy, Action = "SuspendCronJob", Cluster = cluster, Namespace = ns, ResourceType = "CronJob", ResourceName = name, Reason = reason, Success = false, ErrorMessage = ex.Message }, ct);
            return OperationResult.Fail(correlationId, ex.Message);
        }
    }

    private static CronJobDto MapToDto(V1CronJob cj) => new()
    {
        Name = cj.Metadata.Name,
        Namespace = cj.Metadata.NamespaceProperty,
        Schedule = cj.Spec?.Schedule ?? string.Empty,
        Suspended = cj.Spec?.Suspend ?? false,
        LastScheduleTime = cj.Status?.LastScheduleTime?.ToString("O") ?? string.Empty,
        Status = cj.Spec?.Suspend == true ? "Suspended" : "Active",
        CreatedAt = cj.Metadata.CreationTimestamp?.ToString("O") ?? string.Empty
    };
}
