namespace KuberManager.Service.Application.DTOs;

public sealed class PodInfoDto
{
    public string Name { get; init; } = string.Empty;
    public string Namespace { get; init; } = string.Empty;
    public string Phase { get; init; } = string.Empty;
    public string NodeName { get; init; } = string.Empty;
    public string PodIp { get; init; } = string.Empty;
    public string StartTime { get; init; } = string.Empty;
    public IReadOnlyList<ContainerStatusDto> Containers { get; init; } = [];
}

public sealed class ContainerStatusDto
{
    public string Name { get; init; } = string.Empty;
    public bool Ready { get; init; }
    public int RestartCount { get; init; }
    public string State { get; init; } = string.Empty;
}
