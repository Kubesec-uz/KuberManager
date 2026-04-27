using k8s.Models;
using KuberManager.Service.Tests.Helpers;

namespace KuberManager.Service.Tests.Application;

public class ServiceAccountManagerTests
{
    private readonly IKubernetesFacadeFactory _factory = Substitute.For<IKubernetesFacadeFactory>();
    private readonly IKubernetesFacade _facade = Substitute.For<IKubernetesFacade>();
    private readonly IOperationPolicy _policy = Substitute.For<IOperationPolicy>();
    private readonly IAuditService _audit = Substitute.For<IAuditService>();
    private readonly ILogger<ServiceAccountManager> _logger = Substitute.For<ILogger<ServiceAccountManager>>();
    private readonly ServiceAccountManager _sut;

    public ServiceAccountManagerTests()
    {
        _factory.For(Arg.Any<string>()).Returns(_facade);
        _sut = new ServiceAccountManager(_factory, _policy, _audit, _logger);
    }

    [Fact]
    public async Task List_EnforcesPolicy_ReturnsMapped()
    {
        _facade.ListServiceAccountsAsync("default")
               .Returns(new List<V1ServiceAccount> { K8sModelFactory.ServiceAccount("app-sa", "default") });

        var result = await _sut.ListAsync("prod", "default");

        result.Should().HaveCount(1);
        result[0].Name.Should().Be("app-sa");
        _policy.Received(1).EnsureNamespaceAllowed("default");
    }

    [Fact]
    public async Task Create_WithAnnotations_AndAudits()
    {
        var dto = new CreateServiceAccountDto
        {
            Cluster = "prod", Namespace = "default", Name = "app-sa",
            Annotations = new() { ["eks.amazonaws.com/role-arn"] = "arn:aws:iam::123:role/my-role" },
            RequestedBy = "neo", Reason = "irsa", CorrelationId = "corr-sa1"
        };
        _facade.CreateServiceAccountAsync("default", Arg.Any<V1ServiceAccount>())
               .Returns(K8sModelFactory.ServiceAccount("app-sa"));

        var result = await _sut.CreateAsync(dto);

        result.Success.Should().BeTrue();
        await _audit.Received(1).RecordAsync(Arg.Is<AuditEntry>(a =>
            a.Action == "CreateServiceAccount" && a.ResourceName == "app-sa" && a.Success), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Delete_Success_AuditsCorrectly()
    {
        var result = await _sut.DeleteAsync("prod", "default", "app-sa", "neo", "cleanup", "corr-sa2");

        result.Success.Should().BeTrue();
        await _facade.Received(1).DeleteServiceAccountAsync("default", "app-sa");
        await _audit.Received(1).RecordAsync(Arg.Is<AuditEntry>(a =>
            a.Action == "DeleteServiceAccount" && a.Success), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Create_FacadeThrows_ReturnsFailAndAudits()
    {
        var dto = new CreateServiceAccountDto
        {
            Cluster = "prod", Namespace = "default", Name = "app-sa",
            RequestedBy = "neo", Reason = "test", CorrelationId = "corr-sa3"
        };
        _facade.CreateServiceAccountAsync("default", Arg.Any<V1ServiceAccount>())
               .ThrowsAsync(new Exception("already exists"));

        var result = await _sut.CreateAsync(dto);

        result.Success.Should().BeFalse();
        result.Message.Should().Contain("already exists");
        await _audit.Received(1).RecordAsync(Arg.Is<AuditEntry>(a => !a.Success), Arg.Any<CancellationToken>());
    }
}

public class RbacManagerTests
{
    private readonly IKubernetesFacadeFactory _factory = Substitute.For<IKubernetesFacadeFactory>();
    private readonly IKubernetesFacade _facade = Substitute.For<IKubernetesFacade>();
    private readonly IOperationPolicy _policy = Substitute.For<IOperationPolicy>();
    private readonly IAuditService _audit = Substitute.For<IAuditService>();
    private readonly ILogger<RbacManager> _logger = Substitute.For<ILogger<RbacManager>>();
    private readonly RbacManager _sut;

    public RbacManagerTests()
    {
        _factory.For(Arg.Any<string>()).Returns(_facade);
        _sut = new RbacManager(_factory, _policy, _audit, _logger);
    }

    [Fact]
    public async Task ListRoles_ReturnsMappedWithRules()
    {
        _facade.ListRolesAsync("default")
               .Returns(new List<V1Role> { K8sModelFactory.Role("pod-reader", "default") });

        var result = await _sut.ListRolesAsync("prod", "default");

        result.Should().HaveCount(1);
        result[0].Rules.Should().HaveCount(1);
        result[0].Rules[0].Verbs.Should().Contain("get");
        result[0].Rules[0].Resources.Should().Contain("pods");
    }

    [Fact]
    public async Task CreateRole_AndAudits()
    {
        var dto = new CreateRoleDto
        {
            Cluster = "prod", Namespace = "default", Name = "pod-reader",
            Rules = new() { new PolicyRuleDto { ApiGroups = new() { "" }, Resources = new() { "pods" }, Verbs = new() { "get", "list" } } },
            RequestedBy = "neo", Reason = "obs", CorrelationId = "corr-r1"
        };
        _facade.CreateRoleAsync("default", Arg.Any<V1Role>()).Returns(K8sModelFactory.Role("pod-reader"));

        var result = await _sut.CreateRoleAsync(dto);

        result.Success.Should().BeTrue();
        await _audit.Received(1).RecordAsync(Arg.Is<AuditEntry>(a =>
            a.Action == "CreateRole" && a.Success), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task DeleteRole_Success()
    {
        var result = await _sut.DeleteRoleAsync("prod", "default", "pod-reader", "neo", "remove", "corr-r2");

        result.Success.Should().BeTrue();
        await _facade.Received(1).DeleteRoleAsync("default", "pod-reader");
    }

    [Fact]
    public async Task ListClusterRoles_ReturnsMapped()
    {
        _facade.ListClusterRolesAsync()
               .Returns(new List<V1ClusterRole> { K8sModelFactory.ClusterRole("deploy-admin") });

        var result = await _sut.ListClusterRolesAsync("prod");

        result.Should().HaveCount(1);
        result[0].Name.Should().Be("deploy-admin");
        result[0].Rules[0].Resources.Should().Contain("deployments");
    }

    [Fact]
    public async Task CreateClusterRole_AndAudits()
    {
        var dto = new CreateClusterRoleDto
        {
            Cluster = "prod", Name = "deploy-admin",
            Rules = new() { new PolicyRuleDto { ApiGroups = new() { "apps" }, Resources = new() { "deployments" }, Verbs = new() { "*" } } },
            RequestedBy = "neo", Reason = "admin", CorrelationId = "corr-cr1"
        };
        _facade.CreateClusterRoleAsync(Arg.Any<V1ClusterRole>()).Returns(K8sModelFactory.ClusterRole("deploy-admin"));

        var result = await _sut.CreateClusterRoleAsync(dto);

        result.Success.Should().BeTrue();
        await _audit.Received(1).RecordAsync(Arg.Is<AuditEntry>(a =>
            a.Action == "CreateClusterRole" && a.Success), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task CreateRoleBinding_BindsSubjects_AndAudits()
    {
        var dto = new CreateRoleBindingDto
        {
            Cluster = "prod", Namespace = "default", Name = "binding",
            RoleKind = "Role", RoleName = "pod-reader",
            Subjects = new() { new SubjectDto { Kind = "ServiceAccount", Name = "default", Namespace = "default" } },
            RequestedBy = "neo", Reason = "bind", CorrelationId = "corr-rb1"
        };
        _facade.CreateRoleBindingAsync("default", Arg.Any<V1RoleBinding>()).Returns(K8sModelFactory.RoleBinding());

        var result = await _sut.CreateRoleBindingAsync(dto);

        result.Success.Should().BeTrue();
        await _audit.Received(1).RecordAsync(Arg.Is<AuditEntry>(a =>
            a.Action == "CreateRoleBinding" && a.Success), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task ListRoleBindings_ReturnsMapped()
    {
        _facade.ListRoleBindingsAsync("default")
               .Returns(new List<V1RoleBinding> { K8sModelFactory.RoleBinding("my-rb", "default") });

        var result = await _sut.ListRoleBindingsAsync("prod", "default");

        result.Should().HaveCount(1);
        result[0].RoleKind.Should().Be("Role");
        result[0].Subjects.Should().HaveCount(1);
        result[0].Subjects[0].Kind.Should().Be("ServiceAccount");
    }

    [Fact]
    public async Task CreateClusterRoleBinding_AndAudits()
    {
        var dto = new CreateClusterRoleBindingDto
        {
            Cluster = "prod", Name = "admin-crb", RoleName = "cluster-admin",
            Subjects = new() { new SubjectDto { Kind = "User", Name = "neo" } },
            RequestedBy = "neo", Reason = "full access", CorrelationId = "corr-crb1"
        };
        _facade.CreateClusterRoleBindingAsync(Arg.Any<V1ClusterRoleBinding>()).Returns(K8sModelFactory.ClusterRoleBinding("admin-crb"));

        var result = await _sut.CreateClusterRoleBindingAsync(dto);

        result.Success.Should().BeTrue();
        await _audit.Received(1).RecordAsync(Arg.Is<AuditEntry>(a =>
            a.Action == "CreateClusterRoleBinding" && a.Success), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task DeleteClusterRoleBinding_Success()
    {
        var result = await _sut.DeleteClusterRoleBindingAsync("prod", "admin-crb", "neo", "remove", "corr-crb2");

        result.Success.Should().BeTrue();
        await _facade.Received(1).DeleteClusterRoleBindingAsync("admin-crb");
    }

    [Fact]
    public async Task DeleteRole_FacadeThrows_ReturnsFail()
    {
        _facade.DeleteRoleAsync("default", "pod-reader").ThrowsAsync(new Exception("not found"));

        var result = await _sut.DeleteRoleAsync("prod", "default", "pod-reader", "neo", "test", "corr-r3");

        result.Success.Should().BeFalse();
    }
}

public class NetworkPolicyManagerTests
{
    private readonly IKubernetesFacadeFactory _factory = Substitute.For<IKubernetesFacadeFactory>();
    private readonly IKubernetesFacade _facade = Substitute.For<IKubernetesFacade>();
    private readonly IOperationPolicy _policy = Substitute.For<IOperationPolicy>();
    private readonly IAuditService _audit = Substitute.For<IAuditService>();
    private readonly ILogger<NetworkPolicyManager> _logger = Substitute.For<ILogger<NetworkPolicyManager>>();
    private readonly NetworkPolicyManager _sut;

    public NetworkPolicyManagerTests()
    {
        _factory.For(Arg.Any<string>()).Returns(_facade);
        _sut = new NetworkPolicyManager(_factory, _policy, _audit, _logger);
    }

    [Fact]
    public async Task List_ReturnsMappedPolicies()
    {
        _facade.ListNetworkPoliciesAsync("default")
               .Returns(new List<V1NetworkPolicy> { K8sModelFactory.NetworkPolicy("deny-all", "default") });

        var result = await _sut.ListAsync("prod", "default");

        result.Should().HaveCount(1);
        result[0].PolicyTypes.Should().Contain("Ingress");
        result[0].PodSelectorLabels.Should().Contain("app=test");
    }

    [Fact]
    public async Task Create_DenyAllIngress_Success()
    {
        var dto = new CreateNetworkPolicyDto
        {
            Cluster = "prod", Namespace = "default", Name = "deny-ingress",
            PodSelector = new() { ["app"] = "test" },
            PolicyTypes = new() { "Ingress" },
            DenyAllIngress = true,
            RequestedBy = "neo", Reason = "isolation", CorrelationId = "corr-np1"
        };
        _facade.CreateNetworkPolicyAsync("default", Arg.Any<V1NetworkPolicy>())
               .Returns(K8sModelFactory.NetworkPolicy("deny-ingress"));

        var result = await _sut.CreateAsync(dto);

        result.Success.Should().BeTrue();
        await _audit.Received(1).RecordAsync(Arg.Is<AuditEntry>(a =>
            a.Action == "CreateNetworkPolicy" && a.Success), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Create_WithIngressPeers_Success()
    {
        var dto = new CreateNetworkPolicyDto
        {
            Cluster = "prod", Namespace = "default", Name = "allow-monitoring",
            PodSelector = new() { ["app"] = "api" },
            PolicyTypes = new() { "Ingress" },
            AllowIngressFrom = new() { new NetworkPolicyPeerDto { NamespaceSelector = new() { ["name"] = "monitoring" } } },
            RequestedBy = "neo", Reason = "prometheus", CorrelationId = "corr-np2"
        };
        _facade.CreateNetworkPolicyAsync("default", Arg.Any<V1NetworkPolicy>())
               .Returns(K8sModelFactory.NetworkPolicy("allow-monitoring"));

        var result = await _sut.CreateAsync(dto);

        result.Success.Should().BeTrue();
    }

    [Fact]
    public async Task Delete_EnforcesPolicy_AndAudits()
    {
        var result = await _sut.DeleteAsync("prod", "default", "deny-all", "neo", "remove", "corr-np3");

        result.Success.Should().BeTrue();
        _policy.Received(1).EnsureNamespaceAllowed("default");
        await _facade.Received(1).DeleteNetworkPolicyAsync("default", "deny-all");
        await _audit.Received(1).RecordAsync(Arg.Is<AuditEntry>(a =>
            a.Action == "DeleteNetworkPolicy" && a.Success), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Create_NoDenyAll_NoRules_IngressIsNull()
    {
        V1NetworkPolicy? captured = null;
        _facade.CreateNetworkPolicyAsync("default", Arg.Do<V1NetworkPolicy>(np => captured = np))
               .Returns(K8sModelFactory.NetworkPolicy("empty-policy"));

        var dto = new CreateNetworkPolicyDto
        {
            Cluster = "prod", Namespace = "default", Name = "empty-policy",
            PolicyTypes = new() { "Ingress" },
            DenyAllIngress = false,
            // AllowIngressFrom bo'sh → Ingress rules null bo'lishi kerak (k8s da "no restriction")
            RequestedBy = "neo", Reason = "test", CorrelationId = "corr-np5"
        };

        await _sut.CreateAsync(dto);

        captured.Should().NotBeNull();
        captured!.Spec.Ingress.Should().BeNull();
    }

    [Fact]
    public async Task Create_DenyAllIngress_IngressIsEmptyList()
    {
        V1NetworkPolicy? captured = null;
        _facade.CreateNetworkPolicyAsync("default", Arg.Do<V1NetworkPolicy>(np => captured = np))
               .Returns(K8sModelFactory.NetworkPolicy("deny-all"));

        var dto = new CreateNetworkPolicyDto
        {
            Cluster = "prod", Namespace = "default", Name = "deny-all",
            PolicyTypes = new() { "Ingress" },
            DenyAllIngress = true,
            RequestedBy = "neo", Reason = "isolation", CorrelationId = "corr-np6"
        };

        await _sut.CreateAsync(dto);

        captured.Should().NotBeNull();
        // Bo'sh list = k8s deny-all ingress semantikasi
        captured!.Spec.Ingress.Should().NotBeNull().And.BeEmpty();
    }

    [Fact]
    public async Task Create_PolicyViolation_Throws()
    {
        _policy.When(p => p.EnsureNamespaceAllowed("kube-system"))
               .Do(_ => throw new PolicyViolationException("blocked"));

        var dto = new CreateNetworkPolicyDto
        {
            Cluster = "prod", Namespace = "kube-system", Name = "test",
            RequestedBy = "neo", Reason = "test", CorrelationId = "corr-np4"
        };

        await Assert.ThrowsAsync<PolicyViolationException>(() => _sut.CreateAsync(dto));
        await _facade.DidNotReceive().CreateNetworkPolicyAsync(Arg.Any<string>(), Arg.Any<V1NetworkPolicy>());
    }
}
