using k8s.Models;
using KuberManager.Service.Tests.Helpers;

namespace KuberManager.Service.Tests.Application;

public class StatefulSetManagerTests
{
    private readonly IKubernetesFacadeFactory _factory = Substitute.For<IKubernetesFacadeFactory>();
    private readonly IKubernetesFacade _facade = Substitute.For<IKubernetesFacade>();
    private readonly IOperationPolicy _policy = Substitute.For<IOperationPolicy>();
    private readonly IAuditService _audit = Substitute.For<IAuditService>();
    private readonly ILogger<StatefulSetManager> _logger = Substitute.For<ILogger<StatefulSetManager>>();
    private readonly StatefulSetManager _sut;

    public StatefulSetManagerTests()
    {
        _factory.For(Arg.Any<string>()).Returns(_facade);
        _sut = new StatefulSetManager(_factory, _policy, _audit, _logger);
    }

    [Fact]
    public async Task List_ReturnsAllStatefulSets()
    {
        _facade.ListStatefulSetsAsync("default")
               .Returns(new List<V1StatefulSet> { K8sModelFactory.StatefulSet("redis"), K8sModelFactory.StatefulSet("postgres") });

        var result = await _sut.ListAsync("prod", "default");

        result.Should().HaveCount(2);
        result.Select(r => r.Name).Should().Contain(new[] { "redis", "postgres" });
    }

    [Fact]
    public async Task Get_MapsFieldsCorrectly()
    {
        _facade.GetStatefulSetAsync("default", "redis")
               .Returns(K8sModelFactory.StatefulSet("redis", "default", desired: 3, ready: 3));

        var dto = await _sut.GetAsync("prod", "default", "redis");

        dto.Name.Should().Be("redis");
        dto.DesiredReplicas.Should().Be(3);
        dto.ReadyReplicas.Should().Be(3);
        dto.ServiceName.Should().Be("redis-svc");
    }

    [Fact]
    public async Task Scale_CallsFacadeWithCorrectArgs_AndAudits()
    {
        var result = await _sut.ScaleAsync("prod", "default", "redis", 5, "neo", "scale up", "corr-s1");

        result.Success.Should().BeTrue();
        await _facade.Received(1).PatchStatefulSetReplicasAsync("default", "redis", 5);
        await _audit.Received(1).RecordAsync(Arg.Is<AuditEntry>(a =>
            a.Action == "ScaleStatefulSet" && a.ResourceName == "redis" && a.Success), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Delete_CallsFacade_AndAudits()
    {
        var result = await _sut.DeleteAsync("prod", "default", "redis", "neo", "cleanup", "corr-s2");

        result.Success.Should().BeTrue();
        await _facade.Received(1).DeleteStatefulSetAsync("default", "redis");
        await _audit.Received(1).RecordAsync(Arg.Is<AuditEntry>(a =>
            a.Action == "DeleteStatefulSet" && a.Success), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Restart_CallsFacade_AndAudits()
    {
        var result = await _sut.RestartAsync("prod", "default", "redis", "neo", "patch", "corr-s3");

        result.Success.Should().BeTrue();
        await _audit.Received(1).RecordAsync(Arg.Is<AuditEntry>(a =>
            a.Action == "RestartStatefulSet" && a.Success), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Scale_FacadeThrows_ReturnsFailAndAudits()
    {
        _facade.PatchStatefulSetReplicasAsync("default", "redis", Arg.Any<int>())
               .ThrowsAsync(new Exception("scale failed"));

        var result = await _sut.ScaleAsync("prod", "default", "redis", 3, "neo", "test", "corr-s4");

        result.Success.Should().BeFalse();
        await _audit.Received(1).RecordAsync(Arg.Is<AuditEntry>(a => !a.Success && a.Action == "ScaleStatefulSet"), Arg.Any<CancellationToken>());
    }
}

public class DaemonSetManagerTests
{
    private readonly IKubernetesFacadeFactory _factory = Substitute.For<IKubernetesFacadeFactory>();
    private readonly IKubernetesFacade _facade = Substitute.For<IKubernetesFacade>();
    private readonly IOperationPolicy _policy = Substitute.For<IOperationPolicy>();
    private readonly IAuditService _audit = Substitute.For<IAuditService>();
    private readonly ILogger<DaemonSetManager> _logger = Substitute.For<ILogger<DaemonSetManager>>();
    private readonly DaemonSetManager _sut;

    public DaemonSetManagerTests()
    {
        _factory.For(Arg.Any<string>()).Returns(_facade);
        _sut = new DaemonSetManager(_factory, _policy, _audit, _logger);
    }

    [Fact]
    public async Task List_ReturnsAllDaemonSets()
    {
        _facade.ListDaemonSetsAsync("kube-system")
               .Returns(new List<V1DaemonSet> { K8sModelFactory.DaemonSet("fluentbit", "kube-system") });

        var result = await _sut.ListAsync("prod", "kube-system");

        result.Should().HaveCount(1);
        result[0].Name.Should().Be("fluentbit");
        result[0].DesiredNumberScheduled.Should().Be(3);
    }

    [Fact]
    public async Task Delete_Success_AuditsCorrectly()
    {
        var result = await _sut.DeleteAsync("prod", "default", "ds1", "neo", "cleanup", "corr-d1");

        result.Success.Should().BeTrue();
        await _facade.Received(1).DeleteDaemonSetAsync("default", "ds1");
    }

    [Fact]
    public async Task Delete_FacadeThrows_ReturnsFailAndAudits()
    {
        _facade.DeleteDaemonSetAsync("default", "ds1").ThrowsAsync(new Exception("forbidden"));

        var result = await _sut.DeleteAsync("prod", "default", "ds1", "neo", "test", "corr-d2");

        result.Success.Should().BeFalse();
        await _audit.Received(1).RecordAsync(Arg.Is<AuditEntry>(a => !a.Success && a.Action == "DeleteDaemonSet"), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Restart_CallsFacade_AndAudits()
    {
        var result = await _sut.RestartAsync("prod", "default", "ds1", "neo", "update", "corr-d3");

        result.Success.Should().BeTrue();
        await _audit.Received(1).RecordAsync(Arg.Is<AuditEntry>(a =>
            a.Action == "RestartDaemonSet" && a.Success), Arg.Any<CancellationToken>());
    }
}
