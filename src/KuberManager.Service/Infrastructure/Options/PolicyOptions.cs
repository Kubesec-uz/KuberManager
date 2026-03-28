namespace KuberManager.Service.Infrastructure.Options;

public sealed class PolicyOptions
{
    public const string Section = "Policy";

    // If empty, all namespaces are allowed
    public HashSet<string> AllowedNamespaces { get; init; } = [];

    // Format: "namespace/deployment-name"
    public HashSet<string> BlockedDeployments { get; init; } = [];

    public int MaxReplicas { get; init; } = 20;
}
