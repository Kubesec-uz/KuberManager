using Grpc.Core;
using KuberManager.Contracts;

namespace KuberManager.TestClient;

internal static class SecretTests
{
    public static async Task Run(Clients c, Metadata h, string cluster)
    {
        Menu.SubMenu("Secrets", ["List", "Get", "Create", "Delete"]);
        switch (Console.ReadLine()?.Trim())
        {
            case "1":
                var ns = Menu.Ask("Namespace", "app-prod");
                var list = await c.Secret.ListAsync(new ListSecretsRequest { Cluster = cluster, Namespace = ns }, h);
                foreach (var s in list.Secrets)
                    Menu.Print($"{s.Name}  type={s.Type}  keys=[{string.Join(", ", s.Data.Keys)}]");
                if (list.Secrets.Count == 0) Menu.Print("(bo'sh)");
                break;

            case "2":
                var ns2   = Menu.Ask("Namespace", "app-prod");
                var name2 = Menu.Ask("Secret nomi");
                var s2 = await c.Secret.GetAsync(new SecretRef { Cluster = cluster, Namespace = ns2, Name = name2 }, h);
                Menu.Ok($"{s2.Name}  type={s2.Type}  keys=[{string.Join(", ", s2.Data.Keys)}]");
                break;

            case "3":
                var ns3   = Menu.Ask("Namespace", "app-prod");
                var name3 = Menu.Ask("Secret nomi");
                var key3  = Menu.Ask("Key");
                var val3  = Menu.Ask("Value");
                var req3  = new CreateSecretRequest
                {
                    Cluster = cluster, Namespace = ns3, Name = name3, Type = "Opaque",
                    RequestedBy = "test-client", Reason = "test", CorrelationId = Guid.NewGuid().ToString()
                };
                req3.StringData[key3] = val3;
                var r3 = await c.Secret.CreateAsync(req3, h);
                Menu.Ok(r3.Message);
                break;

            case "4":
                var ns4   = Menu.Ask("Namespace", "app-prod");
                var name4 = Menu.Ask("Secret nomi");
                var r4 = await c.Secret.DeleteAsync(new DeleteSecretRequest
                {
                    Cluster = cluster, Namespace = ns4, Name = name4,
                    RequestedBy = "test-client", Reason = "test delete", CorrelationId = Guid.NewGuid().ToString()
                }, h);
                Menu.Ok(r4.Message);
                break;
        }
    }
}
