namespace KuberManager.Service.Application.DTOs;

public class IngressDto
{
    public string Name { get; set; } = string.Empty;
    public string Namespace { get; set; } = string.Empty;
    public string IngressClass { get; set; } = string.Empty;
    public List<IngressRuleDto> Rules { get; set; } = new();
    public string CreatedAt { get; set; } = string.Empty;
}

public class IngressRuleDto
{
    public string Host { get; set; } = string.Empty;
    public List<IngressPathDto> Paths { get; set; } = new();
}

public class IngressPathDto
{
    public string Path { get; set; } = string.Empty;
    public string PathType { get; set; } = string.Empty;
    public string ServiceName { get; set; } = string.Empty;
    public int ServicePort { get; set; }
}

public class CreateIngressDto
{
    public string Cluster { get; set; } = string.Empty;
    public string Namespace { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public string IngressClass { get; set; } = string.Empty;
    public List<IngressRuleDto> Rules { get; set; } = new();
    public string RequestedBy { get; set; } = string.Empty;
    public string Reason { get; set; } = string.Empty;
    public string CorrelationId { get; set; } = string.Empty;
}
