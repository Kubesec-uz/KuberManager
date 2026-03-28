using k8s;
using Microsoft.Extensions.Options;
using KuberManager.Service.Infrastructure.Options;

namespace KuberManager.Service.Infrastructure.Kubernetes;

public sealed class KubernetesClientFactory : IKubernetesClientFactory
{
    private readonly KubernetesOptions _options;
    private readonly ILogger<KubernetesClientFactory> _logger;
    private readonly Dictionary<string, IKubernetes> _clients = new();
    private readonly Lock _lock = new();

    public KubernetesClientFactory(IOptions<KubernetesOptions> options, ILogger<KubernetesClientFactory> logger)
    {
        _options = options.Value;
        _logger = logger;
    }

    public IKubernetes Create(string clusterName)
    {
        lock (_lock)
        {
            if (_clients.TryGetValue(clusterName, out var existing))
                return existing;

            var config = BuildConfig(clusterName);
            var client = new k8s.Kubernetes(config);
            _clients[clusterName] = client;
            _logger.LogInformation("Created Kubernetes client for cluster {Cluster}", clusterName);
            return client;
        }
    }

    private KubernetesClientConfiguration BuildConfig(string clusterName)
    {
        if (_options.UseInClusterConfig)
        {
            _logger.LogInformation("Using in-cluster config for cluster {Cluster}", clusterName);
            return KubernetesClientConfiguration.InClusterConfig();
        }

        if (_options.Clusters.TryGetValue(clusterName, out var kubeconfigPath) && !string.IsNullOrEmpty(kubeconfigPath))
        {
            _logger.LogInformation("Using kubeconfig at {Path} for cluster {Cluster}", kubeconfigPath, clusterName);
            return KubernetesClientConfiguration.BuildConfigFromConfigFile(kubeconfigPath);
        }

        _logger.LogInformation("Using default kubeconfig for cluster {Cluster}", clusterName);
        return KubernetesClientConfiguration.BuildDefaultConfig();
    }
}
