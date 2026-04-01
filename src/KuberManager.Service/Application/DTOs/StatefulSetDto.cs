using k8s.Models;
using KuberManager.Service.Domain;
using KuberManager.Service.Infrastructure.Audit;
using KuberManager.Service.Infrastructure.Kubernetes;
using KuberManager.Service.Infrastructure.Policy;

namespace KuberManager.Service.Application.DTOs;

public class StatefulSetDto
{
    public string Name { get; set; } = string.Empty;
    public string Namespace { get; set; } = string.Empty;
    public int DesiredReplicas { get; set; }
    public int ReadyReplicas { get; set; }
    public int CurrentReplicas { get; set; }
    public string ServiceName { get; set; } = string.Empty;
    public string Status { get; set; } = string.Empty;
    public string CreatedAt { get; set; } = string.Empty;
}

public class CreateStatefulSetDto
{
    public string Cluster { get; set; } = string.Empty;
    public string Namespace { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public string Image { get; set; } = string.Empty;
    public int Replicas { get; set; } = 1;
    public string ServiceName { get; set; } = string.Empty;
    public Dictionary<string, string> Labels { get; set; } = new();
    public Dictionary<string, string> EnvVars { get; set; } = new();
    public List<ContainerPortDto> Ports { get; set; } = new();
    public string RequestedBy { get; set; } = string.Empty;
    public string Reason { get; set; } = string.Empty;
    public string CorrelationId { get; set; } = string.Empty;
}
