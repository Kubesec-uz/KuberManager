using Grpc.Core;
using KuberManager.Contracts;

namespace KuberManager.TestClient;

internal static class HpaTests
{
    public static async Task Run(Clients c, Metadata h, string cluster)
    {
        Menu.SubMenu("HPA", ["List", "Get", "Create", "Delete"]);
        switch (Console.ReadLine()?.Trim())
        {
            case "1":
                var ns = Menu.Ask("Namespace", "app-prod");
                var list = await c.Hpa.ListAsync(new ListHpasRequest { Cluster = cluster, Namespace = ns }, h);
                foreach (var hpa in list.Items)
                    Menu.Print($"{hpa.Name}  target={hpa.TargetName}  min={hpa.MinReplicas}  max={hpa.MaxReplicas}  current={hpa.CurrentReplicas}");
                if (list.Items.Count == 0) Menu.Print("(bo'sh)");
                break;

            case "2":
                var ns2   = Menu.Ask("Namespace", "app-prod");
                var name2 = Menu.Ask("HPA nomi");
                var hpa2 = await c.Hpa.GetAsync(new HpaRef { Cluster = cluster, Namespace = ns2, Name = name2 }, h);
                Menu.Ok($"{hpa2.Name}  min={hpa2.MinReplicas}  max={hpa2.MaxReplicas}  current={hpa2.CurrentReplicas}  cpuTarget={hpa2.CpuTargetUtilization}%");
                break;

            case "3":
                var ns3      = Menu.Ask("Namespace", "app-prod");
                var name3    = Menu.Ask("HPA nomi");
                var kind3    = Menu.Ask("Target kind", "Deployment");
                var target3  = Menu.Ask("Target nomi");
                var min3     = int.Parse(Menu.Ask("Min replicas", "1"));
                var max3     = int.Parse(Menu.Ask("Max replicas", "5"));
                var cpu3     = int.Parse(Menu.Ask("CPU target %", "70"));
                var r3 = await c.Hpa.CreateAsync(new CreateHpaRequest
                {
                    Cluster = cluster, Namespace = ns3, Name = name3,
                    TargetKind = kind3, TargetName = target3,
                    MinReplicas = min3, MaxReplicas = max3, CpuTargetUtilization = cpu3,
                    RequestedBy = "test-client", Reason = "test", CorrelationId = Guid.NewGuid().ToString()
                }, h);
                Menu.Ok(r3.Message);
                break;

            case "4":
                var ns4   = Menu.Ask("Namespace", "app-prod");
                var name4 = Menu.Ask("HPA nomi");
                var r4 = await c.Hpa.DeleteAsync(new DeleteHpaRequest
                {
                    Cluster = cluster, Namespace = ns4, Name = name4,
                    RequestedBy = "test-client", Reason = "test delete", CorrelationId = Guid.NewGuid().ToString()
                }, h);
                Menu.Ok(r4.Message);
                break;
        }
    }
}
