namespace KuberManager.Service.Application.DTOs;

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
