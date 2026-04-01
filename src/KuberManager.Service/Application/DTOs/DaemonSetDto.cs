namespace KuberManager.Service.Application.DTOs;

public class DaemonSetDto
{
    public string Name { get; set; } = string.Empty;
    public string Namespace { get; set; } = string.Empty;
    public int DesiredNumberScheduled { get; set; }
    public int CurrentNumberScheduled { get; set; }
    public int NumberReady { get; set; }
    public string Status { get; set; } = string.Empty;
    public string CreatedAt { get; set; } = string.Empty;
}

public class CreateDaemonSetDto
{
    public string Cluster { get; set; } = string.Empty;
    public string Namespace { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public string Image { get; set; } = string.Empty;
    public Dictionary<string, string> Labels { get; set; } = new();
    public Dictionary<string, string> EnvVars { get; set; } = new();
    public string RequestedBy { get; set; } = string.Empty;
    public string Reason { get; set; } = string.Empty;
    public string CorrelationId { get; set; } = string.Empty;
}
