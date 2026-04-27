using Grpc.Core;
using KuberManager.Contracts;

namespace KuberManager.TestClient;

internal static class ConfigMapTests
{
    public static async Task Run(Clients c, Metadata h, string cluster)
    {
        Menu.SubMenu("ConfigMaps", ["List", "Get", "Create", "Update", "Delete"]);
        switch (Console.ReadLine()?.Trim())
        {
            case "1":
                var ns = Menu.Ask("Namespace", "app-prod");
                var list = await c.ConfigMap.ListAsync(new ListConfigMapsRequest { Cluster = cluster, Namespace = ns }, h);
                foreach (var cm in list.Configmaps)
                    Menu.Print($"{cm.Name}  keys=[{string.Join(", ", cm.Data.Keys)}]");
                if (list.Configmaps.Count == 0) Menu.Print("(bo'sh)");
                break;

            case "2":
                var ns2   = Menu.Ask("Namespace", "app-prod");
                var name2 = Menu.Ask("ConfigMap nomi");
                var cm2 = await c.ConfigMap.GetAsync(new ConfigMapRef { Cluster = cluster, Namespace = ns2, Name = name2 }, h);
                foreach (var kv in cm2.Data)
                    Menu.Print($"  {kv.Key} = {kv.Value}");
                break;

            case "3":
                var ns3   = Menu.Ask("Namespace", "app-prod");
                var name3 = Menu.Ask("ConfigMap nomi");
                var key3  = Menu.Ask("Key");
                var val3  = Menu.Ask("Value");
                var req3  = new CreateConfigMapRequest
                {
                    Cluster = cluster, Namespace = ns3, Name = name3,
                    RequestedBy = "test-client", Reason = "test", CorrelationId = Guid.NewGuid().ToString()
                };
                req3.Data[key3] = val3;
                var r3 = await c.ConfigMap.CreateAsync(req3, h);
                Menu.Ok(r3.Message);
                break;

            case "4":
                var ns4   = Menu.Ask("Namespace", "app-prod");
                var name4 = Menu.Ask("ConfigMap nomi");
                var key4  = Menu.Ask("Key");
                var val4  = Menu.Ask("Yangi Value");
                var req4  = new UpdateConfigMapRequest
                {
                    Cluster = cluster, Namespace = ns4, Name = name4,
                    RequestedBy = "test-client", Reason = "test update", CorrelationId = Guid.NewGuid().ToString()
                };
                req4.Data[key4] = val4;
                var r4 = await c.ConfigMap.UpdateAsync(req4, h);
                Menu.Ok(r4.Message);
                break;

            case "5":
                var ns5   = Menu.Ask("Namespace", "app-prod");
                var name5 = Menu.Ask("ConfigMap nomi");
                var r5 = await c.ConfigMap.DeleteAsync(new DeleteConfigMapRequest
                {
                    Cluster = cluster, Namespace = ns5, Name = name5,
                    RequestedBy = "test-client", Reason = "test delete", CorrelationId = Guid.NewGuid().ToString()
                }, h);
                Menu.Ok(r5.Message);
                break;
        }
    }
}
