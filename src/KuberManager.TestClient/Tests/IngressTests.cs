using Grpc.Core;
using KuberManager.Contracts;

namespace KuberManager.TestClient;

internal static class IngressTests
{
    public static async Task Run(Clients c, Metadata h, string cluster)
    {
        Menu.SubMenu("Ingresses", ["List", "Get", "Create", "Delete"]);
        switch (Console.ReadLine()?.Trim())
        {
            case "1":
                var ns = Menu.Ask("Namespace", "app-prod");
                var list = await c.Ingress.ListAsync(new ListIngressesRequest { Cluster = cluster, Namespace = ns }, h);
                foreach (var i in list.Ingresses)
                {
                    var hosts = string.Join(", ", i.Rules.Select(r => r.Host));
                    Menu.Print($"{i.Name}  class={i.IngressClass}  hosts=[{hosts}]");
                }
                if (list.Ingresses.Count == 0) Menu.Print("(bo'sh)");
                break;

            case "2":
                var ns2   = Menu.Ask("Namespace", "app-prod");
                var name2 = Menu.Ask("Ingress nomi");
                var ing = await c.Ingress.GetAsync(new IngressRef { Cluster = cluster, Namespace = ns2, Name = name2 }, h);
                Menu.Ok($"{ing.Name}  class={ing.IngressClass}");
                foreach (var rule in ing.Rules)
                    foreach (var path in rule.Paths)
                        Menu.Print($"  {rule.Host}{path.Path} → {path.ServiceName}:{path.ServicePort}");
                break;

            case "3":
                var ns3   = Menu.Ask("Namespace", "app-prod");
                var name3 = Menu.Ask("Ingress nomi");
                var host3 = Menu.Ask("Host", "example.com");
                var path3 = Menu.Ask("Path", "/");
                var svc3  = Menu.Ask("Backend service nomi");
                var port3 = int.Parse(Menu.Ask("Backend port", "80"));
                var cls3  = Menu.Ask("Ingress class", "nginx");
                var req3  = new CreateIngressRequest
                {
                    Cluster = cluster, Namespace = ns3, Name = name3, IngressClass = cls3,
                    RequestedBy = "test-client", Reason = "test", CorrelationId = Guid.NewGuid().ToString()
                };
                var rule3 = new IngressRuleRequest { Host = host3 };
                rule3.Paths.Add(new IngressPathRequest { Path = path3, PathType = "Prefix", ServiceName = svc3, ServicePort = port3 });
                req3.Rules.Add(rule3);
                var r3 = await c.Ingress.CreateAsync(req3, h);
                Menu.Ok(r3.Message);
                break;

            case "4":
                var ns4   = Menu.Ask("Namespace", "app-prod");
                var name4 = Menu.Ask("Ingress nomi");
                var r4 = await c.Ingress.DeleteAsync(new DeleteIngressRequest
                {
                    Cluster = cluster, Namespace = ns4, Name = name4,
                    RequestedBy = "test-client", Reason = "test delete", CorrelationId = Guid.NewGuid().ToString()
                }, h);
                Menu.Ok(r4.Message);
                break;
        }
    }
}
