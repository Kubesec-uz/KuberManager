using Grpc.Core;
using KuberManager.Contracts;

namespace KuberManager.TestClient;

internal static class ServiceAccountTests
{
    public static async Task Run(Clients c, Metadata h, string cluster)
    {
        Menu.SubMenu("ServiceAccounts", ["List", "Get", "Create", "Delete"]);
        switch (Console.ReadLine()?.Trim())
        {
            case "1":
                var ns = Menu.Ask("Namespace", "app-prod");
                var list = await c.ServiceAccount.ListAsync(new ListServiceAccountsRequest { Cluster = cluster, Namespace = ns }, h);
                foreach (var sa in list.Items)
                    Menu.Print($"{sa.Name}");
                if (list.Items.Count == 0) Menu.Print("(bo'sh)");
                break;

            case "2":
                var ns2   = Menu.Ask("Namespace", "app-prod");
                var name2 = Menu.Ask("ServiceAccount nomi");
                var sa2 = await c.ServiceAccount.GetAsync(new ServiceAccountRef { Cluster = cluster, Namespace = ns2, Name = name2 }, h);
                Menu.Ok($"{sa2.Name}  secrets={sa2.Secrets.Count}");
                break;

            case "3":
                var ns3   = Menu.Ask("Namespace", "app-prod");
                var name3 = Menu.Ask("ServiceAccount nomi");
                var r3 = await c.ServiceAccount.CreateAsync(new CreateServiceAccountRequest
                {
                    Cluster = cluster, Namespace = ns3, Name = name3,
                    RequestedBy = "test-client", Reason = "test", CorrelationId = Guid.NewGuid().ToString()
                }, h);
                Menu.Ok(r3.Message);
                break;

            case "4":
                var ns4   = Menu.Ask("Namespace", "app-prod");
                var name4 = Menu.Ask("ServiceAccount nomi");
                var r4 = await c.ServiceAccount.DeleteAsync(new DeleteServiceAccountRequest
                {
                    Cluster = cluster, Namespace = ns4, Name = name4,
                    RequestedBy = "test-client", Reason = "test delete", CorrelationId = Guid.NewGuid().ToString()
                }, h);
                Menu.Ok(r4.Message);
                break;
        }
    }
}
