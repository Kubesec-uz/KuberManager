namespace KuberManager.Service.Application.DTOs;

// ─── Shared ──────────────────────────────────────────────────────────────────

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

// ─── Role ────────────────────────────────────────────────────────────────────

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

// ─── ClusterRole ─────────────────────────────────────────────────────────────

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

// ─── RoleBinding ─────────────────────────────────────────────────────────────

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

// ─── ClusterRoleBinding ───────────────────────────────────────────────────────

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
