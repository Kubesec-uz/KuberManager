namespace KuberManager.Service.Infrastructure.Policy;

public interface IOperationPolicy
{
    void EnsureNamespaceAllowed(string ns);
    void EnsureDeploymentAllowed(string ns, string deployment);
    void EnsureReplicaLimit(int replicas);
}
