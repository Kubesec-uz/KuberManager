namespace KuberManager.Service.Application.DTOs;

public class ServiceDto
{
    public string Name { get; set; } = string.Empty;
    public string Namespace { get; set; } = string.Empty;
    public string Type { get; set; } = string.Empty;
    public string ClusterIp { get; set; } = string.Empty;
    public List<ServicePortDto> Ports { get; set; } = new();
    public string CreatedAt { get; set; } = string.Empty;
}

public class ServicePortDto
{
    public string Name { get; set; } = string.Empty;
    public int Port { get; set; }
    public int TargetPort { get; set; }
    public string Protocol { get; set; } = "TCP";
    public int NodePort { get; set; }
}

public class CreateServiceDto
{
    public string Cluster { get; set; } = string.Empty;
    public string Namespace { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public string Type { get; set; } = "ClusterIP";
    public Dictionary<string, string> Selector { get; set; } = new();
    public List<ServicePortDto> Ports { get; set; } = new();
    public string RequestedBy { get; set; } = string.Empty;
    public string Reason { get; set; } = string.Empty;
    public string CorrelationId { get; set; } = string.Empty;
}
