using Grpc.Core;
using KuberManager.Contracts;

namespace KuberManager.TestClient;

internal static class DeploymentTests
{
    public static async Task Run(Clients c, Metadata h, string cluster)
    {
        Menu.SubMenu("Deployments", ["List", "Get Status", "Scale", "Restart", "Create", "Delete"]);
        switch (Console.ReadLine()?.Trim())
        {
            case "1":
                var ns = Menu.Ask("Namespace", "app-prod");
                var list = await c.Deployment.ListAsync(new ListDeploymentsRequest { Cluster = cluster, Namespace = ns }, h);
                foreach (var d in list.Deployments)
                    Menu.Print($"{d.Name}  {d.ReadyReplicas}/{d.DesiredReplicas}  [{d.Status}]");
                if (list.Deployments.Count == 0) Menu.Print("(bo'sh)");
                break;

            case "2":
                var ns2 = Menu.Ask("Namespace", "app-prod");
                var name2 = Menu.Ask("Deployment nomi");
                var status = await c.Deployment.GetStatusAsync(new DeploymentRef { Cluster = cluster, Namespace = ns2, Name = name2 }, h);
                Menu.Ok($"{status.Name}  Ready={status.ReadyReplicas}/{status.DesiredReplicas}  Status={status.Status}");
                break;

            case "3":
                var ns3 = Menu.Ask("Namespace", "app-prod");
                var name3 = Menu.Ask("Deployment nomi");
                var rep = int.Parse(Menu.Ask("Replica soni", "1"));
                var r3 = await c.Deployment.ScaleAsync(new ScaleDeploymentRequest
                {
                    Cluster = cluster, Namespace = ns3, Name = name3, Replicas = rep,
                    RequestedBy = "test-client", Reason = "manual scale", CorrelationId = Guid.NewGuid().ToString()
                }, h);
                Menu.Ok(r3.Message);
                break;

            case "4":
                var ns4 = Menu.Ask("Namespace", "app-prod");
                var name4 = Menu.Ask("Deployment nomi");
                var r4 = await c.Deployment.RestartAsync(new RestartDeploymentRequest
                {
                    Cluster = cluster, Namespace = ns4, Name = name4,
                    RequestedBy = "test-client", Reason = "manual restart", CorrelationId = Guid.NewGuid().ToString()
                }, h);
                Menu.Ok(r4.Message);
                break;

            case "5":
                var ns5   = Menu.Ask("Namespace", "app-prod");
                var name5 = Menu.Ask("Deployment nomi");
                var image = Menu.Ask("Image", "nginx:latest");
                var reps  = int.Parse(Menu.Ask("Replicas", "1"));
                var req = new CreateDeploymentRequest
                {
                    Cluster = cluster, Namespace = ns5, Name = name5, Image = image, Replicas = reps,
                    RequestedBy = "test-client", Reason = "test create", CorrelationId = Guid.NewGuid().ToString()
                };
                var r5 = await c.Deployment.CreateAsync(req, h);
                Menu.Ok(r5.Message);
                break;

            case "6":
                var ns6   = Menu.Ask("Namespace", "app-prod");
                var name6 = Menu.Ask("Deployment nomi");
                var r6 = await c.Deployment.DeleteAsync(new DeleteDeploymentRequest
                {
                    Cluster = cluster, Namespace = ns6, Name = name6,
                    RequestedBy = "test-client", Reason = "test delete", CorrelationId = Guid.NewGuid().ToString()
                }, h);
                Menu.Ok(r6.Message);
                break;
        }
    }
}
