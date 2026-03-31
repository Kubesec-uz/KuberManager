namespace KuberManager.Service.Infrastructure.Kubernetes;

public interface IKubernetesFacadeFactory
{
    IKubernetesFacade For(string clusterName);
}
