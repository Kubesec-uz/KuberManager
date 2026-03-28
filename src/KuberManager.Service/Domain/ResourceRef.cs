namespace KuberManager.Service.Domain;

public sealed record ResourceRef(string Cluster, string Namespace, string Name);
