using k8s.Models;
using KuberManager.Service.Domain;
using KuberManager.Service.Infrastructure.Audit;
using KuberManager.Service.Infrastructure.Kubernetes;
using KuberManager.Service.Infrastructure.Policy;

namespace KuberManager.Service.Application.DTOs;

public class ServiceAccountDto
{
    public string Name { get; set; } = string.Empty;
    public string Namespace { get; set; } = string.Empty;
    public List<string> Secrets { get; set; } = new();
    public string CreatedAt { get; set; } = string.Empty;
}

public class CreateServiceAccountDto
{
    public string Cluster { get; set; } = string.Empty;
    public string Namespace { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public Dictionary<string, string> Annotations { get; set; } = new();
    public string RequestedBy { get; set; } = string.Empty;
    public string Reason { get; set; } = string.Empty;
    public string CorrelationId { get; set; } = string.Empty;
}

public class PolicyRuleDto
{
    public List<string> ApiGroups { get; set; } = new();
    public List<string> Resources { get; set; } = new();
    public List<string> Verbs { get; set; } = new();
    public List<string> ResourceNames { get; set; } = new();
}

public class SubjectDto
{
    public string Kind { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public string Namespace { get; set; } = string.Empty;
    public string ApiGroup { get; set; } = string.Empty;
}

public class RoleDto
{
    public string Name { get; set; } = string.Empty;
    public string Namespace { get; set; } = string.Empty;
    public List<PolicyRuleDto> Rules { get; set; } = new();
    public string CreatedAt { get; set; } = string.Empty;
}

public class CreateRoleDto
{
    public string Cluster { get; set; } = string.Empty;
    public string Namespace { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public List<PolicyRuleDto> Rules { get; set; } = new();
    public string RequestedBy { get; set; } = string.Empty;
    public string Reason { get; set; } = string.Empty;
    public string CorrelationId { get; set; } = string.Empty;
}

public class ClusterRoleDto
{
    public string Name { get; set; } = string.Empty;
    public List<PolicyRuleDto> Rules { get; set; } = new();
    public string CreatedAt { get; set; } = string.Empty;
}

public class CreateClusterRoleDto
{
    public string Cluster { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public List<PolicyRuleDto> Rules { get; set; } = new();
    public string RequestedBy { get; set; } = string.Empty;
    public string Reason { get; set; } = string.Empty;
    public string CorrelationId { get; set; } = string.Empty;
}

public class RoleBindingDto
{
    public string Name { get; set; } = string.Empty;
    public string Namespace { get; set; } = string.Empty;
    public string RoleKind { get; set; } = string.Empty;
    public string RoleName { get; set; } = string.Empty;
    public List<SubjectDto> Subjects { get; set; } = new();
    public string CreatedAt { get; set; } = string.Empty;
}

public class CreateRoleBindingDto
{
    public string Cluster { get; set; } = string.Empty;
    public string Namespace { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public string RoleKind { get; set; } = string.Empty;
    public string RoleName { get; set; } = string.Empty;
    public List<SubjectDto> Subjects { get; set; } = new();
    public string RequestedBy { get; set; } = string.Empty;
    public string Reason { get; set; } = string.Empty;
    public string CorrelationId { get; set; } = string.Empty;
}

public class ClusterRoleBindingDto
{
    public string Name { get; set; } = string.Empty;
    public string RoleName { get; set; } = string.Empty;
    public List<SubjectDto> Subjects { get; set; } = new();
    public string CreatedAt { get; set; } = string.Empty;
}

public class CreateClusterRoleBindingDto
{
    public string Cluster { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public string RoleName { get; set; } = string.Empty;
    public List<SubjectDto> Subjects { get; set; } = new();
    public string RequestedBy { get; set; } = string.Empty;
    public string Reason { get; set; } = string.Empty;
    public string CorrelationId { get; set; } = string.Empty;
}

public class NetworkPolicyDto
{
    public string Name { get; set; } = string.Empty;
    public string Namespace { get; set; } = string.Empty;
    public List<string> PodSelectorLabels { get; set; } = new();
    public List<string> PolicyTypes { get; set; } = new();
    public string CreatedAt { get; set; } = string.Empty;
}

public class NetworkPolicyPeerDto
{
    public Dictionary<string, string> NamespaceSelector { get; set; } = new();
    public Dictionary<string, string> PodSelector { get; set; } = new();
    public string IpBlock { get; set; } = string.Empty;
}

public class CreateNetworkPolicyDto
{
    public string Cluster { get; set; } = string.Empty;
    public string Namespace { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public Dictionary<string, string> PodSelector { get; set; } = new();
    public List<string> PolicyTypes { get; set; } = new();
    public bool DenyAllIngress { get; set; }
    public bool DenyAllEgress { get; set; }
    public List<NetworkPolicyPeerDto> AllowIngressFrom { get; set; } = new();
    public List<NetworkPolicyPeerDto> AllowEgressTo { get; set; } = new();
    public string RequestedBy { get; set; } = string.Empty;
    public string Reason { get; set; } = string.Empty;
    public string CorrelationId { get; set; } = string.Empty;
}
