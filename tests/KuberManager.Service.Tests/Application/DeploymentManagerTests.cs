using k8s.Models;
using KuberManager.Service.Tests.Helpers;

namespace KuberManager.Service.Tests.Application;

public class DeploymentManagerTests
{
    private readonly IKubernetesFacadeFactory _factory = Substitute.For<IKubernetesFacadeFactory>();
    private readonly IKubernetesFacade _facade = Substitute.For<IKubernetesFacade>();
    private readonly IOperationPolicy _policy = Substitute.For<IOperationPolicy>();
    private readonly IAuditService _audit = Substitute.For<IAuditService>();
    private readonly ILogger<DeploymentManager> _logger = Substitute.For<ILogger<DeploymentManager>>();
    private readonly DeploymentManager _sut;

    public DeploymentManagerTests()
    {
        _factory.For(Arg.Any<string>()).Returns(_facade);
        _sut = new DeploymentManager(_factory, _policy, _audit, _logger);
    }

    // ─── List ───────────────────────────────────────────────────────────────

    [Fact]
    public async Task List_ReturnsAllDeployments()
    {
        _facade.ListDeploymentsAsync("default")
               .Returns(new List<V1Deployment> { K8sModelFactory.Deployment("api"), K8sModelFactory.Deployment("worker") });

        var result = await _sut.ListAsync("prod", "default");

        result.Should().HaveCount(2);
        result.Select(r => r.Name).Should().Contain(new[] { "api", "worker" });
    }

    [Fact]
    public async Task List_EnforcesNamespacePolicy()
    {
        _policy.When(p => p.EnsureNamespaceAllowed("kube-system"))
               .Do(_ => throw new PolicyViolationException("blocked"));

        await Assert.ThrowsAsync<PolicyViolationException>(
            () => _sut.ListAsync("prod", "kube-system"));
    }

    // ─── GetStatus ──────────────────────────────────────────────────────────

    [Fact]
    public async Task GetStatus_ReturnsMappedDto()
    {
        _facade.GetDeploymentAsync("default", "api")
               .Returns(K8sModelFactory.Deployment("api", "default", desired: 3, ready: 3));

        var dto = await _sut.GetStatusAsync("prod", "default", "api");

        dto.Name.Should().Be("api");
        dto.DesiredReplicas.Should().Be(3);
        dto.ReadyReplicas.Should().Be(3);
    }

    // ─── Scale ──────────────────────────────────────────────────────────────

    [Fact]
    public async Task Scale_Success_ReturnsOkAndAudits()
    {
        var dep = K8sModelFactory.Deployment("api", "default", 2, 2);
        _facade.GetDeploymentAsync("default", "api").Returns(dep);

        var result = await _sut.ScaleAsync("prod", "default", "api", 5, "neo", "load test", "corr-1");

        result.Success.Should().BeTrue();
        result.CorrelationId.Should().Be("corr-1");
        await _audit.Received(1).RecordAsync(Arg.Is<AuditEntry>(a =>
            a.Action == "ScaleDeployment" && a.Success && a.CorrelationId == "corr-1"), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Scale_ExceedsMaxReplicas_ThrowsPolicyViolation()
    {
        _policy.When(p => p.EnsureReplicaLimit(Arg.Is<int>(r => r > 50)))
               .Do(_ => throw new PolicyViolationException("too many replicas"));

        await Assert.ThrowsAsync<PolicyViolationException>(
            () => _sut.ScaleAsync("prod", "default", "api", 100, "neo", "test", "corr-2"));
    }

    [Fact]
    public async Task Scale_FacadeThrows_ReturnsFailAndAudits()
    {
        _facade.PatchDeploymentReplicasAsync("default", "api", 3).ThrowsAsync(new Exception("k8s timeout"));

        var result = await _sut.ScaleAsync("prod", "default", "api", 3, "neo", "test", "corr-3");

        result.Success.Should().BeFalse();
        result.Message.Should().Contain("k8s timeout");
        await _audit.Received(1).RecordAsync(Arg.Is<AuditEntry>(a =>
            a.Action == "ScaleDeployment" && !a.Success), Arg.Any<CancellationToken>());
    }

    // ─── Delete ─────────────────────────────────────────────────────────────

    [Fact]
    public async Task Delete_Success_ReturnsOkAndAudits()
    {
        var result = await _sut.DeleteAsync("prod", "default", "api", "neo", "cleanup", "corr-4");

        result.Success.Should().BeTrue();
        await _facade.Received(1).DeleteDeploymentAsync("default", "api");
        await _audit.Received(1).RecordAsync(Arg.Is<AuditEntry>(a =>
            a.Action == "DeleteDeployment" && a.Success), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Delete_FacadeThrows_ReturnsFailAndAudits()
    {
        _facade.DeleteDeploymentAsync("default", "api").ThrowsAsync(new Exception("not found"));

        var result = await _sut.DeleteAsync("prod", "default", "api", "neo", "test", "corr-5");

        result.Success.Should().BeFalse();
        await _audit.Received(1).RecordAsync(Arg.Is<AuditEntry>(a => !a.Success), Arg.Any<CancellationToken>());
    }

    // ─── Create ─────────────────────────────────────────────────────────────

    [Fact]
    public async Task Create_Success_ReturnsOkAndAudits()
    {
        var dto = new CreateDeploymentDto
        {
            Name = "api", Image = "nginx:latest", Replicas = 2,
            Labels = new() { ["app"] = "api" },
            RequestedBy = "neo", Reason = "initial deploy", CorrelationId = "corr-6"
        };
        _facade.CreateDeploymentAsync(Arg.Any<string>(), Arg.Any<V1Deployment>())
               .Returns(K8sModelFactory.Deployment("api"));

        var result = await _sut.CreateAsync("prod", "default", dto);

        result.Success.Should().BeTrue();
        await _audit.Received(1).RecordAsync(Arg.Is<AuditEntry>(a =>
            a.Action == "CreateDeployment" && a.Success && a.ResourceName == "api"), Arg.Any<CancellationToken>());
    }

    // ─── Restart ────────────────────────────────────────────────────────────

    [Fact]
    public async Task Restart_Success_ReturnsOkAndAudits()
    {
        var result = await _sut.RestartAsync("prod", "default", "api", "neo", "rolling update", "corr-7");

        result.Success.Should().BeTrue();
        await _audit.Received(1).RecordAsync(Arg.Is<AuditEntry>(a =>
            a.Action == "RestartDeployment" && a.Success), Arg.Any<CancellationToken>());
    }
}
