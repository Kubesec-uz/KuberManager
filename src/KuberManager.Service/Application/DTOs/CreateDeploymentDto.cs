namespace KuberManager.Service.Application.DTOs;

public sealed class CreateDeploymentDto
{
    public string Name { get; init; } = string.Empty;
    public string Image { get; init; } = string.Empty;
    public int Replicas { get; init; } = 1;
    public Dictionary<string, string> Labels { get; init; } = new();
    public Dictionary<string, string> EnvVars { get; init; } = new();
    public List<ContainerPortDto> Ports { get; init; } = new();
    public string RequestedBy { get; init; } = string.Empty;
    public string Reason { get; init; } = string.Empty;
    public string CorrelationId { get; init; } = string.Empty;
}

public sealed class ContainerPortDto
{
    public string Name { get; init; } = string.Empty;
    public int ContainerPort { get; init; }
    public string Protocol { get; init; } = "TCP";
}
