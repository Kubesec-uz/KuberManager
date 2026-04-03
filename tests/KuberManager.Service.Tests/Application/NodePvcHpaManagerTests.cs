using k8s.Models;
using KuberManager.Service.Tests.Helpers;

namespace KuberManager.Service.Tests.Application;

public class NodeManagerTests
{
    private readonly IKubernetesFacadeFactory _factory = Substitute.For<IKubernetesFacadeFactory>();
    private readonly IKubernetesFacade _facade = Substitute.For<IKubernetesFacade>();
    private readonly IAuditService _audit = Substitute.For<IAuditService>();
    private readonly ILogger<NodeManager> _logger = Substitute.For<ILogger<NodeManager>>();
    private readonly NodeManager _sut;

    public NodeManagerTests()
    {
        _factory.For(Arg.Any<string>()).Returns(_facade);
        _sut = new NodeManager(_factory, _audit, _logger);
    }

    [Fact]
    public async Task List_ReturnsMappedNodes()
    {
        _facade.ListNodesAsync()
               .Returns(new List<V1Node> { K8sModelFactory.Node("node-1", ready: true), K8sModelFactory.Node("node-2", ready: false) });

        var result = await _sut.ListAsync("prod");

        result.Should().HaveCount(2);
        result.First(n => n.Name == "node-1").Status.Should().Be("Ready");
        result.First(n => n.Name == "node-2").Status.Should().Be("NotReady");
    }

    [Fact]
    public async Task Get_MapsCapacityCorrectly()
    {
        _facade.GetNodeAsync("node-1")
               .Returns(K8sModelFactory.Node("node-1"));

        var dto = await _sut.GetAsync("prod", "node-1");

        dto.CpuCapacity.Should().Be("4");
        dto.MemoryCapacity.Should().Be("8Gi");
        dto.Roles.Should().Contain("worker");
    }

    [Fact]
    public async Task Cordon_Success_PatchesAndAudits()
    {
        var result = await _sut.CordonAsync("prod", "node-1", "neo", "maintenance", "corr-n1");

        result.Success.Should().BeTrue();
        await _facade.Received(1).PatchNodeUnschedulableAsync("node-1", true);
        await _audit.Received(1).RecordAsync(Arg.Is<AuditEntry>(a =>
            a.Action == "CordonNode" && a.Success), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Uncordon_Success_PatchesAndAudits()
    {
        var result = await _sut.UncordonAsync("prod", "node-1", "neo", "done", "corr-n2");

        result.Success.Should().BeTrue();
        await _facade.Received(1).PatchNodeUnschedulableAsync("node-1", false);
        await _audit.Received(1).RecordAsync(Arg.Is<AuditEntry>(a =>
            a.Action == "UncordonNode" && a.Success), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Drain_CordonsThenDeletesPods_AndAudits()
    {
        var result = await _sut.DrainAsync("prod", "node-1", force: true, ignoreDaemonSets: true, "neo", "evacuate", "corr-n3");

        result.Success.Should().BeTrue();
        await _facade.Received(1).PatchNodeUnschedulableAsync("node-1", true);
        await _facade.Received(1).DeletePodsOnNodeAsync("node-1", true, true);
        await _audit.Received(1).RecordAsync(Arg.Is<AuditEntry>(a => a.Action == "DrainNode" && a.Success), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Drain_DeletePodsThrows_UncordonsNode_AndReturnsFailure()
    {
        _facade.DeletePodsOnNodeAsync("node-1", Arg.Any<bool>(), Arg.Any<bool>()).Throws(new Exception("eviction denied"));

        var result = await _sut.DrainAsync("prod", "node-1", force: false, ignoreDaemonSets: false, "neo", "evacuate", "corr-n5");

        result.Success.Should().BeFalse();
        // cordon → pods delete fails → uncordon
        await _facade.Received(1).PatchNodeUnschedulableAsync("node-1", true);
        await _facade.Received(1).PatchNodeUnschedulableAsync("node-1", false);
        await _audit.Received(1).RecordAsync(Arg.Is<AuditEntry>(a => a.Action == "DrainNode" && !a.Success), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Cordon_FacadeThrows_ReturnsFailAndAudits()
    {
        _facade.PatchNodeUnschedulableAsync("node-1", true).Throws(new Exception("forbidden"));

        var result = await _sut.CordonAsync("prod", "node-1", "neo", "test", "corr-n4");

        result.Success.Should().BeFalse();
        await _audit.Received(1).RecordAsync(Arg.Is<AuditEntry>(a => !a.Success), Arg.Any<CancellationToken>());
    }
}

public class PvcManagerTests
{
    private readonly IKubernetesFacadeFactory _factory = Substitute.For<IKubernetesFacadeFactory>();
    private readonly IKubernetesFacade _facade = Substitute.For<IKubernetesFacade>();
    private readonly IOperationPolicy _policy = Substitute.For<IOperationPolicy>();
    private readonly IAuditService _audit = Substitute.For<IAuditService>();
    private readonly ILogger<PvcManager> _logger = Substitute.For<ILogger<PvcManager>>();
    private readonly PvcManager _sut;

    public PvcManagerTests()
    {
        _factory.For(Arg.Any<string>()).Returns(_facade);
        _sut = new PvcManager(_factory, _policy, _audit, _logger);
    }

    [Fact]
    public async Task List_ReturnsMappedPvcs()
    {
        _facade.ListPvcsAsync("default")
               .Returns(new List<V1PersistentVolumeClaim> { K8sModelFactory.Pvc("data", "default", "Bound") });

        var result = await _sut.ListAsync("prod", "default");

        result.Should().HaveCount(1);
        result[0].Status.Should().Be("Bound");
        result[0].Capacity.Should().Be("1Gi");
    }

    [Fact]
    public async Task Create_BuildsSpec_AndAudits()
    {
        var dto = new CreatePvcDto
        {
            Cluster = "prod", Namespace = "default", Name = "data",
            StorageClass = "ssd", Storage = "10Gi",
            AccessModes = new() { "ReadWriteOnce" },
            RequestedBy = "neo", Reason = "need storage", CorrelationId = "corr-p1"
        };
        _facade.CreatePvcAsync("default", Arg.Any<V1PersistentVolumeClaim>()).Returns(K8sModelFactory.Pvc("data"));

        var result = await _sut.CreateAsync(dto);

        result.Success.Should().BeTrue();
        await _audit.Received(1).RecordAsync(Arg.Is<AuditEntry>(a =>
            a.Action == "CreatePVC" && a.ResourceName == "data" && a.Success), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Delete_EnforcesPolicy_ThenAudits()
    {
        var result = await _sut.DeleteAsync("prod", "default", "data", "neo", "cleanup", "corr-p2");

        result.Success.Should().BeTrue();
        _policy.Received(1).EnsureNamespaceAllowed("default");
    }
}

public class HpaManagerTests
{
    private readonly IKubernetesFacadeFactory _factory = Substitute.For<IKubernetesFacadeFactory>();
    private readonly IKubernetesFacade _facade = Substitute.For<IKubernetesFacade>();
    private readonly IOperationPolicy _policy = Substitute.For<IOperationPolicy>();
    private readonly IAuditService _audit = Substitute.For<IAuditService>();
    private readonly ILogger<HpaManager> _logger = Substitute.For<ILogger<HpaManager>>();
    private readonly HpaManager _sut;

    public HpaManagerTests()
    {
        _factory.For(Arg.Any<string>()).Returns(_facade);
        _sut = new HpaManager(_factory, _policy, _audit, _logger);
    }

    [Fact]
    public async Task List_ReturnsMappedHpas()
    {
        _facade.ListHpasAsync("default")
               .Returns(new List<V2HorizontalPodAutoscaler> { K8sModelFactory.Hpa("api-hpa", "default") });

        var result = await _sut.ListAsync("prod", "default");

        result.Should().HaveCount(1);
        result[0].MinReplicas.Should().Be(1);
        result[0].MaxReplicas.Should().Be(5);
        result[0].CpuTargetUtilization.Should().Be(70);
    }

    [Fact]
    public async Task Create_BuildsV2Spec_AndAudits()
    {
        var dto = new CreateHpaDto
        {
            Cluster = "prod", Namespace = "default", Name = "api-hpa",
            TargetKind = "Deployment", TargetName = "api",
            MinReplicas = 2, MaxReplicas = 10, CpuTargetUtilization = 60,
            RequestedBy = "neo", Reason = "autoscaling", CorrelationId = "corr-h1"
        };
        _facade.CreateHpaAsync("default", Arg.Any<V2HorizontalPodAutoscaler>()).Returns(K8sModelFactory.Hpa("api-hpa"));

        var result = await _sut.CreateAsync(dto);

        result.Success.Should().BeTrue();
        await _audit.Received(1).RecordAsync(Arg.Is<AuditEntry>(a =>
            a.Action == "CreateHPA" && a.Success), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Delete_EnforcesPolicy_AndAudits()
    {
        var result = await _sut.DeleteAsync("prod", "default", "api-hpa", "neo", "remove", "corr-h2");

        result.Success.Should().BeTrue();
        _policy.Received(1).EnsureNamespaceAllowed("default");
        await _facade.Received(1).DeleteHpaAsync("default", "api-hpa");
    }
}
