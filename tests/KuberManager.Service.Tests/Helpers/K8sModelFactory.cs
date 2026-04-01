using k8s.Models;

namespace KuberManager.Service.Tests.Helpers;

/// <summary>
/// Kubernetes model factory — testlarda takror ishlatish uchun.
/// </summary>
public static class K8sModelFactory
{
    public static V1Deployment Deployment(string name = "test-deploy", string ns = "default",
        int desired = 2, int ready = 2) => new()
    {
        Metadata = new V1ObjectMeta { Name = name, NamespaceProperty = ns, CreationTimestamp = DateTime.UtcNow },
        Spec = new V1DeploymentSpec
        {
            Replicas = desired,
            Selector = new V1LabelSelector { MatchLabels = new Dictionary<string, string> { ["app"] = name } },
            Template = new V1PodTemplateSpec
            {
                Metadata = new V1ObjectMeta { Labels = new Dictionary<string, string> { ["app"] = name } },
                Spec = new V1PodSpec { Containers = new List<V1Container> { new() { Name = name, Image = "nginx:latest" } } }
            }
        },
        Status = new V1DeploymentStatus { Replicas = desired, ReadyReplicas = ready, AvailableReplicas = ready }
    };

    public static V1StatefulSet StatefulSet(string name = "test-sts", string ns = "default",
        int desired = 1, int ready = 1) => new()
    {
        Metadata = new V1ObjectMeta { Name = name, NamespaceProperty = ns, CreationTimestamp = DateTime.UtcNow },
        Spec = new V1StatefulSetSpec
        {
            Replicas = desired, ServiceName = $"{name}-svc",
            Selector = new V1LabelSelector { MatchLabels = new Dictionary<string, string> { ["app"] = name } },
            Template = new V1PodTemplateSpec
            {
                Metadata = new V1ObjectMeta { Labels = new Dictionary<string, string> { ["app"] = name } },
                Spec = new V1PodSpec { Containers = new List<V1Container> { new() { Name = name, Image = "redis:7" } } }
            }
        },
        Status = new V1StatefulSetStatus { Replicas = desired, ReadyReplicas = ready, CurrentReplicas = desired }
    };

    public static V1DaemonSet DaemonSet(string name = "test-ds", string ns = "default",
        int desired = 3, int ready = 3) => new()
    {
        Metadata = new V1ObjectMeta { Name = name, NamespaceProperty = ns, CreationTimestamp = DateTime.UtcNow },
        Spec = new V1DaemonSetSpec
        {
            Selector = new V1LabelSelector { MatchLabels = new Dictionary<string, string> { ["app"] = name } },
            Template = new V1PodTemplateSpec
            {
                Metadata = new V1ObjectMeta { Labels = new Dictionary<string, string> { ["app"] = name } },
                Spec = new V1PodSpec { Containers = new List<V1Container> { new() { Name = name, Image = "fluent/fluent-bit" } } }
            }
        },
        Status = new V1DaemonSetStatus { DesiredNumberScheduled = desired, CurrentNumberScheduled = desired, NumberReady = ready }
    };

    public static V1Job Job(string name = "test-job", string ns = "default",
        int succeeded = 1) => new()
    {
        Metadata = new V1ObjectMeta { Name = name, NamespaceProperty = ns, CreationTimestamp = DateTime.UtcNow },
        Spec = new V1JobSpec
        {
            Completions = 1, Parallelism = 1,
            Template = new V1PodTemplateSpec
            {
                Spec = new V1PodSpec { Containers = new List<V1Container> { new() { Name = name, Image = "busybox" } }, RestartPolicy = "Never" }
            }
        },
        Status = new V1JobStatus { Succeeded = succeeded, Failed = 0, Active = 0 }
    };

    public static V1CronJob CronJob(string name = "test-cron", string ns = "default",
        string schedule = "*/5 * * * *") => new()
    {
        Metadata = new V1ObjectMeta { Name = name, NamespaceProperty = ns, CreationTimestamp = DateTime.UtcNow },
        Spec = new V1CronJobSpec
        {
            Schedule = schedule,
            JobTemplate = new V1JobTemplateSpec
            {
                Spec = new V1JobSpec
                {
                    Template = new V1PodTemplateSpec
                    {
                        Spec = new V1PodSpec { Containers = new List<V1Container> { new() { Name = name, Image = "busybox" } }, RestartPolicy = "OnFailure" }
                    }
                }
            }
        },
        Status = new V1CronJobStatus { LastScheduleTime = DateTime.UtcNow.AddMinutes(-5) }
    };

    public static V1Pod Pod(string name = "test-pod", string ns = "default",
        string phase = "Running") => new()
    {
        Metadata = new V1ObjectMeta { Name = name, NamespaceProperty = ns, CreationTimestamp = DateTime.UtcNow },
        Spec = new V1PodSpec { Containers = new List<V1Container> { new() { Name = name, Image = "nginx" } } },
        Status = new V1PodStatus { Phase = phase }
    };

    public static V1Namespace Namespace(string name = "test-ns") => new()
    {
        Metadata = new V1ObjectMeta { Name = name, CreationTimestamp = DateTime.UtcNow },
        Status = new V1NamespaceStatus { Phase = "Active" }
    };

    public static V1ConfigMap ConfigMap(string name = "test-cm", string ns = "default") => new()
    {
        Metadata = new V1ObjectMeta { Name = name, NamespaceProperty = ns, CreationTimestamp = DateTime.UtcNow },
        Data = new Dictionary<string, string> { ["key"] = "value" }
    };

    public static V1Secret Secret(string name = "test-secret", string ns = "default") => new()
    {
        Metadata = new V1ObjectMeta { Name = name, NamespaceProperty = ns, CreationTimestamp = DateTime.UtcNow },
        Type = "Opaque",
        Data = new Dictionary<string, byte[]> { ["password"] = System.Text.Encoding.UTF8.GetBytes("secret123") }
    };

    public static V1ServiceAccount ServiceAccount(string name = "test-sa", string ns = "default") => new()
    {
        Metadata = new V1ObjectMeta { Name = name, NamespaceProperty = ns, CreationTimestamp = DateTime.UtcNow }
    };

    public static V1Role Role(string name = "test-role", string ns = "default") => new()
    {
        Metadata = new V1ObjectMeta { Name = name, NamespaceProperty = ns, CreationTimestamp = DateTime.UtcNow },
        Rules = new List<V1PolicyRule>
        {
            new() { ApiGroups = new[] { "" }, Resources = new[] { "pods" }, Verbs = new[] { "get", "list" } }
        }
    };

    public static V1ClusterRole ClusterRole(string name = "test-clusterrole") => new()
    {
        Metadata = new V1ObjectMeta { Name = name, CreationTimestamp = DateTime.UtcNow },
        Rules = new List<V1PolicyRule>
        {
            new() { ApiGroups = new[] { "apps" }, Resources = new[] { "deployments" }, Verbs = new[] { "get", "list", "watch" } }
        }
    };

    public static V1RoleBinding RoleBinding(string name = "test-rb", string ns = "default") => new()
    {
        Metadata = new V1ObjectMeta { Name = name, NamespaceProperty = ns, CreationTimestamp = DateTime.UtcNow },
        RoleRef = new V1RoleRef { ApiGroup = "rbac.authorization.k8s.io", Kind = "Role", Name = "test-role" },
        Subjects = new List<Rbacv1Subject> { new() { Kind = "ServiceAccount", Name = "default", NamespaceProperty = ns } }
    };

    public static V1ClusterRoleBinding ClusterRoleBinding(string name = "test-crb") => new()
    {
        Metadata = new V1ObjectMeta { Name = name, CreationTimestamp = DateTime.UtcNow },
        RoleRef = new V1RoleRef { ApiGroup = "rbac.authorization.k8s.io", Kind = "ClusterRole", Name = "test-clusterrole" },
        Subjects = new List<Rbacv1Subject> { new() { Kind = "User", Name = "admin" } }
    };

    public static V1NetworkPolicy NetworkPolicy(string name = "test-netpol", string ns = "default") => new()
    {
        Metadata = new V1ObjectMeta { Name = name, NamespaceProperty = ns, CreationTimestamp = DateTime.UtcNow },
        Spec = new V1NetworkPolicySpec
        {
            PodSelector = new V1LabelSelector { MatchLabels = new Dictionary<string, string> { ["app"] = "test" } },
            PolicyTypes = new List<string> { "Ingress" }
        }
    };

    public static V1Node Node(string name = "test-node", bool ready = true) => new()
    {
        Metadata = new V1ObjectMeta
        {
            Name = name, CreationTimestamp = DateTime.UtcNow,
            Labels = new Dictionary<string, string> { ["node-role.kubernetes.io/worker"] = "" }
        },
        Spec = new V1NodeSpec { Unschedulable = false },
        Status = new V1NodeStatus
        {
            Conditions = new List<V1NodeCondition>
            {
                new() { Type = "Ready", Status = ready ? "True" : "False" }
            },
            NodeInfo = new V1NodeSystemInfo
            {
                OsImage = "Ubuntu 22.04", KernelVersion = "5.15.0", ContainerRuntimeVersion = "containerd://1.6.0"
            },
            Capacity = new Dictionary<string, ResourceQuantity>
            {
                ["cpu"] = new ResourceQuantity("4"),
                ["memory"] = new ResourceQuantity("8Gi")
            }
        }
    };

    public static V1PersistentVolumeClaim Pvc(string name = "test-pvc", string ns = "default",
        string phase = "Bound") => new()
    {
        Metadata = new V1ObjectMeta { Name = name, NamespaceProperty = ns, CreationTimestamp = DateTime.UtcNow },
        Spec = new V1PersistentVolumeClaimSpec
        {
            StorageClassName = "standard",
            AccessModes = new List<string> { "ReadWriteOnce" },
            Resources = new V1VolumeResourceRequirements
            {
                Requests = new Dictionary<string, ResourceQuantity> { ["storage"] = new("1Gi") }
            }
        },
        Status = new V1PersistentVolumeClaimStatus
        {
            Phase = phase,
            Capacity = new Dictionary<string, ResourceQuantity> { ["storage"] = new("1Gi") }
        }
    };

    public static V2HorizontalPodAutoscaler Hpa(string name = "test-hpa", string ns = "default") => new()
    {
        Metadata = new V1ObjectMeta { Name = name, NamespaceProperty = ns, CreationTimestamp = DateTime.UtcNow },
        Spec = new V2HorizontalPodAutoscalerSpec
        {
            ScaleTargetRef = new V2CrossVersionObjectReference { ApiVersion = "apps/v1", Kind = "Deployment", Name = "test-deploy" },
            MinReplicas = 1,
            MaxReplicas = 5,
            Metrics = new List<V2MetricSpec>
            {
                new() { Type = "Resource", Resource = new V2ResourceMetricSource { Name = "cpu", Target = new V2MetricTarget { Type = "Utilization", AverageUtilization = 70 } } }
            }
        },
        Status = new V2HorizontalPodAutoscalerStatus { CurrentReplicas = 2, DesiredReplicas = 2 }
    };
}
