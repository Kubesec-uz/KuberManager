using Grpc.Core;
using KuberManager.Contracts;

namespace KuberManager.TestClient;

internal static class NetworkPolicyTests
{
    public static async Task Run(Clients c, Metadata h, string cluster)
    {
        Menu.SubMenu("NetworkPolicies", ["List", "Get", "Delete"]);
        switch (Console.ReadLine()?.Trim())
        {
            case "1":
                var ns = Menu.Ask("Namespace", "app-prod");
                var list = await c.NetworkPolicy.ListAsync(new ListNetworkPoliciesRequest { Cluster = cluster, Namespace = ns }, h);
                foreach (var np in list.Items)
                    Menu.Print($"{np.Name}  types=[{string.Join(", ", np.PolicyTypes)}]  selectors=[{string.Join(", ", np.PodSelectorLabels)}]");
                if (list.Items.Count == 0) Menu.Print("(bo'sh)");
                break;

            case "2":
                var ns2   = Menu.Ask("Namespace", "app-prod");
                var name2 = Menu.Ask("NetworkPolicy nomi");
                var np2 = await c.NetworkPolicy.GetAsync(new NetworkPolicyRef { Cluster = cluster, Namespace = ns2, Name = name2 }, h);
                Menu.Ok($"{np2.Name}  types=[{string.Join(", ", np2.PolicyTypes)}]");
                break;

            case "3":
                var ns3   = Menu.Ask("Namespace", "app-prod");
                var name3 = Menu.Ask("NetworkPolicy nomi");
                var r3 = await c.NetworkPolicy.DeleteAsync(new DeleteNetworkPolicyRequest
                {
                    Cluster = cluster, Namespace = ns3, Name = name3,
                    RequestedBy = "test-client", Reason = "test delete", CorrelationId = Guid.NewGuid().ToString()
                }, h);
                Menu.Ok(r3.Message);
                break;
        }
    }
}
