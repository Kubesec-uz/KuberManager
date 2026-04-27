using Grpc.Core;
using KuberManager.Contracts;

namespace KuberManager.TestClient;

internal static class HealthTests
{
    public static async Task Run(Clients c, Metadata h, string cluster)
    {
        Menu.SubMenu("Health", ["Ping", "Cluster Health", "Can Access Namespace"]);
        switch (Console.ReadLine()?.Trim())
        {
            case "1":
                var ping = await c.Health.PingAsync(new PingRequest(), h);
                Menu.Ok($"Status: {ping.Status}  v{ping.Version}");
                break;

            case "2":
                var health = await c.Health.ClusterHealthAsync(new ClusterHealthRequest { Cluster = cluster }, h);
                Menu.Ok($"Reachable={health.Reachable}  ServerVersion={health.ServerVersion}");
                Menu.Print($"Message: {health.Message}");
                break;

            case "3":
                var ns = Menu.Ask("Namespace");
                var access = await c.Health.CanAccessNamespaceAsync(new NamespaceAccessRequest { Cluster = cluster, Namespace = ns }, h);
                Menu.Ok($"CanAccess={access.CanAccess}  Message={access.Message}");
                break;
        }
    }
}
