using Grpc.Core;
using KuberManager.Contracts;

namespace KuberManager.TestClient;

internal static class ServiceTests
{
    public static async Task Run(Clients c, Metadata h, string cluster)
    {
        Menu.SubMenu("Services", ["List", "Get", "Create", "Delete"]);
        switch (Console.ReadLine()?.Trim())
        {
            case "1":
                var ns = Menu.Ask("Namespace", "app-prod");
                var list = await c.Service.ListAsync(new ListServicesRequest { Cluster = cluster, Namespace = ns }, h);
                foreach (var s in list.Services)
                    Menu.Print($"{s.Name}  type={s.Type}  clusterIP={s.ClusterIp}");
                if (list.Services.Count == 0) Menu.Print("(bo'sh)");
                break;

            case "2":
                var ns2   = Menu.Ask("Namespace", "app-prod");
                var name2 = Menu.Ask("Service nomi");
                var svc = await c.Service.GetAsync(new ServiceRef { Cluster = cluster, Namespace = ns2, Name = name2 }, h);
                Menu.Ok($"{svc.Name}  type={svc.Type}  IP={svc.ClusterIp}");
                break;

            case "3":
                var ns3   = Menu.Ask("Namespace", "app-prod");
                var name3 = Menu.Ask("Service nomi");
                var port3 = int.Parse(Menu.Ask("Port", "80"));
                var tport = int.Parse(Menu.Ask("TargetPort", "8080"));
                var req3  = new CreateServiceRequest
                {
                    Cluster = cluster, Namespace = ns3, Name = name3, Type = "ClusterIP",
                    RequestedBy = "test-client", Reason = "test", CorrelationId = Guid.NewGuid().ToString()
                };
                req3.Ports.Add(new ServicePort { Name = "http", Port = port3, TargetPort = tport, Protocol = "TCP" });
                var r3 = await c.Service.CreateAsync(req3, h);
                Menu.Ok(r3.Message);
                break;

            case "4":
                var ns4   = Menu.Ask("Namespace", "app-prod");
                var name4 = Menu.Ask("Service nomi");
                var r4 = await c.Service.DeleteAsync(new DeleteServiceRequest
                {
                    Cluster = cluster, Namespace = ns4, Name = name4,
                    RequestedBy = "test-client", Reason = "test delete", CorrelationId = Guid.NewGuid().ToString()
                }, h);
                Menu.Ok(r4.Message);
                break;
        }
    }
}
