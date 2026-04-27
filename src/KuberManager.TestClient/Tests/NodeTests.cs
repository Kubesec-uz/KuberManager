using Grpc.Core;
using KuberManager.Contracts;

namespace KuberManager.TestClient;

internal static class NodeTests
{
    public static async Task Run(Clients c, Metadata h, string cluster)
    {
        Menu.SubMenu("Nodes", ["List", "Get", "Cordon", "Uncordon", "Drain"]);
        switch (Console.ReadLine()?.Trim())
        {
            case "1":
                var list = await c.Node.ListAsync(new ListNodesRequest { Cluster = cluster }, h);
                foreach (var n in list.Items)
                    Menu.Print($"{n.Name}  status={n.Status}  roles={n.Roles}  cpu={n.CpuCapacity}  mem={n.MemoryCapacity}  unschedulable={n.Unschedulable}");
                break;

            case "2":
                var name2 = Menu.Ask("Node nomi");
                var node = await c.Node.GetAsync(new NodeRef { Cluster = cluster, Name = name2 }, h);
                Menu.Ok($"{node.Name}  status={node.Status}  unschedulable={node.Unschedulable}  os={node.OsImage}");
                break;

            case "3":
                var name3 = Menu.Ask("Node nomi");
                var r3 = await c.Node.CordonAsync(new CordonNodeRequest
                {
                    Cluster = cluster, Name = name3,
                    RequestedBy = "test-client", Reason = "test cordon", CorrelationId = Guid.NewGuid().ToString()
                }, h);
                Menu.Ok(r3.Message);
                break;

            case "4":
                var name4 = Menu.Ask("Node nomi");
                var r4 = await c.Node.UncordonAsync(new UncordonNodeRequest
                {
                    Cluster = cluster, Name = name4,
                    RequestedBy = "test-client", Reason = "test uncordon", CorrelationId = Guid.NewGuid().ToString()
                }, h);
                Menu.Ok(r4.Message);
                break;

            case "5":
                var name5 = Menu.Ask("Node nomi");
                var r5 = await c.Node.DrainAsync(new DrainNodeRequest
                {
                    Cluster = cluster, Name = name5, Force = false, IgnoreDaemonsets = true,
                    RequestedBy = "test-client", Reason = "test drain", CorrelationId = Guid.NewGuid().ToString()
                }, h);
                Menu.Ok(r5.Message);
                break;
        }
    }
}
