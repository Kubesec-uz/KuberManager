namespace KuberManager.Service.Application.DTOs;

public class NodeDto
{
    public string Name { get; set; } = string.Empty;
    public string Status { get; set; } = string.Empty;
    public string Roles { get; set; } = string.Empty;
    public string OsImage { get; set; } = string.Empty;
    public string KernelVersion { get; set; } = string.Empty;
    public string ContainerRuntime { get; set; } = string.Empty;
    public string CpuCapacity { get; set; } = string.Empty;
    public string MemoryCapacity { get; set; } = string.Empty;
    public bool Unschedulable { get; set; }
    public string CreatedAt { get; set; } = string.Empty;
}

public class PvcDto
{
    public string Name { get; set; } = string.Empty;
    public string Namespace { get; set; } = string.Empty;
    public string Status { get; set; } = string.Empty;
    public string StorageClass { get; set; } = string.Empty;
    public string Capacity { get; set; } = string.Empty;
    public string AccessModes { get; set; } = string.Empty;
    public string VolumeName { get; set; } = string.Empty;
    public string CreatedAt { get; set; } = string.Empty;
}

public class CreatePvcDto
{
    public string Cluster { get; set; } = string.Empty;
    public string Namespace { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public string StorageClass { get; set; } = string.Empty;
    public string Storage { get; set; } = string.Empty;
    public List<string> AccessModes { get; set; } = new();
    public string RequestedBy { get; set; } = string.Empty;
    public string Reason { get; set; } = string.Empty;
    public string CorrelationId { get; set; } = string.Empty;
}

public class HpaDto
{
    public string Name { get; set; } = string.Empty;
    public string Namespace { get; set; } = string.Empty;
    public string TargetKind { get; set; } = string.Empty;
    public string TargetName { get; set; } = string.Empty;
    public int MinReplicas { get; set; }
    public int MaxReplicas { get; set; }
    public int CurrentReplicas { get; set; }
    public int DesiredReplicas { get; set; }
    public int CpuTargetUtilization { get; set; }
    public string CreatedAt { get; set; } = string.Empty;
}

public class CreateHpaDto
{
    public string Cluster { get; set; } = string.Empty;
    public string Namespace { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public string TargetKind { get; set; } = string.Empty;
    public string TargetName { get; set; } = string.Empty;
    public int MinReplicas { get; set; } = 1;
    public int MaxReplicas { get; set; } = 10;
    public int CpuTargetUtilization { get; set; } = 80;
    public string RequestedBy { get; set; } = string.Empty;
    public string Reason { get; set; } = string.Empty;
    public string CorrelationId { get; set; } = string.Empty;
}
