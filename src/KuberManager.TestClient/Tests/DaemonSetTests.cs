using Grpc.Core;
using KuberManager.Contracts;

namespace KuberManager.TestClient;

internal static class DaemonSetTests
{
    public static async Task Run(Clients c, Metadata h, string cluster)
    {
        Menu.SubMenu("DaemonSets", ["List", "Get", "Restart", "Delete"]);
        switch (Console.ReadLine()?.Trim())
        {
            case "1":
                var ns = Menu.Ask("Namespace", "app-prod");
                var list = await c.DaemonSet.ListAsync(new ListDaemonSetsRequest { Cluster = cluster, Namespace = ns }, h);
                foreach (var d in list.Items)
                    Menu.Print($"{d.Name}  desired={d.DesiredNumberScheduled}  ready={d.NumberReady}");
                if (list.Items.Count == 0) Menu.Print("(bo'sh)");
                break;

            case "2":
                var ns2   = Menu.Ask("Namespace", "app-prod");
                var name2 = Menu.Ask("DaemonSet nomi");
                var ds = await c.DaemonSet.GetAsync(new DaemonSetRef { Cluster = cluster, Namespace = ns2, Name = name2 }, h);
                Menu.Ok($"{ds.Name}  desired={ds.DesiredNumberScheduled}  ready={ds.NumberReady}");
                break;

            case "3":
                var ns3   = Menu.Ask("Namespace", "app-prod");
                var name3 = Menu.Ask("DaemonSet nomi");
                var r3 = await c.DaemonSet.RestartAsync(new RestartDaemonSetRequest
                {
                    Cluster = cluster, Namespace = ns3, Name = name3,
                    RequestedBy = "test-client", Reason = "test restart", CorrelationId = Guid.NewGuid().ToString()
                }, h);
                Menu.Ok(r3.Message);
                break;

            case "4":
                var ns4   = Menu.Ask("Namespace", "app-prod");
                var name4 = Menu.Ask("DaemonSet nomi");
                var r4 = await c.DaemonSet.DeleteAsync(new DeleteDaemonSetRequest
                {
                    Cluster = cluster, Namespace = ns4, Name = name4,
                    RequestedBy = "test-client", Reason = "test delete", CorrelationId = Guid.NewGuid().ToString()
                }, h);
                Menu.Ok(r4.Message);
                break;
        }
    }
}
