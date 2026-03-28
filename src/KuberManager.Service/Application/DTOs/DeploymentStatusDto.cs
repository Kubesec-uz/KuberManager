namespace KuberManager.Service.Application.DTOs;

public sealed class DeploymentStatusDto
{
    public string Name { get; init; } = string.Empty;
    public string Namespace { get; init; } = string.Empty;
    public int DesiredReplicas { get; init; }
    public int ReadyReplicas { get; init; }
    public int AvailableReplicas { get; init; }
    public string Strategy { get; init; } = string.Empty;
    public string Status { get; init; } = string.Empty;
}
