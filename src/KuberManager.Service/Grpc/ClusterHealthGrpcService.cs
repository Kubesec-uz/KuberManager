using Grpc.Core;
using KuberManager.Contracts;
using KuberManager.Service.Infrastructure.Kubernetes;
using KuberManager.Service.Infrastructure.Options;
using Microsoft.Extensions.Options;

namespace KuberManager.Service.Grpc;

public sealed class ClusterHealthGrpcService : ClusterHealthService.ClusterHealthServiceBase
{
    private readonly IKubernetesFacade _k8s;
    private readonly PolicyOptions _policy;
    private readonly ILogger<ClusterHealthGrpcService> _logger;

    public ClusterHealthGrpcService(
        IKubernetesFacade k8s,
        IOptions<PolicyOptions> policy,
        ILogger<ClusterHealthGrpcService> logger)
    {
        _k8s = k8s;
        _policy = policy.Value;
        _logger = logger;
    }

    public override Task<PingReply> Ping(PingRequest request, ServerCallContext context)
    {
        return Task.FromResult(new PingReply { Status = "ok", Version = "1.0" });
    }

    public override async Task<ClusterHealthReply> ClusterHealth(ClusterHealthRequest request, ServerCallContext context)
    {
        try
        {
            var version = await _k8s.GetServerVersionAsync(context.CancellationToken);
            return new ClusterHealthReply
            {
                Cluster = request.Cluster,
                Reachable = true,
                ServerVersion = version,
                Message = "Cluster is reachable."
            };
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Cluster health check failed for {Cluster}", request.Cluster);
            return new ClusterHealthReply
            {
                Cluster = request.Cluster,
                Reachable = false,
                Message = ex.Message
            };
        }
    }

    public override Task<NamespaceAccessReply> CanAccessNamespace(NamespaceAccessRequest request, ServerCallContext context)
    {
        var allowed = _policy.AllowedNamespaces.Count == 0 || _policy.AllowedNamespaces.Contains(request.Namespace);
        return Task.FromResult(new NamespaceAccessReply
        {
            CanAccess = allowed,
            Message = allowed ? "Access granted." : $"Namespace '{request.Namespace}' is not in the allowed list."
        });
    }
}
