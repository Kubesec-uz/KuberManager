using KuberManager.Service.Infrastructure.Options;
using KuberManager.Service.Infrastructure.Policy;
using Microsoft.Extensions.Options;

namespace KuberManager.Service.Tests.Infrastructure;

public class OperationPolicyTests
{
    private static OperationPolicy Create(
        HashSet<string>? allowedNamespaces = null,
        HashSet<string>? blockedDeployments = null,
        int maxReplicas = 20)
    {
        var opts = Options.Create(new PolicyOptions
        {
            AllowedNamespaces = allowedNamespaces ?? new(),
            BlockedDeployments = blockedDeployments ?? new(),
            MaxReplicas = maxReplicas
        });
        return new OperationPolicy(opts, Substitute.For<ILogger<OperationPolicy>>());
    }

    // ─── EnsureNamespaceAllowed ──────────────────────────────────────────────

    [Fact]
    public void EnsureNamespaceAllowed_AllowListEmpty_ThrowsForAnyNs()
    {
        var policy = Create(allowedNamespaces: new());

        var act = () => policy.EnsureNamespaceAllowed("default");

        act.Should().Throw<PolicyViolationException>()
           .WithMessage("*default*");
    }

    [Fact]
    public void EnsureNamespaceAllowed_NsInList_DoesNotThrow()
    {
        var policy = Create(allowedNamespaces: new() { "default", "staging" });

        var act = () => policy.EnsureNamespaceAllowed("default");

        act.Should().NotThrow();
    }

    [Fact]
    public void EnsureNamespaceAllowed_NsNotInList_Throws()
    {
        var policy = Create(allowedNamespaces: new() { "production" });

        var act = () => policy.EnsureNamespaceAllowed("kube-system");

        act.Should().Throw<PolicyViolationException>()
           .WithMessage("*kube-system*");
    }

    [Theory]
    [InlineData("dev")]
    [InlineData("test")]
    [InlineData("qa")]
    public void EnsureNamespaceAllowed_MultipleAllowed_UnknownThrows(string blockedNs)
    {
        var policy = Create(allowedNamespaces: new() { "production" });

        var act = () => policy.EnsureNamespaceAllowed(blockedNs);

        act.Should().Throw<PolicyViolationException>();
    }

    // ─── EnsureDeploymentAllowed ─────────────────────────────────────────────

    [Fact]
    public void EnsureDeploymentAllowed_NotBlocked_DoesNotThrow()
    {
        var policy = Create(blockedDeployments: new() { "default/critical-api" });

        var act = () => policy.EnsureDeploymentAllowed("default", "my-api");

        act.Should().NotThrow();
    }

    [Fact]
    public void EnsureDeploymentAllowed_Blocked_Throws()
    {
        var policy = Create(blockedDeployments: new() { "default/critical-api" });

        var act = () => policy.EnsureDeploymentAllowed("default", "critical-api");

        act.Should().Throw<PolicyViolationException>()
           .WithMessage("*critical-api*");
    }

    [Fact]
    public void EnsureDeploymentAllowed_SameNameDifferentNs_DoesNotThrow()
    {
        var policy = Create(blockedDeployments: new() { "production/api" });

        var act = () => policy.EnsureDeploymentAllowed("staging", "api");

        act.Should().NotThrow();
    }

    // ─── EnsureReplicaLimit ───────────────────────────────────────────────────

    [Theory]
    [InlineData(1)]
    [InlineData(10)]
    [InlineData(20)]
    public void EnsureReplicaLimit_WithinLimit_DoesNotThrow(int replicas)
    {
        var policy = Create(maxReplicas: 20);

        var act = () => policy.EnsureReplicaLimit(replicas);

        act.Should().NotThrow();
    }

    [Fact]
    public void EnsureReplicaLimit_ExceedsMax_Throws()
    {
        var policy = Create(maxReplicas: 10);

        var act = () => policy.EnsureReplicaLimit(11);

        act.Should().Throw<PolicyViolationException>()
           .WithMessage("*11*");
    }

    [Fact]
    public void EnsureReplicaLimit_Negative_Throws()
    {
        var policy = Create();

        var act = () => policy.EnsureReplicaLimit(-1);

        act.Should().Throw<PolicyViolationException>()
           .WithMessage("*negative*");
    }

    [Fact]
    public void EnsureReplicaLimit_Zero_DoesNotThrow()
    {
        var policy = Create(maxReplicas: 20);

        var act = () => policy.EnsureReplicaLimit(0);

        act.Should().NotThrow();
    }
}
