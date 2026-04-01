namespace KuberManager.Service.Application.DTOs;

public class JobDto
{
    public string Name { get; set; } = string.Empty;
    public string Namespace { get; set; } = string.Empty;
    public int Completions { get; set; }
    public int Succeeded { get; set; }
    public int Failed { get; set; }
    public bool Active { get; set; }
    public string Status { get; set; } = string.Empty;
    public string CreatedAt { get; set; } = string.Empty;
}

public class CreateJobDto
{
    public string Cluster { get; set; } = string.Empty;
    public string Namespace { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public string Image { get; set; } = string.Empty;
    public List<string> Command { get; set; } = new();
    public Dictionary<string, string> EnvVars { get; set; } = new();
    public int Completions { get; set; } = 1;
    public int Parallelism { get; set; } = 1;
    public string RequestedBy { get; set; } = string.Empty;
    public string Reason { get; set; } = string.Empty;
    public string CorrelationId { get; set; } = string.Empty;
}

public class CronJobDto
{
    public string Name { get; set; } = string.Empty;
    public string Namespace { get; set; } = string.Empty;
    public string Schedule { get; set; } = string.Empty;
    public bool Suspended { get; set; }
    public string LastScheduleTime { get; set; } = string.Empty;
    public string Status { get; set; } = string.Empty;
    public string CreatedAt { get; set; } = string.Empty;
}

public class CreateCronJobDto
{
    public string Cluster { get; set; } = string.Empty;
    public string Namespace { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public string Schedule { get; set; } = string.Empty;
    public string Image { get; set; } = string.Empty;
    public List<string> Command { get; set; } = new();
    public Dictionary<string, string> EnvVars { get; set; } = new();
    public string RequestedBy { get; set; } = string.Empty;
    public string Reason { get; set; } = string.Empty;
    public string CorrelationId { get; set; } = string.Empty;
}
