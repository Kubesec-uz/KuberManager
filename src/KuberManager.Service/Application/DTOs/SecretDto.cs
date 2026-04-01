namespace KuberManager.Service.Application.DTOs;

public class SecretDto
{
    public string Name { get; set; } = string.Empty;
    public string Namespace { get; set; } = string.Empty;
    public string Type { get; set; } = string.Empty;
    public Dictionary<string, string> Data { get; set; } = new();
    public string CreatedAt { get; set; } = string.Empty;
}

public class CreateSecretDto
{
    public string Cluster { get; set; } = string.Empty;
    public string Namespace { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public string Type { get; set; } = "Opaque";
    public Dictionary<string, string> StringData { get; set; } = new();
    public string RequestedBy { get; set; } = string.Empty;
    public string Reason { get; set; } = string.Empty;
    public string CorrelationId { get; set; } = string.Empty;
}
