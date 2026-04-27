using Grpc.Core;
using KuberManager.Contracts;

namespace KuberManager.TestClient;

internal static class PodTests
{
    public static async Task Run(Clients c, Metadata h, string cluster)
    {
        Menu.SubMenu("Pods", ["List", "Get", "Logs", "Delete", "Exec"]);
        switch (Console.ReadLine()?.Trim())
        {
            case "1":
                var ns = Menu.Ask("Namespace", "app-prod");
                var list = await c.Pod.ListAsync(new ListPodsRequest { Cluster = cluster }, h);
                foreach (var p in list.Pods)
                    Menu.Print($"{p.Name}  [{p.Phase}]  {p.PodIp}  node={p.NodeName}");
                if (list.Pods.Count == 0) Menu.Print("(bo'sh)");
                break;

            case "2":
                var ns2  = Menu.Ask("Namespace", "app-prod");
                var pod2 = Menu.Ask("Pod nomi");
                var info = await c.Pod.GetAsync(new PodRef { Cluster = cluster, Namespace = ns2, PodName = pod2 }, h);
                Menu.Ok($"{info.Name}  Phase={info.Phase}  IP={info.PodIp}  Node={info.NodeName}");
                foreach (var ct in info.Containers)
                    Menu.Print($"  container: {ct.Name}  ready={ct.Ready}  restarts={ct.RestartCount}  state={ct.State}");
                break;

            case "3":
                var ns3  = Menu.Ask("Namespace", "app-prod");
                var pod3 = Menu.Ask("Pod nomi");
                var tail = int.Parse(Menu.Ask("Tail lines", "50"));
                var logs = await c.Pod.GetLogsAsync(new PodLogsRequest
                    { Cluster = cluster, Namespace = ns3, PodName = pod3, TailLines = tail }, h);
                Console.WriteLine(logs.Content);
                break;

            case "4":
                var ns4  = Menu.Ask("Namespace", "app-prod");
                var pod4 = Menu.Ask("Pod nomi");
                var r4 = await c.Pod.DeleteAsync(new DeletePodRequest
                {
                    Cluster = cluster, Namespace = ns4, PodName = pod4,
                    RequestedBy = "test-client", Reason = "test delete", CorrelationId = Guid.NewGuid().ToString()
                }, h);
                Menu.Ok(r4.Message);
                break;

            case "5":
                var ns5  = Menu.Ask("Namespace", "app-prod");
                var pod5 = Menu.Ask("Pod nomi");
                var cont = Menu.Ask("Container nomi");
                var cmd  = Menu.Ask("Buyruq", "ls -la");
                var req5 = new ExecRequest
                {
                    Cluster = cluster, Namespace = ns5, PodName = pod5,
                    ContainerName = cont, RequestedBy = "test-client", CorrelationId = Guid.NewGuid().ToString()
                };
                req5.Command.AddRange(cmd.Split(' '));
                {
                    using var call = c.Pod.Exec(req5, h);
                    await foreach (var reply in call.ResponseStream.ReadAllAsync())
                        Console.Write(reply.Output);
                    Console.WriteLine();
                }
                break;
        }
    }
}
