using k8s.Models;
using KuberManager.Service.Tests.Helpers;

namespace KuberManager.Service.Tests.Application;

public class JobManagerTests
{
    private readonly IKubernetesFacadeFactory _factory = Substitute.For<IKubernetesFacadeFactory>();
    private readonly IKubernetesFacade _facade = Substitute.For<IKubernetesFacade>();
    private readonly IOperationPolicy _policy = Substitute.For<IOperationPolicy>();
    private readonly IAuditService _audit = Substitute.For<IAuditService>();
    private readonly ILogger<JobManager> _logger = Substitute.For<ILogger<JobManager>>();
    private readonly JobManager _sut;

    public JobManagerTests()
    {
        _factory.For(Arg.Any<string>()).Returns(_facade);
        _sut = new JobManager(_factory, _policy, _audit, _logger);
    }

    [Fact]
    public async Task List_ReturnsMappedJobs()
    {
        _facade.ListJobsAsync("default")
               .Returns(new List<V1Job> { K8sModelFactory.Job("migrate", "default", succeeded: 1) });

        var result = await _sut.ListAsync("prod", "default");

        result.Should().HaveCount(1);
        result[0].Name.Should().Be("migrate");
        result[0].Succeeded.Should().Be(1);
    }

    [Fact]
    public async Task Create_BuildsJobCorrectly_AndAudits()
    {
        var dto = new CreateJobDto
        {
            Cluster = "prod", Namespace = "default", Name = "migrate",
            Image = "migrate:v1", Command = new() { "sh", "-c", "migrate.sh" },
            Completions = 1, Parallelism = 1,
            RequestedBy = "neo", Reason = "db migration", CorrelationId = "corr-j1"
        };
        _facade.CreateJobAsync("default", Arg.Any<V1Job>()).Returns(K8sModelFactory.Job("migrate"));

        var result = await _sut.CreateAsync(dto);

        result.Success.Should().BeTrue();
        await _audit.Received(1).RecordAsync(Arg.Is<AuditEntry>(a =>
            a.Action == "CreateJob" && a.ResourceName == "migrate" && a.Success), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Delete_Success_AuditsCorrectly()
    {
        var result = await _sut.DeleteAsync("prod", "default", "migrate", "neo", "done", "corr-j2");

        result.Success.Should().BeTrue();
        await _facade.Received(1).DeleteJobAsync("default", "migrate");
        await _audit.Received(1).RecordAsync(Arg.Is<AuditEntry>(a =>
            a.Action == "DeleteJob" && a.Success), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Create_FacadeThrows_ReturnsFailAndAudits()
    {
        var dto = new CreateJobDto
        {
            Cluster = "prod", Namespace = "default", Name = "fail-job",
            Image = "busybox", Completions = 1, Parallelism = 1,
            RequestedBy = "neo", Reason = "test", CorrelationId = "corr-j3"
        };
        _facade.CreateJobAsync("default", Arg.Any<V1Job>()).ThrowsAsync(new Exception("quota exceeded"));

        var result = await _sut.CreateAsync(dto);

        result.Success.Should().BeFalse();
        result.Message.Should().Contain("quota exceeded");
    }
}

public class CronJobManagerTests
{
    private readonly IKubernetesFacadeFactory _factory = Substitute.For<IKubernetesFacadeFactory>();
    private readonly IKubernetesFacade _facade = Substitute.For<IKubernetesFacade>();
    private readonly IOperationPolicy _policy = Substitute.For<IOperationPolicy>();
    private readonly IAuditService _audit = Substitute.For<IAuditService>();
    private readonly ILogger<CronJobManager> _logger = Substitute.For<ILogger<CronJobManager>>();
    private readonly CronJobManager _sut;

    public CronJobManagerTests()
    {
        _factory.For(Arg.Any<string>()).Returns(_facade);
        _sut = new CronJobManager(_factory, _policy, _audit, _logger);
    }

    [Fact]
    public async Task List_ReturnsMappedCronJobs()
    {
        _facade.ListCronJobsAsync("default")
               .Returns(new List<V1CronJob> { K8sModelFactory.CronJob("backup", "default", "0 2 * * *") });

        var result = await _sut.ListAsync("prod", "default");

        result.Should().HaveCount(1);
        result[0].Schedule.Should().Be("0 2 * * *");
        result[0].Suspended.Should().BeFalse();
    }

    [Fact]
    public async Task Get_MapsSchedule()
    {
        _facade.GetCronJobAsync("default", "backup")
               .Returns(K8sModelFactory.CronJob("backup", "default", "0 2 * * *"));

        var dto = await _sut.GetAsync("prod", "default", "backup");

        dto.Schedule.Should().Be("0 2 * * *");
    }

    [Fact]
    public async Task Suspend_True_CallsFacade_AndAudits()
    {
        _facade.PatchCronJobSuspendAsync("default", "backup", true).Returns(Task.CompletedTask);

        var result = await _sut.SuspendAsync("prod", "default", "backup", true, "neo", "maintenance", "corr-c1");

        result.Success.Should().BeTrue();
        await _facade.Received(1).PatchCronJobSuspendAsync("default", "backup", true);
        await _audit.Received(1).RecordAsync(Arg.Is<AuditEntry>(a =>
            a.Action == "SuspendCronJob" && a.Success), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Suspend_False_AuditsAsResume()
    {
        _facade.PatchCronJobSuspendAsync("default", "backup", false).Returns(Task.CompletedTask);

        var result = await _sut.SuspendAsync("prod", "default", "backup", false, "neo", "resume", "corr-c2");

        result.Success.Should().BeTrue();
        await _audit.Received(1).RecordAsync(Arg.Is<AuditEntry>(a =>
            a.Action == "ResumeCronJob" && a.Success), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Delete_Success_AuditsCorrectly()
    {
        var result = await _sut.DeleteAsync("prod", "default", "backup", "neo", "remove", "corr-c3");

        result.Success.Should().BeTrue();
        await _facade.Received(1).DeleteCronJobAsync("default", "backup");
    }
}
