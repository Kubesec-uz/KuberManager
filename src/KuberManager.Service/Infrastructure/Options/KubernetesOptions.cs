namespace KuberManager.Service.Infrastructure.Options;

public sealed class KubernetesOptions
{
    public const string Section = "Kubernetes";

    public bool UseInClusterConfig { get; init; } = false;

    // clusterName -> path to kubeconfig
    public Dictionary<string, string> Clusters { get; init; } = new();
}
