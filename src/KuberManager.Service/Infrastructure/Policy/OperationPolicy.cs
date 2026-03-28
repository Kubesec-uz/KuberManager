using KuberManager.Service.Infrastructure.Options;
using Microsoft.Extensions.Options;

namespace KuberManager.Service.Infrastructure.Policy;

public sealed class OperationPolicy : IOperationPolicy
{
    private readonly PolicyOptions _options;
    private readonly ILogger<OperationPolicy> _logger;

    public OperationPolicy(IOptions<PolicyOptions> options, ILogger<OperationPolicy> logger)
    {
        _options = options.Value;
        _logger = logger;
    }

    public void EnsureNamespaceAllowed(string ns)
    {
        if (_options.AllowedNamespaces.Count > 0 && !_options.AllowedNamespaces.Contains(ns))
        {
            _logger.LogWarning("Access denied to namespace {Namespace}", ns);
            throw new PolicyViolationException($"Access to namespace '{ns}' is not allowed.");
        }
    }

    public void EnsureDeploymentAllowed(string ns, string deployment)
    {
        var key = $"{ns}/{deployment}";
        if (_options.BlockedDeployments.Contains(key))
        {
            _logger.LogWarning("Access denied to deployment {Namespace}/{Deployment}", ns, deployment);
            throw new PolicyViolationException($"Operations on deployment '{deployment}' in namespace '{ns}' are not allowed.");
        }
    }

    public void EnsureReplicaLimit(int replicas)
    {
        if (replicas < 0)
            throw new PolicyViolationException("Replica count cannot be negative.");

        if (replicas > _options.MaxReplicas)
            throw new PolicyViolationException($"Replica count {replicas} exceeds the maximum allowed limit of {_options.MaxReplicas}.");
    }
}
