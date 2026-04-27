using Grpc.Core;
using KuberManager.Contracts;

namespace KuberManager.TestClient;

internal static class StatefulSetTests
{
    public static async Task Run(Clients c, Metadata h, string cluster)
    {
        Menu.SubMenu("StatefulSets", ["List", "Get", "Scale", "Restart", "Delete"]);
        switch (Console.ReadLine()?.Trim())
        {
            case "1":
                var ns = Menu.Ask("Namespace", "app-prod");
                var list = await c.StatefulSet.ListAsync(new ListStatefulSetsRequest { Cluster = cluster, Namespace = ns }, h);
                foreach (var s in list.Items)
                    Menu.Print($"{s.Name}  {s.ReadyReplicas}/{s.DesiredReplicas}  [{s.Status}]");
                if (list.Items.Count == 0) Menu.Print("(bo'sh)");
                break;

            case "2":
                var ns2   = Menu.Ask("Namespace", "app-prod");
                var name2 = Menu.Ask("StatefulSet nomi");
                var ss = await c.StatefulSet.GetAsync(new StatefulSetRef { Cluster = cluster, Namespace = ns2, Name = name2 }, h);
                Menu.Ok($"{ss.Name}  Ready={ss.ReadyReplicas}/{ss.DesiredReplicas}  Status={ss.Status}");
                break;

            case "3":
                var ns3   = Menu.Ask("Namespace", "app-prod");
                var name3 = Menu.Ask("StatefulSet nomi");
                var reps  = int.Parse(Menu.Ask("Replica soni", "1"));
                var r3 = await c.StatefulSet.ScaleAsync(new ScaleStatefulSetRequest
                {
                    Cluster = cluster, Namespace = ns3, Name = name3, Replicas = reps,
                    RequestedBy = "test-client", Reason = "test scale", CorrelationId = Guid.NewGuid().ToString()
                }, h);
                Menu.Ok(r3.Message);
                break;

            case "4":
                var ns4   = Menu.Ask("Namespace", "app-prod");
                var name4 = Menu.Ask("StatefulSet nomi");
                var r4 = await c.StatefulSet.RestartAsync(new RestartStatefulSetRequest
                {
                    Cluster = cluster, Namespace = ns4, Name = name4,
                    RequestedBy = "test-client", Reason = "test restart", CorrelationId = Guid.NewGuid().ToString()
                }, h);
                Menu.Ok(r4.Message);
                break;

            case "5":
                var ns5   = Menu.Ask("Namespace", "app-prod");
                var name5 = Menu.Ask("StatefulSet nomi");
                var r5 = await c.StatefulSet.DeleteAsync(new DeleteStatefulSetRequest
                {
                    Cluster = cluster, Namespace = ns5, Name = name5,
                    RequestedBy = "test-client", Reason = "test delete", CorrelationId = Guid.NewGuid().ToString()
                }, h);
                Menu.Ok(r5.Message);
                break;
        }
    }
}
