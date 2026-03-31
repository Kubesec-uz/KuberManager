namespace KuberManager.Service.Infrastructure.Kubernetes;

public sealed class KubernetesFacadeFactory : IKubernetesFacadeFactory
{
    private readonly IKubernetesClientFactory _clientFactory;
    private readonly ILoggerFactory _loggerFactory;

    public KubernetesFacadeFactory(IKubernetesClientFactory clientFactory, ILoggerFactory loggerFactory)
    {
        _clientFactory = clientFactory;
        _loggerFactory = loggerFactory;
    }

    public IKubernetesFacade For(string clusterName)
        => new KubernetesFacade(_clientFactory, _loggerFactory.CreateLogger<KubernetesFacade>(), clusterName);
}
