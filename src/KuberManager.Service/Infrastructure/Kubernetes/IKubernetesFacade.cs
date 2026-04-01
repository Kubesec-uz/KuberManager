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

    // Services
    Task<IReadOnlyList<V1Service>> ListServicesAsync(string ns, CancellationToken ct = default);
    Task<V1Service> GetServiceAsync(string ns, string name, CancellationToken ct = default);
    Task<V1Service> CreateServiceAsync(string ns, V1Service service, CancellationToken ct = default);
    Task<V1Service> UpdateServiceAsync(string ns, string name, V1Service service, CancellationToken ct = default);
    Task DeleteServiceAsync(string ns, string name, CancellationToken ct = default);

    // Secrets
    Task<IReadOnlyList<V1Secret>> ListSecretsAsync(string ns, CancellationToken ct = default);
    Task<V1Secret> GetSecretAsync(string ns, string name, CancellationToken ct = default);
    Task<V1Secret> CreateSecretAsync(string ns, V1Secret secret, CancellationToken ct = default);
    Task<V1Secret> UpdateSecretAsync(string ns, string name, V1Secret secret, CancellationToken ct = default);
    Task DeleteSecretAsync(string ns, string name, CancellationToken ct = default);

    // Ingresses
    Task<IReadOnlyList<V1Ingress>> ListIngressesAsync(string ns, CancellationToken ct = default);
    Task<V1Ingress> GetIngressAsync(string ns, string name, CancellationToken ct = default);
    Task<V1Ingress> CreateIngressAsync(string ns, V1Ingress ingress, CancellationToken ct = default);
    Task<V1Ingress> UpdateIngressAsync(string ns, string name, V1Ingress ingress, CancellationToken ct = default);
    Task DeleteIngressAsync(string ns, string name, CancellationToken ct = default);

    // Cluster
    Task<string> GetServerVersionAsync(CancellationToken ct = default);

    // StatefulSets
    Task<IReadOnlyList<V1StatefulSet>> ListStatefulSetsAsync(string ns, CancellationToken ct = default);
    Task<V1StatefulSet> GetStatefulSetAsync(string ns, string name, CancellationToken ct = default);
    Task<V1StatefulSet> CreateStatefulSetAsync(string ns, V1StatefulSet sts, CancellationToken ct = default);
    Task DeleteStatefulSetAsync(string ns, string name, CancellationToken ct = default);
    Task PatchStatefulSetReplicasAsync(string ns, string name, int replicas, CancellationToken ct = default);
    Task PatchStatefulSetRestartAsync(string ns, string name, DateTimeOffset restartedAt, CancellationToken ct = default);

    // DaemonSets
    Task<IReadOnlyList<V1DaemonSet>> ListDaemonSetsAsync(string ns, CancellationToken ct = default);
    Task<V1DaemonSet> GetDaemonSetAsync(string ns, string name, CancellationToken ct = default);
    Task<V1DaemonSet> CreateDaemonSetAsync(string ns, V1DaemonSet ds, CancellationToken ct = default);
    Task DeleteDaemonSetAsync(string ns, string name, CancellationToken ct = default);
    Task PatchDaemonSetRestartAsync(string ns, string name, DateTimeOffset restartedAt, CancellationToken ct = default);

    // Jobs
    Task<IReadOnlyList<V1Job>> ListJobsAsync(string ns, CancellationToken ct = default);
    Task<V1Job> GetJobAsync(string ns, string name, CancellationToken ct = default);
    Task<V1Job> CreateJobAsync(string ns, V1Job job, CancellationToken ct = default);
    Task DeleteJobAsync(string ns, string name, CancellationToken ct = default);

    // CronJobs
    Task<IReadOnlyList<V1CronJob>> ListCronJobsAsync(string ns, CancellationToken ct = default);
    Task<V1CronJob> GetCronJobAsync(string ns, string name, CancellationToken ct = default);
    Task<V1CronJob> CreateCronJobAsync(string ns, V1CronJob cronJob, CancellationToken ct = default);
    Task DeleteCronJobAsync(string ns, string name, CancellationToken ct = default);
    Task PatchCronJobSuspendAsync(string ns, string name, bool suspend, CancellationToken ct = default);

    // Nodes
    Task<IReadOnlyList<V1Node>> ListNodesAsync(CancellationToken ct = default);
    Task<V1Node> GetNodeAsync(string name, CancellationToken ct = default);
    Task PatchNodeUnschedulableAsync(string name, bool unschedulable, CancellationToken ct = default);
    Task DeletePodsOnNodeAsync(string nodeName, bool force, bool ignoreDaemonSets, CancellationToken ct = default);

    // PersistentVolumeClaims
    Task<IReadOnlyList<V1PersistentVolumeClaim>> ListPvcsAsync(string ns, CancellationToken ct = default);
    Task<V1PersistentVolumeClaim> GetPvcAsync(string ns, string name, CancellationToken ct = default);
    Task<V1PersistentVolumeClaim> CreatePvcAsync(string ns, V1PersistentVolumeClaim pvc, CancellationToken ct = default);
    Task DeletePvcAsync(string ns, string name, CancellationToken ct = default);

    // HorizontalPodAutoscalers
    Task<IReadOnlyList<V2HorizontalPodAutoscaler>> ListHpasAsync(string ns, CancellationToken ct = default);
    Task<V2HorizontalPodAutoscaler> GetHpaAsync(string ns, string name, CancellationToken ct = default);
    Task<V2HorizontalPodAutoscaler> CreateHpaAsync(string ns, V2HorizontalPodAutoscaler hpa, CancellationToken ct = default);
    Task DeleteHpaAsync(string ns, string name, CancellationToken ct = default);

    // ServiceAccounts
    Task<IReadOnlyList<V1ServiceAccount>> ListServiceAccountsAsync(string ns, CancellationToken ct = default);
    Task<V1ServiceAccount> GetServiceAccountAsync(string ns, string name, CancellationToken ct = default);
    Task<V1ServiceAccount> CreateServiceAccountAsync(string ns, V1ServiceAccount sa, CancellationToken ct = default);
    Task DeleteServiceAccountAsync(string ns, string name, CancellationToken ct = default);

    // RBAC – Roles
    Task<IReadOnlyList<V1Role>> ListRolesAsync(string ns, CancellationToken ct = default);
    Task<V1Role> GetRoleAsync(string ns, string name, CancellationToken ct = default);
    Task<V1Role> CreateRoleAsync(string ns, V1Role role, CancellationToken ct = default);
    Task DeleteRoleAsync(string ns, string name, CancellationToken ct = default);

    // RBAC – ClusterRoles
    Task<IReadOnlyList<V1ClusterRole>> ListClusterRolesAsync(CancellationToken ct = default);
    Task<V1ClusterRole> GetClusterRoleAsync(string name, CancellationToken ct = default);
    Task<V1ClusterRole> CreateClusterRoleAsync(V1ClusterRole clusterRole, CancellationToken ct = default);
    Task DeleteClusterRoleAsync(string name, CancellationToken ct = default);

    // RBAC – RoleBindings
    Task<IReadOnlyList<V1RoleBinding>> ListRoleBindingsAsync(string ns, CancellationToken ct = default);
    Task<V1RoleBinding> GetRoleBindingAsync(string ns, string name, CancellationToken ct = default);
    Task<V1RoleBinding> CreateRoleBindingAsync(string ns, V1RoleBinding binding, CancellationToken ct = default);
    Task DeleteRoleBindingAsync(string ns, string name, CancellationToken ct = default);

    // RBAC – ClusterRoleBindings
    Task<IReadOnlyList<V1ClusterRoleBinding>> ListClusterRoleBindingsAsync(CancellationToken ct = default);
    Task<V1ClusterRoleBinding> GetClusterRoleBindingAsync(string name, CancellationToken ct = default);
    Task<V1ClusterRoleBinding> CreateClusterRoleBindingAsync(V1ClusterRoleBinding binding, CancellationToken ct = default);
    Task DeleteClusterRoleBindingAsync(string name, CancellationToken ct = default);

    // NetworkPolicies
    Task<IReadOnlyList<V1NetworkPolicy>> ListNetworkPoliciesAsync(string ns, CancellationToken ct = default);
    Task<V1NetworkPolicy> GetNetworkPolicyAsync(string ns, string name, CancellationToken ct = default);
    Task<V1NetworkPolicy> CreateNetworkPolicyAsync(string ns, V1NetworkPolicy policy, CancellationToken ct = default);
    Task DeleteNetworkPolicyAsync(string ns, string name, CancellationToken ct = default);
}
