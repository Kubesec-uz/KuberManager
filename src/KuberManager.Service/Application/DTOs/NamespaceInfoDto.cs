namespace KuberManager.Service.Application.DTOs;

public sealed class NamespaceInfoDto
{
    public string Name { get; init; } = string.Empty;
    public string Status { get; init; } = string.Empty;
    public string CreatedAt { get; init; } = string.Empty;
    public Dictionary<string, string> Labels { get; init; } = new();
}
