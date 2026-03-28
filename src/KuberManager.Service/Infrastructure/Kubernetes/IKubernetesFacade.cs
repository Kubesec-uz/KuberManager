using k8s.Models;

namespace KuberManager.Service.Infrastructure.Kubernetes;

public interface IKubernetesFacade
{
    // Deployments
    Task<V1Deployment> GetDeploymentAsync(string ns, string name, CancellationToken ct = default);
    Task<IReadOnlyList<V1Deployment>> ListDeploymentsAsync(string ns, CancellationToken ct = default);
    Task<V1Deployment> CreateDeploymentAsync(string ns, V1Deployment deployment, CancellationToken ct = default);
    Task DeleteDeploymentAsync(string ns, string name, CancellationToken ct = default);
    Task PatchDeploymentReplicasAsync(string ns, string name, int replicas, CancellationToken ct = default);
    Task PatchDeploymentRestartAsync(string ns, string name, DateTimeOffset restartedAt, CancellationToken ct = default);

    // Pods
    Task<IReadOnlyList<V1Pod>> ListPodsAsync(string ns, string? labelSelector = null, CancellationToken ct = default);
    Task<V1Pod> GetPodAsync(string ns, string podName, CancellationToken ct = default);
    Task<string> GetPodLogsAsync(string ns, string podName, string? container = null, int? tailLines = null, CancellationToken ct = default);
    Task DeletePodAsync(string ns, string podName, CancellationToken ct = default);
    IAsyncEnumerable<(string output, bool isStderr)> ExecInPodAsync(string ns, string podName, string container, string[] command, CancellationToken ct = default);

    // Namespaces
    Task<IReadOnlyList<V1Namespace>> ListNamespacesAsync(CancellationToken ct = default);
    Task<V1Namespace> GetNamespaceAsync(string name, CancellationToken ct = default);
    Task<V1Namespace> CreateNamespaceAsync(V1Namespace ns, CancellationToken ct = default);
    Task DeleteNamespaceAsync(string name, CancellationToken ct = default);

    // ConfigMaps
    Task<IReadOnlyList<V1ConfigMap>> ListConfigMapsAsync(string ns, CancellationToken ct = default);
    Task<V1ConfigMap> GetConfigMapAsync(string ns, string name, CancellationToken ct = default);
    Task<V1ConfigMap> CreateConfigMapAsync(string ns, V1ConfigMap configMap, CancellationToken ct = default);
    Task<V1ConfigMap> UpdateConfigMapAsync(string ns, string name, V1ConfigMap configMap, CancellationToken ct = default);
    Task DeleteConfigMapAsync(string ns, string name, CancellationToken ct = default);

    // Cluster
    Task<string> GetServerVersionAsync(CancellationToken ct = default);
}
