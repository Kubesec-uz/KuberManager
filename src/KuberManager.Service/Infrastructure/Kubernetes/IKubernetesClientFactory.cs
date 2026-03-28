using k8s;

namespace KuberManager.Service.Infrastructure.Kubernetes;

public interface IKubernetesClientFactory
{
    IKubernetes Create(string clusterName);
}
