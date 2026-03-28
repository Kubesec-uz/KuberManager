namespace KuberManager.Service.Application.DTOs;

public sealed class ConfigMapDto
{
    public string Name { get; init; } = string.Empty;
    public string Namespace { get; init; } = string.Empty;
    public Dictionary<string, string> Data { get; init; } = new();
    public string CreatedAt { get; init; } = string.Empty;
}
