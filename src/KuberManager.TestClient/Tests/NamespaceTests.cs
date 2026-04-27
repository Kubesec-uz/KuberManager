using Grpc.Core;
using KuberManager.Contracts;

namespace KuberManager.TestClient;

internal static class NamespaceTests
{
    public static async Task Run(Clients c, Metadata h, string cluster)
    {
        Menu.SubMenu("Namespaces", ["List", "Get", "Create", "Delete"]);
        switch (Console.ReadLine()?.Trim())
        {
            case "1":
                var list = await c.Namespace.ListAsync(new ListNamespacesRequest { Cluster = cluster }, h);
                foreach (var n in list.Namespaces)
                    Menu.Print($"{n.Name}  [{n.Status}]");
                break;

            case "2":
                var name2 = Menu.Ask("Namespace nomi");
                var info = await c.Namespace.GetAsync(new NamespaceRef { Cluster = cluster, Name = name2 }, h);
                Menu.Ok($"{info.Name}  Status={info.Status}  Age={info.CreatedAt}");
                break;

            case "3":
                var name3 = Menu.Ask("Namespace nomi");
                var r3 = await c.Namespace.CreateAsync(new CreateNamespaceRequest
                {
                    Cluster = cluster, Name = name3,
                    RequestedBy = "test-client", Reason = "test", CorrelationId = Guid.NewGuid().ToString()
                }, h);
                Menu.Ok(r3.Message);
                break;

            case "4":
                var name4 = Menu.Ask("Namespace nomi");
                var r4 = await c.Namespace.DeleteAsync(new DeleteNamespaceRequest
                {
                    Cluster = cluster, Name = name4,
                    RequestedBy = "test-client", Reason = "test delete", CorrelationId = Guid.NewGuid().ToString()
                }, h);
                Menu.Ok(r4.Message);
                break;
        }
    }
}
