using k8s;
using k8s.Models;
using System.Runtime.CompilerServices;
using System.Threading.Channels;

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

        var channel = Channel.CreateUnbounded<(string output, bool isStderr)>();

        var stdoutTask = ReadStreamToChannelAsync(stdoutStream, false, channel.Writer, ct);
        var stderrTask = ReadStreamToChannelAsync(stderrStream, true, channel.Writer, ct);

        _ = Task.WhenAll(stdoutTask, stderrTask).ContinueWith(_ => channel.Writer.TryComplete(), CancellationToken.None);

        await foreach (var item in channel.Reader.ReadAllAsync(ct))
        {
            yield return item;
        }
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

    // ── Services ─────────────────────────────────────────────────────────────

    public async Task<IReadOnlyList<V1Service>> ListServicesAsync(string ns, CancellationToken ct = default)
    {
        _logger.LogDebug("Listing services in {Namespace}", ns);
        var list = await _client.CoreV1.ListNamespacedServiceAsync(ns, cancellationToken: ct);
        return (IReadOnlyList<V1Service>)list.Items;
    }

    public async Task<V1Service> GetServiceAsync(string ns, string name, CancellationToken ct = default)
    {
        _logger.LogDebug("Getting service {Namespace}/{Name}", ns, name);
        return await _client.CoreV1.ReadNamespacedServiceAsync(name, ns, cancellationToken: ct);
    }

    public async Task<V1Service> CreateServiceAsync(string ns, V1Service service, CancellationToken ct = default)
    {
        _logger.LogDebug("Creating service {Namespace}/{Name}", ns, service.Metadata.Name);
        return await _client.CoreV1.CreateNamespacedServiceAsync(service, ns, cancellationToken: ct);
    }

    public async Task<V1Service> UpdateServiceAsync(string ns, string name, V1Service service, CancellationToken ct = default)
    {
        _logger.LogDebug("Replacing service {Namespace}/{Name}", ns, name);
        return await _client.CoreV1.ReplaceNamespacedServiceAsync(service, name, ns, cancellationToken: ct);
    }

    public async Task DeleteServiceAsync(string ns, string name, CancellationToken ct = default)
    {
        _logger.LogDebug("Deleting service {Namespace}/{Name}", ns, name);
        await _client.CoreV1.DeleteNamespacedServiceAsync(name, ns, cancellationToken: ct);
    }

    // ── Secrets ──────────────────────────────────────────────────────────────

    public async Task<IReadOnlyList<V1Secret>> ListSecretsAsync(string ns, CancellationToken ct = default)
    {
        _logger.LogDebug("Listing secrets in {Namespace}", ns);
        var list = await _client.CoreV1.ListNamespacedSecretAsync(ns, cancellationToken: ct);
        return (IReadOnlyList<V1Secret>)list.Items;
    }

    public async Task<V1Secret> GetSecretAsync(string ns, string name, CancellationToken ct = default)
    {
        _logger.LogDebug("Getting secret {Namespace}/{Name}", ns, name);
        return await _client.CoreV1.ReadNamespacedSecretAsync(name, ns, cancellationToken: ct);
    }

    public async Task<V1Secret> CreateSecretAsync(string ns, V1Secret secret, CancellationToken ct = default)
    {
        _logger.LogDebug("Creating secret {Namespace}/{Name}", ns, secret.Metadata.Name);
        return await _client.CoreV1.CreateNamespacedSecretAsync(secret, ns, cancellationToken: ct);
    }

    public async Task<V1Secret> UpdateSecretAsync(string ns, string name, V1Secret secret, CancellationToken ct = default)
    {
        _logger.LogDebug("Replacing secret {Namespace}/{Name}", ns, name);
        return await _client.CoreV1.ReplaceNamespacedSecretAsync(secret, name, ns, cancellationToken: ct);
    }

    public async Task DeleteSecretAsync(string ns, string name, CancellationToken ct = default)
    {
        _logger.LogDebug("Deleting secret {Namespace}/{Name}", ns, name);
        await _client.CoreV1.DeleteNamespacedSecretAsync(name, ns, cancellationToken: ct);
    }

    // ── Ingresses ────────────────────────────────────────────────────────────

    public async Task<IReadOnlyList<V1Ingress>> ListIngressesAsync(string ns, CancellationToken ct = default)
    {
        _logger.LogDebug("Listing ingresses in {Namespace}", ns);
        var list = await _client.NetworkingV1.ListNamespacedIngressAsync(ns, cancellationToken: ct);
        return (IReadOnlyList<V1Ingress>)list.Items;
    }

    public async Task<V1Ingress> GetIngressAsync(string ns, string name, CancellationToken ct = default)
    {
        _logger.LogDebug("Getting ingress {Namespace}/{Name}", ns, name);
        return await _client.NetworkingV1.ReadNamespacedIngressAsync(name, ns, cancellationToken: ct);
    }

    public async Task<V1Ingress> CreateIngressAsync(string ns, V1Ingress ingress, CancellationToken ct = default)
    {
        _logger.LogDebug("Creating ingress {Namespace}/{Name}", ns, ingress.Metadata.Name);
        return await _client.NetworkingV1.CreateNamespacedIngressAsync(ingress, ns, cancellationToken: ct);
    }

    public async Task<V1Ingress> UpdateIngressAsync(string ns, string name, V1Ingress ingress, CancellationToken ct = default)
    {
        _logger.LogDebug("Replacing ingress {Namespace}/{Name}", ns, name);
        return await _client.NetworkingV1.ReplaceNamespacedIngressAsync(ingress, name, ns, cancellationToken: ct);
    }

    public async Task DeleteIngressAsync(string ns, string name, CancellationToken ct = default)
    {
        _logger.LogDebug("Deleting ingress {Namespace}/{Name}", ns, name);
        await _client.NetworkingV1.DeleteNamespacedIngressAsync(name, ns, cancellationToken: ct);
    }

    // ── Cluster ────────────────────────────────────────────────────────────────

    public async Task<string> GetServerVersionAsync(CancellationToken ct = default)
    {
        var version = await _client.Version.GetCodeAsync(cancellationToken: ct);
        return $"{version.Major}.{version.Minor}";
    }

    // ── StatefulSets ──────────────────────────────────────────────────────────

    public async Task<IReadOnlyList<V1StatefulSet>> ListStatefulSetsAsync(string ns, CancellationToken ct = default)
    {
        _logger.LogDebug("Listing statefulsets in {Namespace}", ns);
        var list = await _client.AppsV1.ListNamespacedStatefulSetAsync(ns, cancellationToken: ct);
        return (IReadOnlyList<V1StatefulSet>)list.Items;
    }

    public async Task<V1StatefulSet> GetStatefulSetAsync(string ns, string name, CancellationToken ct = default)
    {
        _logger.LogDebug("Getting statefulset {Namespace}/{Name}", ns, name);
        return await _client.AppsV1.ReadNamespacedStatefulSetAsync(name, ns, cancellationToken: ct);
    }

    public async Task<V1StatefulSet> CreateStatefulSetAsync(string ns, V1StatefulSet sts, CancellationToken ct = default)
    {
        _logger.LogDebug("Creating statefulset {Namespace}/{Name}", ns, sts.Metadata.Name);
        return await _client.AppsV1.CreateNamespacedStatefulSetAsync(sts, ns, cancellationToken: ct);
    }

    public async Task DeleteStatefulSetAsync(string ns, string name, CancellationToken ct = default)
    {
        _logger.LogDebug("Deleting statefulset {Namespace}/{Name}", ns, name);
        await _client.AppsV1.DeleteNamespacedStatefulSetAsync(name, ns, cancellationToken: ct);
    }

    public async Task PatchStatefulSetReplicasAsync(string ns, string name, int replicas, CancellationToken ct = default)
    {
        _logger.LogDebug("Patching replicas {Namespace}/{Name} -> {Replicas}", ns, name, replicas);
        var patch = new V1Patch(new { spec = new { replicas } }, V1Patch.PatchType.MergePatch);
        await _client.AppsV1.PatchNamespacedStatefulSetAsync(patch, name, ns, cancellationToken: ct);
    }

    public async Task PatchStatefulSetRestartAsync(string ns, string name, DateTimeOffset restartedAt, CancellationToken ct = default)
    {
        _logger.LogDebug("Patching restart annotation statefulset {Namespace}/{Name}", ns, name);
        var patch = new V1Patch(new { spec = new { template = new { metadata = new { annotations = new Dictionary<string, string> { ["kubectl.kubernetes.io/restartedAt"] = restartedAt.ToString("O") } } } } }, V1Patch.PatchType.MergePatch);
        await _client.AppsV1.PatchNamespacedStatefulSetAsync(patch, name, ns, cancellationToken: ct);
    }

    // ── DaemonSets ────────────────────────────────────────────────────────────

    public async Task<IReadOnlyList<V1DaemonSet>> ListDaemonSetsAsync(string ns, CancellationToken ct = default)
    {
        _logger.LogDebug("Listing daemonsets in {Namespace}", ns);
        var list = await _client.AppsV1.ListNamespacedDaemonSetAsync(ns, cancellationToken: ct);
        return (IReadOnlyList<V1DaemonSet>)list.Items;
    }

    public async Task<V1DaemonSet> GetDaemonSetAsync(string ns, string name, CancellationToken ct = default)
    {
        _logger.LogDebug("Getting daemonset {Namespace}/{Name}", ns, name);
        return await _client.AppsV1.ReadNamespacedDaemonSetAsync(name, ns, cancellationToken: ct);
    }

    public async Task<V1DaemonSet> CreateDaemonSetAsync(string ns, V1DaemonSet ds, CancellationToken ct = default)
    {
        _logger.LogDebug("Creating daemonset {Namespace}/{Name}", ns, ds.Metadata.Name);
        return await _client.AppsV1.CreateNamespacedDaemonSetAsync(ds, ns, cancellationToken: ct);
    }

    public async Task DeleteDaemonSetAsync(string ns, string name, CancellationToken ct = default)
    {
        _logger.LogDebug("Deleting daemonset {Namespace}/{Name}", ns, name);
        await _client.AppsV1.DeleteNamespacedDaemonSetAsync(name, ns, cancellationToken: ct);
    }

    public async Task PatchDaemonSetRestartAsync(string ns, string name, DateTimeOffset restartedAt, CancellationToken ct = default)
    {
        _logger.LogDebug("Patching restart annotation daemonset {Namespace}/{Name}", ns, name);
        var patch = new V1Patch(new { spec = new { template = new { metadata = new { annotations = new Dictionary<string, string> { ["kubectl.kubernetes.io/restartedAt"] = restartedAt.ToString("O") } } } } }, V1Patch.PatchType.MergePatch);
        await _client.AppsV1.PatchNamespacedDaemonSetAsync(patch, name, ns, cancellationToken: ct);
    }

    // ── Jobs ──────────────────────────────────────────────────────────────────

    public async Task<IReadOnlyList<V1Job>> ListJobsAsync(string ns, CancellationToken ct = default)
    {
        _logger.LogDebug("Listing jobs in {Namespace}", ns);
        var list = await _client.BatchV1.ListNamespacedJobAsync(ns, cancellationToken: ct);
        return (IReadOnlyList<V1Job>)list.Items;
    }

    public async Task<V1Job> GetJobAsync(string ns, string name, CancellationToken ct = default)
    {
        _logger.LogDebug("Getting job {Namespace}/{Name}", ns, name);
        return await _client.BatchV1.ReadNamespacedJobAsync(name, ns, cancellationToken: ct);
    }

    public async Task<V1Job> CreateJobAsync(string ns, V1Job job, CancellationToken ct = default)
    {
        _logger.LogDebug("Creating job {Namespace}/{Name}", ns, job.Metadata.Name);
        return await _client.BatchV1.CreateNamespacedJobAsync(job, ns, cancellationToken: ct);
    }

    public async Task DeleteJobAsync(string ns, string name, CancellationToken ct = default)
    {
        _logger.LogDebug("Deleting job {Namespace}/{Name}", ns, name);
        await _client.BatchV1.DeleteNamespacedJobAsync(name, ns,
            body: new V1DeleteOptions { PropagationPolicy = "Foreground" },
            cancellationToken: ct);
    }

    // ── CronJobs ──────────────────────────────────────────────────────────────

    public async Task<IReadOnlyList<V1CronJob>> ListCronJobsAsync(string ns, CancellationToken ct = default)
    {
        _logger.LogDebug("Listing cronjobs in {Namespace}", ns);
        var list = await _client.BatchV1.ListNamespacedCronJobAsync(ns, cancellationToken: ct);
        return (IReadOnlyList<V1CronJob>)list.Items;
    }

    public async Task<V1CronJob> GetCronJobAsync(string ns, string name, CancellationToken ct = default)
    {
        _logger.LogDebug("Getting cronjob {Namespace}/{Name}", ns, name);
        return await _client.BatchV1.ReadNamespacedCronJobAsync(name, ns, cancellationToken: ct);
    }

    public async Task<V1CronJob> CreateCronJobAsync(string ns, V1CronJob cronJob, CancellationToken ct = default)
    {
        _logger.LogDebug("Creating cronjob {Namespace}/{Name}", ns, cronJob.Metadata.Name);
        return await _client.BatchV1.CreateNamespacedCronJobAsync(cronJob, ns, cancellationToken: ct);
    }

    public async Task DeleteCronJobAsync(string ns, string name, CancellationToken ct = default)
    {
        _logger.LogDebug("Deleting cronjob {Namespace}/{Name}", ns, name);
        await _client.BatchV1.DeleteNamespacedCronJobAsync(name, ns, cancellationToken: ct);
    }

    public async Task PatchCronJobSuspendAsync(string ns, string name, bool suspend, CancellationToken ct = default)
    {
        _logger.LogDebug("Patching suspend={Suspend} cronjob {Namespace}/{Name}", suspend, ns, name);
        var patch = new V1Patch(new { spec = new { suspend } }, V1Patch.PatchType.MergePatch);
        await _client.BatchV1.PatchNamespacedCronJobAsync(patch, name, ns, cancellationToken: ct);
    }

    // ── Nodes ─────────────────────────────────────────────────────────────────

    public async Task<IReadOnlyList<V1Node>> ListNodesAsync(CancellationToken ct = default)
    {
        _logger.LogDebug("Listing nodes");
        var list = await _client.CoreV1.ListNodeAsync(cancellationToken: ct);
        return (IReadOnlyList<V1Node>)list.Items;
    }

    public async Task<V1Node> GetNodeAsync(string name, CancellationToken ct = default)
    {
        _logger.LogDebug("Getting node {Name}", name);
        return await _client.CoreV1.ReadNodeAsync(name, cancellationToken: ct);
    }

    public async Task PatchNodeUnschedulableAsync(string name, bool unschedulable, CancellationToken ct = default)
    {
        _logger.LogDebug("Patching node {Name} unschedulable={Unschedulable}", name, unschedulable);
        var patch = new V1Patch(new { spec = new { unschedulable } }, V1Patch.PatchType.MergePatch);
        await _client.CoreV1.PatchNodeAsync(patch, name, cancellationToken: ct);
    }

    public async Task DeletePodsOnNodeAsync(string nodeName, bool force, bool ignoreDaemonSets, CancellationToken ct = default)
    {
        _logger.LogDebug("Deleting pods on node {NodeName} force={Force}", nodeName, force);
        var pods = await _client.CoreV1.ListPodForAllNamespacesAsync(
            fieldSelector: $"spec.nodeName={nodeName}", cancellationToken: ct);

        foreach (var pod in pods.Items)
        {
            if (ignoreDaemonSets && pod.OwnerReferences()
                    .Any(r => r.Kind == "DaemonSet")) continue;

            var ns = pod.Metadata.NamespaceProperty;
            var podName = pod.Metadata.Name;

            if (force)
            {
                await _client.CoreV1.DeleteNamespacedPodAsync(podName, ns,
                    body: new V1DeleteOptions { GracePeriodSeconds = 0 },
                    cancellationToken: ct);
            }
            else
            {
                await _client.CoreV1.DeleteNamespacedPodAsync(podName, ns, cancellationToken: ct);
            }
        }
    }

    // ── PersistentVolumeClaims ────────────────────────────────────────────────

    public async Task<IReadOnlyList<V1PersistentVolumeClaim>> ListPvcsAsync(string ns, CancellationToken ct = default)
    {
        _logger.LogDebug("Listing PVCs in {Namespace}", ns);
        var list = await _client.CoreV1.ListNamespacedPersistentVolumeClaimAsync(ns, cancellationToken: ct);
        return (IReadOnlyList<V1PersistentVolumeClaim>)list.Items;
    }

    public async Task<V1PersistentVolumeClaim> GetPvcAsync(string ns, string name, CancellationToken ct = default)
    {
        _logger.LogDebug("Getting PVC {Namespace}/{Name}", ns, name);
        return await _client.CoreV1.ReadNamespacedPersistentVolumeClaimAsync(name, ns, cancellationToken: ct);
    }

    public async Task<V1PersistentVolumeClaim> CreatePvcAsync(string ns, V1PersistentVolumeClaim pvc, CancellationToken ct = default)
    {
        _logger.LogDebug("Creating PVC {Namespace}/{Name}", ns, pvc.Metadata.Name);
        return await _client.CoreV1.CreateNamespacedPersistentVolumeClaimAsync(pvc, ns, cancellationToken: ct);
    }

    public async Task DeletePvcAsync(string ns, string name, CancellationToken ct = default)
    {
        _logger.LogDebug("Deleting PVC {Namespace}/{Name}", ns, name);
        await _client.CoreV1.DeleteNamespacedPersistentVolumeClaimAsync(name, ns, cancellationToken: ct);
    }

    // ── HorizontalPodAutoscalers ──────────────────────────────────────────────

    public async Task<IReadOnlyList<V2HorizontalPodAutoscaler>> ListHpasAsync(string ns, CancellationToken ct = default)
    {
        _logger.LogDebug("Listing HPAs in {Namespace}", ns);
        var list = await _client.AutoscalingV2.ListNamespacedHorizontalPodAutoscalerAsync(ns, cancellationToken: ct);
        return (IReadOnlyList<V2HorizontalPodAutoscaler>)list.Items;
    }

    public async Task<V2HorizontalPodAutoscaler> GetHpaAsync(string ns, string name, CancellationToken ct = default)
    {
        _logger.LogDebug("Getting HPA {Namespace}/{Name}", ns, name);
        return await _client.AutoscalingV2.ReadNamespacedHorizontalPodAutoscalerAsync(name, ns, cancellationToken: ct);
    }

    public async Task<V2HorizontalPodAutoscaler> CreateHpaAsync(string ns, V2HorizontalPodAutoscaler hpa, CancellationToken ct = default)
    {
        _logger.LogDebug("Creating HPA {Namespace}/{Name}", ns, hpa.Metadata.Name);
        return await _client.AutoscalingV2.CreateNamespacedHorizontalPodAutoscalerAsync(hpa, ns, cancellationToken: ct);
    }

    public async Task DeleteHpaAsync(string ns, string name, CancellationToken ct = default)
    {
        _logger.LogDebug("Deleting HPA {Namespace}/{Name}", ns, name);
        await _client.AutoscalingV2.DeleteNamespacedHorizontalPodAutoscalerAsync(name, ns, cancellationToken: ct);
    }

    // ── ServiceAccounts ───────────────────────────────────────────────────────

    public async Task<IReadOnlyList<V1ServiceAccount>> ListServiceAccountsAsync(string ns, CancellationToken ct = default)
    {
        _logger.LogDebug("Listing serviceaccounts in {Namespace}", ns);
        var list = await _client.CoreV1.ListNamespacedServiceAccountAsync(ns, cancellationToken: ct);
        return (IReadOnlyList<V1ServiceAccount>)list.Items;
    }

    public async Task<V1ServiceAccount> GetServiceAccountAsync(string ns, string name, CancellationToken ct = default)
    {
        _logger.LogDebug("Getting serviceaccount {Namespace}/{Name}", ns, name);
        return await _client.CoreV1.ReadNamespacedServiceAccountAsync(name, ns, cancellationToken: ct);
    }

    public async Task<V1ServiceAccount> CreateServiceAccountAsync(string ns, V1ServiceAccount sa, CancellationToken ct = default)
    {
        _logger.LogDebug("Creating serviceaccount {Namespace}/{Name}", ns, sa.Metadata.Name);
        return await _client.CoreV1.CreateNamespacedServiceAccountAsync(sa, ns, cancellationToken: ct);
    }

    public async Task DeleteServiceAccountAsync(string ns, string name, CancellationToken ct = default)
    {
        _logger.LogDebug("Deleting serviceaccount {Namespace}/{Name}", ns, name);
        await _client.CoreV1.DeleteNamespacedServiceAccountAsync(name, ns, cancellationToken: ct);
    }

    // ── RBAC – Roles ──────────────────────────────────────────────────────────

    public async Task<IReadOnlyList<V1Role>> ListRolesAsync(string ns, CancellationToken ct = default)
    {
        _logger.LogDebug("Listing roles in {Namespace}", ns);
        var list = await _client.RbacAuthorizationV1.ListNamespacedRoleAsync(ns, cancellationToken: ct);
        return (IReadOnlyList<V1Role>)list.Items;
    }

    public async Task<V1Role> GetRoleAsync(string ns, string name, CancellationToken ct = default)
    {
        _logger.LogDebug("Getting role {Namespace}/{Name}", ns, name);
        return await _client.RbacAuthorizationV1.ReadNamespacedRoleAsync(name, ns, cancellationToken: ct);
    }

    public async Task<V1Role> CreateRoleAsync(string ns, V1Role role, CancellationToken ct = default)
    {
        _logger.LogDebug("Creating role {Namespace}/{Name}", ns, role.Metadata.Name);
        return await _client.RbacAuthorizationV1.CreateNamespacedRoleAsync(role, ns, cancellationToken: ct);
    }

    public async Task DeleteRoleAsync(string ns, string name, CancellationToken ct = default)
    {
        _logger.LogDebug("Deleting role {Namespace}/{Name}", ns, name);
        await _client.RbacAuthorizationV1.DeleteNamespacedRoleAsync(name, ns, cancellationToken: ct);
    }

    // ── RBAC – ClusterRoles ───────────────────────────────────────────────────

    public async Task<IReadOnlyList<V1ClusterRole>> ListClusterRolesAsync(CancellationToken ct = default)
    {
        _logger.LogDebug("Listing clusterroles");
        var list = await _client.RbacAuthorizationV1.ListClusterRoleAsync(cancellationToken: ct);
        return (IReadOnlyList<V1ClusterRole>)list.Items;
    }

    public async Task<V1ClusterRole> GetClusterRoleAsync(string name, CancellationToken ct = default)
    {
        _logger.LogDebug("Getting clusterrole {Name}", name);
        return await _client.RbacAuthorizationV1.ReadClusterRoleAsync(name, cancellationToken: ct);
    }

    public async Task<V1ClusterRole> CreateClusterRoleAsync(V1ClusterRole clusterRole, CancellationToken ct = default)
    {
        _logger.LogDebug("Creating clusterrole {Name}", clusterRole.Metadata.Name);
        return await _client.RbacAuthorizationV1.CreateClusterRoleAsync(clusterRole, cancellationToken: ct);
    }

    public async Task DeleteClusterRoleAsync(string name, CancellationToken ct = default)
    {
        _logger.LogDebug("Deleting clusterrole {Name}", name);
        await _client.RbacAuthorizationV1.DeleteClusterRoleAsync(name, cancellationToken: ct);
    }

    // ── RBAC – RoleBindings ───────────────────────────────────────────────────

    public async Task<IReadOnlyList<V1RoleBinding>> ListRoleBindingsAsync(string ns, CancellationToken ct = default)
    {
        _logger.LogDebug("Listing rolebindings in {Namespace}", ns);
        var list = await _client.RbacAuthorizationV1.ListNamespacedRoleBindingAsync(ns, cancellationToken: ct);
        return (IReadOnlyList<V1RoleBinding>)list.Items;
    }

    public async Task<V1RoleBinding> GetRoleBindingAsync(string ns, string name, CancellationToken ct = default)
    {
        _logger.LogDebug("Getting rolebinding {Namespace}/{Name}", ns, name);
        return await _client.RbacAuthorizationV1.ReadNamespacedRoleBindingAsync(name, ns, cancellationToken: ct);
    }

    public async Task<V1RoleBinding> CreateRoleBindingAsync(string ns, V1RoleBinding binding, CancellationToken ct = default)
    {
        _logger.LogDebug("Creating rolebinding {Namespace}/{Name}", ns, binding.Metadata.Name);
        return await _client.RbacAuthorizationV1.CreateNamespacedRoleBindingAsync(binding, ns, cancellationToken: ct);
    }

    public async Task DeleteRoleBindingAsync(string ns, string name, CancellationToken ct = default)
    {
        _logger.LogDebug("Deleting rolebinding {Namespace}/{Name}", ns, name);
        await _client.RbacAuthorizationV1.DeleteNamespacedRoleBindingAsync(name, ns, cancellationToken: ct);
    }

    // ── RBAC – ClusterRoleBindings ────────────────────────────────────────────

    public async Task<IReadOnlyList<V1ClusterRoleBinding>> ListClusterRoleBindingsAsync(CancellationToken ct = default)
    {
        _logger.LogDebug("Listing clusterrolebindings");
        var list = await _client.RbacAuthorizationV1.ListClusterRoleBindingAsync(cancellationToken: ct);
        return (IReadOnlyList<V1ClusterRoleBinding>)list.Items;
    }

    public async Task<V1ClusterRoleBinding> GetClusterRoleBindingAsync(string name, CancellationToken ct = default)
    {
        _logger.LogDebug("Getting clusterrolebinding {Name}", name);
        return await _client.RbacAuthorizationV1.ReadClusterRoleBindingAsync(name, cancellationToken: ct);
    }

    public async Task<V1ClusterRoleBinding> CreateClusterRoleBindingAsync(V1ClusterRoleBinding binding, CancellationToken ct = default)
    {
        _logger.LogDebug("Creating clusterrolebinding {Name}", binding.Metadata.Name);
        return await _client.RbacAuthorizationV1.CreateClusterRoleBindingAsync(binding, cancellationToken: ct);
    }

    public async Task DeleteClusterRoleBindingAsync(string name, CancellationToken ct = default)
    {
        _logger.LogDebug("Deleting clusterrolebinding {Name}", name);
        await _client.RbacAuthorizationV1.DeleteClusterRoleBindingAsync(name, cancellationToken: ct);
    }

    // ── NetworkPolicies ───────────────────────────────────────────────────────

    public async Task<IReadOnlyList<V1NetworkPolicy>> ListNetworkPoliciesAsync(string ns, CancellationToken ct = default)
    {
        _logger.LogDebug("Listing networkpolicies in {Namespace}", ns);
        var list = await _client.NetworkingV1.ListNamespacedNetworkPolicyAsync(ns, cancellationToken: ct);
        return (IReadOnlyList<V1NetworkPolicy>)list.Items;
    }

    public async Task<V1NetworkPolicy> GetNetworkPolicyAsync(string ns, string name, CancellationToken ct = default)
    {
        _logger.LogDebug("Getting networkpolicy {Namespace}/{Name}", ns, name);
        return await _client.NetworkingV1.ReadNamespacedNetworkPolicyAsync(name, ns, cancellationToken: ct);
    }

    public async Task<V1NetworkPolicy> CreateNetworkPolicyAsync(string ns, V1NetworkPolicy policy, CancellationToken ct = default)
    {
        _logger.LogDebug("Creating networkpolicy {Namespace}/{Name}", ns, policy.Metadata.Name);
        return await _client.NetworkingV1.CreateNamespacedNetworkPolicyAsync(policy, ns, cancellationToken: ct);
    }

    public async Task DeleteNetworkPolicyAsync(string ns, string name, CancellationToken ct = default)
    {
        _logger.LogDebug("Deleting networkpolicy {Namespace}/{Name}", ns, name);
        await _client.NetworkingV1.DeleteNamespacedNetworkPolicyAsync(name, ns, cancellationToken: ct);
    }

    // ── Private helpers ────────────────────────────────────────────────────────

    private static async Task ReadStreamToChannelAsync(
        Stream stream, bool isStderr, ChannelWriter<(string output, bool isStderr)> writer, CancellationToken ct)
    {
        try
        {
            using var reader = new StreamReader(stream);
            string? line;
            while ((line = await reader.ReadLineAsync(ct)) is not null)
            {
                await writer.WriteAsync((line, isStderr), ct);
            }
        }
        catch (OperationCanceledException) { /* Ignored */ }
        catch (Exception ex)
        {
            writer.TryComplete(ex);
        }
    }
}
