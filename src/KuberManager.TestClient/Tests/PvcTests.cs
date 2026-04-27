using Grpc.Core;
using KuberManager.Contracts;

namespace KuberManager.TestClient;

internal static class PvcTests
{
    public static async Task Run(Clients c, Metadata h, string cluster)
    {
        Menu.SubMenu("PVCs", ["List", "Get", "Create", "Delete"]);
        switch (Console.ReadLine()?.Trim())
        {
            case "1":
                var ns = Menu.Ask("Namespace", "app-prod");
                var list = await c.Pvc.ListAsync(new ListPvcsRequest { Cluster = cluster, Namespace = ns }, h);
                foreach (var p in list.Items)
                    Menu.Print($"{p.Name}  status={p.Status}  capacity={p.Capacity}  storageClass={p.StorageClass}");
                if (list.Items.Count == 0) Menu.Print("(bo'sh)");
                break;

            case "2":
                var ns2   = Menu.Ask("Namespace", "app-prod");
                var name2 = Menu.Ask("PVC nomi");
                var pvc = await c.Pvc.GetAsync(new PvcRef { Cluster = cluster, Namespace = ns2, Name = name2 }, h);
                Menu.Ok($"{pvc.Name}  status={pvc.Status}  capacity={pvc.Capacity}  accessModes={pvc.AccessModes}");
                break;

            case "3":
                var ns3     = Menu.Ask("Namespace", "app-prod");
                var name3   = Menu.Ask("PVC nomi");
                var storage = Menu.Ask("Storage (e.g. 1Gi)", "1Gi");
                var sc      = Menu.Ask("StorageClass", "standard");
                var req3    = new CreatePvcRequest
                {
                    Cluster = cluster, Namespace = ns3, Name = name3,
                    Storage = storage, StorageClass = sc,
                    RequestedBy = "test-client", Reason = "test", CorrelationId = Guid.NewGuid().ToString()
                };
                req3.AccessModes.Add("ReadWriteOnce");
                var r3 = await c.Pvc.CreateAsync(req3, h);
                Menu.Ok(r3.Message);
                break;

            case "4":
                var ns4   = Menu.Ask("Namespace", "app-prod");
                var name4 = Menu.Ask("PVC nomi");
                var r4 = await c.Pvc.DeleteAsync(new DeletePvcRequest
                {
                    Cluster = cluster, Namespace = ns4, Name = name4,
                    RequestedBy = "test-client", Reason = "test delete", CorrelationId = Guid.NewGuid().ToString()
                }, h);
                Menu.Ok(r4.Message);
                break;
        }
    }
}
