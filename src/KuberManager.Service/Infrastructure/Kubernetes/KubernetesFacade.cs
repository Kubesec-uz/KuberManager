using k8s;
using k8s.Models;
using System.Runtime.CompilerServices;

namespace KuberManager.Service.Infrastructure.Kubernetes;

public sealed class KubernetesFacade : IKubernetesFacade
{
    private readonly IKubernetes _client;
    private readonly ILogger<KubernetesFacade> _logger;

    public KubernetesFacade(IKubernetesClientFactory factory, ILogger<KubernetesFacade> logger, string clusterName = "default")
    {
        _client = factory.Create(clusterName);
        _logger = logger;
    }

    // ── Deployments ────────────────────────────────────────────────────────────

    public async Task<V1Deployment> GetDeploymentAsync(string ns, string name, CancellationToken ct = default)
    {
        _logger.LogDebug("Getting deployment {Namespace}/{Name}", ns, name);
        return await _client.AppsV1.ReadNamespacedDeploymentAsync(name, ns, cancellationToken: ct);
    }

    public async Task<IReadOnlyList<V1Deployment>> ListDeploymentsAsync(string ns, CancellationToken ct = default)
    {
        _logger.LogDebug("Listing deployments in {Namespace}", ns);
        var list = await _client.AppsV1.ListNamespacedDeploymentAsync(ns, cancellationToken: ct);
        return (IReadOnlyList<V1Deployment>)list.Items;
    }

    public async Task<V1Deployment> CreateDeploymentAsync(string ns, V1Deployment deployment, CancellationToken ct = default)
    {
        _logger.LogDebug("Creating deployment {Namespace}/{Name}", ns, deployment.Metadata.Name);
        return await _client.AppsV1.CreateNamespacedDeploymentAsync(deployment, ns, cancellationToken: ct);
    }

    public async Task DeleteDeploymentAsync(string ns, string name, CancellationToken ct = default)
    {
        _logger.LogDebug("Deleting deployment {Namespace}/{Name}", ns, name);
        await _client.AppsV1.DeleteNamespacedDeploymentAsync(name, ns, cancellationToken: ct);
    }

    public async Task PatchDeploymentReplicasAsync(string ns, string name, int replicas, CancellationToken ct = default)
    {
        _logger.LogDebug("Patching replicas {Namespace}/{Name} -> {Replicas}", ns, name, replicas);
        var patch = new V1Patch(
            new { spec = new { replicas } },
            V1Patch.PatchType.MergePatch);
        await _client.AppsV1.PatchNamespacedDeploymentAsync(patch, name, ns, cancellationToken: ct);
    }

    public async Task PatchDeploymentRestartAsync(string ns, string name, DateTimeOffset restartedAt, CancellationToken ct = default)
    {
        _logger.LogDebug("Patching restart annotation {Namespace}/{Name}", ns, name);
        var patch = new V1Patch(
            new
            {
                spec = new
                {
                    template = new
                    {
                        metadata = new
                        {
                            annotations = new Dictionary<string, string>
                            {
                                ["kubectl.kubernetes.io/restartedAt"] = restartedAt.ToString("O")
                            }
                        }
                    }
                }
            },
            V1Patch.PatchType.MergePatch);
        await _client.AppsV1.PatchNamespacedDeploymentAsync(patch, name, ns, cancellationToken: ct);
    }

    // ── Pods ───────────────────────────────────────────────────────────────────

    public async Task<IReadOnlyList<V1Pod>> ListPodsAsync(string ns, string? labelSelector = null, CancellationToken ct = default)
    {
        _logger.LogDebug("Listing pods {Namespace} selector={Selector}", ns, labelSelector);
        var list = await _client.CoreV1.ListNamespacedPodAsync(ns, labelSelector: labelSelector, cancellationToken: ct);
        return (IReadOnlyList<V1Pod>)list.Items;
    }

    public async Task<V1Pod> GetPodAsync(string ns, string podName, CancellationToken ct = default)
    {
        _logger.LogDebug("Getting pod {Namespace}/{PodName}", ns, podName);
        return await _client.CoreV1.ReadNamespacedPodAsync(podName, ns, cancellationToken: ct);
    }

    public async Task<string> GetPodLogsAsync(string ns, string podName, string? container = null, int? tailLines = null, CancellationToken ct = default)
    {
        _logger.LogDebug("Getting logs {Namespace}/{PodName}", ns, podName);
        var stream = await _client.CoreV1.ReadNamespacedPodLogAsync(
            podName, ns, container: container, tailLines: tailLines, cancellationToken: ct);
        using var reader = new StreamReader(stream);
        return await reader.ReadToEndAsync(ct);
    }

    public async Task DeletePodAsync(string ns, string podName, CancellationToken ct = default)
    {
        _logger.LogDebug("Deleting pod {Namespace}/{PodName}", ns, podName);
        await _client.CoreV1.DeleteNamespacedPodAsync(podName, ns, cancellationToken: ct);
    }

    public async IAsyncEnumerable<(string output, bool isStderr)> ExecInPodAsync(
        string ns, string podName, string container, string[] command,
        [EnumeratorCancellation] CancellationToken ct = default)
    {
        _logger.LogDebug("Exec in pod {Namespace}/{PodName} container={Container}", ns, podName, container);

        var webSocket = await _client.WebSocketNamespacedPodExecAsync(
            podName, ns,
            command: command,
            container: container,
            stdout: true,
            stderr: true,
            stdin: false,
            tty: false,
            cancellationToken: ct);

        using var demux = new StreamDemuxer(webSocket);
        demux.Start();

        using var stdoutStream = demux.GetStream(ChannelIndex.StdOut, null);
        using var stderrStream = demux.GetStream(ChannelIndex.StdErr, null);

        var stdoutTask = ReadStreamAsync(stdoutStream, false, ct);
        var stderrTask = ReadStreamAsync(stderrStream, true, ct);

        await foreach (var item in MergeAsync(stdoutTask, stderrTask, ct))
            yield return item;
    }

    // ── Namespaces ─────────────────────────────────────────────────────────────

    public async Task<IReadOnlyList<V1Namespace>> ListNamespacesAsync(CancellationToken ct = default)
    {
        _logger.LogDebug("Listing namespaces");
        var list = await _client.CoreV1.ListNamespaceAsync(cancellationToken: ct);
        return (IReadOnlyList<V1Namespace>)list.Items;
    }

    public async Task<V1Namespace> GetNamespaceAsync(string name, CancellationToken ct = default)
    {
        _logger.LogDebug("Getting namespace {Name}", name);
        return await _client.CoreV1.ReadNamespaceAsync(name, cancellationToken: ct);
    }

    public async Task<V1Namespace> CreateNamespaceAsync(V1Namespace ns, CancellationToken ct = default)
    {
        _logger.LogDebug("Creating namespace {Name}", ns.Metadata.Name);
        return await _client.CoreV1.CreateNamespaceAsync(ns, cancellationToken: ct);
    }

    public async Task DeleteNamespaceAsync(string name, CancellationToken ct = default)
    {
        _logger.LogDebug("Deleting namespace {Name}", name);
        await _client.CoreV1.DeleteNamespaceAsync(name, cancellationToken: ct);
    }

    // ── ConfigMaps ─────────────────────────────────────────────────────────────

    public async Task<IReadOnlyList<V1ConfigMap>> ListConfigMapsAsync(string ns, CancellationToken ct = default)
    {
        _logger.LogDebug("Listing configmaps in {Namespace}", ns);
        var list = await _client.CoreV1.ListNamespacedConfigMapAsync(ns, cancellationToken: ct);
        return (IReadOnlyList<V1ConfigMap>)list.Items;
    }

    public async Task<V1ConfigMap> GetConfigMapAsync(string ns, string name, CancellationToken ct = default)
    {
        _logger.LogDebug("Getting configmap {Namespace}/{Name}", ns, name);
        return await _client.CoreV1.ReadNamespacedConfigMapAsync(name, ns, cancellationToken: ct);
    }

    public async Task<V1ConfigMap> CreateConfigMapAsync(string ns, V1ConfigMap configMap, CancellationToken ct = default)
    {
        _logger.LogDebug("Creating configmap {Namespace}/{Name}", ns, configMap.Metadata.Name);
        return await _client.CoreV1.CreateNamespacedConfigMapAsync(configMap, ns, cancellationToken: ct);
    }

    public async Task<V1ConfigMap> UpdateConfigMapAsync(string ns, string name, V1ConfigMap configMap, CancellationToken ct = default)
    {
        _logger.LogDebug("Replacing configmap {Namespace}/{Name}", ns, name);
        return await _client.CoreV1.ReplaceNamespacedConfigMapAsync(configMap, name, ns, cancellationToken: ct);
    }

    public async Task DeleteConfigMapAsync(string ns, string name, CancellationToken ct = default)
    {
        _logger.LogDebug("Deleting configmap {Namespace}/{Name}", ns, name);
        await _client.CoreV1.DeleteNamespacedConfigMapAsync(name, ns, cancellationToken: ct);
    }

    // ── Cluster ────────────────────────────────────────────────────────────────

    public async Task<string> GetServerVersionAsync(CancellationToken ct = default)
    {
        var version = await _client.Version.GetCodeAsync(cancellationToken: ct);
        return $"{version.Major}.{version.Minor}";
    }

    // ── Private helpers ────────────────────────────────────────────────────────

    private static async Task<List<(string output, bool isStderr)>> ReadStreamAsync(
        Stream stream, bool isStderr, CancellationToken ct)
    {
        var results = new List<(string, bool)>();
        using var reader = new StreamReader(stream);
        string? line;
        while ((line = await reader.ReadLineAsync(ct)) is not null)
            results.Add((line, isStderr));
        return results;
    }

    private static async IAsyncEnumerable<(string output, bool isStderr)> MergeAsync(
        Task<List<(string, bool)>> stdoutTask,
        Task<List<(string, bool)>> stderrTask,
        [EnumeratorCancellation] CancellationToken ct)
    {
        await Task.WhenAll(stdoutTask, stderrTask);
        foreach (var item in stdoutTask.Result.Concat(stderrTask.Result))
            yield return item;
    }
}
