using Grpc.Net.Client;
using KuberManager.Contracts;

namespace KuberManager.TestClient;

internal sealed class Clients
{
    public DeploymentService.DeploymentServiceClient      Deployment      { get; }
    public PodService.PodServiceClient                    Pod             { get; }
    public NamespaceService.NamespaceServiceClient        Namespace       { get; }
    public ConfigMapService.ConfigMapServiceClient        ConfigMap       { get; }
    public SecretService.SecretServiceClient              Secret          { get; }
    public ServiceResourceService.ServiceResourceServiceClient Service    { get; }
    public IngressService.IngressServiceClient            Ingress         { get; }
    public StatefulSetService.StatefulSetServiceClient    StatefulSet     { get; }
    public DaemonSetService.DaemonSetServiceClient        DaemonSet       { get; }
    public JobService.JobServiceClient                    Job             { get; }
    public CronJobService.CronJobServiceClient            CronJob         { get; }
    public NodeService.NodeServiceClient                  Node            { get; }
    public PersistentVolumeClaimService.PersistentVolumeClaimServiceClient Pvc { get; }
    public HpaService.HpaServiceClient                    Hpa             { get; }
    public RbacService.RbacServiceClient                  Rbac            { get; }
    public ServiceAccountService.ServiceAccountServiceClient ServiceAccount { get; }
    public NetworkPolicyService.NetworkPolicyServiceClient NetworkPolicy  { get; }
    public ClusterHealthService.ClusterHealthServiceClient Health         { get; }

    public Clients(GrpcChannel channel)
    {
        Deployment     = new(channel);
        Pod            = new(channel);
        Namespace      = new(channel);
        ConfigMap      = new(channel);
        Secret         = new(channel);
        Service        = new(channel);
        Ingress        = new(channel);
        StatefulSet    = new(channel);
        DaemonSet      = new(channel);
        Job            = new(channel);
        CronJob        = new(channel);
        Node           = new(channel);
        Pvc            = new(channel);
        Hpa            = new(channel);
        Rbac           = new(channel);
        ServiceAccount = new(channel);
        NetworkPolicy  = new(channel);
        Health         = new(channel);
    }
}
