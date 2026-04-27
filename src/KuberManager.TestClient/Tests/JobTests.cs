using Grpc.Core;
using KuberManager.Contracts;

namespace KuberManager.TestClient;

internal static class JobTests
{
    public static async Task Run(Clients c, Metadata h, string cluster)
    {
        Menu.SubMenu("Jobs / CronJobs", ["Job: List", "Job: Delete", "CronJob: List", "CronJob: Suspend", "CronJob: Resume", "CronJob: Delete"]);
        switch (Console.ReadLine()?.Trim())
        {
            case "1":
                var ns = Menu.Ask("Namespace", "app-prod");
                var list = await c.Job.ListAsync(new ListJobsRequest { Cluster = cluster, Namespace = ns }, h);
                foreach (var j in list.Items)
                    Menu.Print($"{j.Name}  succeeded={j.Succeeded}  failed={j.Failed}  active={j.Active}");
                if (list.Items.Count == 0) Menu.Print("(bo'sh)");
                break;

            case "2":
                var ns2   = Menu.Ask("Namespace", "app-prod");
                var name2 = Menu.Ask("Job nomi");
                var r2 = await c.Job.DeleteAsync(new DeleteJobRequest
                {
                    Cluster = cluster, Namespace = ns2, Name = name2,
                    RequestedBy = "test-client", Reason = "test delete", CorrelationId = Guid.NewGuid().ToString()
                }, h);
                Menu.Ok(r2.Message);
                break;

            case "3":
                var ns3 = Menu.Ask("Namespace", "app-prod");
                var clist = await c.CronJob.ListAsync(new ListCronJobsRequest { Cluster = cluster, Namespace = ns3 }, h);
                foreach (var cj in clist.Items)
                    Menu.Print($"{cj.Name}  schedule={cj.Schedule}  suspended={cj.Suspended}");
                if (clist.Items.Count == 0) Menu.Print("(bo'sh)");
                break;

            case "4":
                var ns4   = Menu.Ask("Namespace", "app-prod");
                var name4 = Menu.Ask("CronJob nomi");
                var r4 = await c.CronJob.SuspendAsync(new SuspendCronJobRequest
                {
                    Cluster = cluster, Namespace = ns4, Name = name4,
                    RequestedBy = "test-client", Reason = "test suspend", CorrelationId = Guid.NewGuid().ToString()
                }, h);
                Menu.Ok(r4.Message);
                break;

            case "5":
                var ns5   = Menu.Ask("Namespace", "app-prod");
                var name5 = Menu.Ask("CronJob nomi");
                var r5 = await c.CronJob.ResumeAsync(new ResumeCronJobRequest
                {
                    Cluster = cluster, Namespace = ns5, Name = name5,
                    RequestedBy = "test-client", Reason = "test resume", CorrelationId = Guid.NewGuid().ToString()
                }, h);
                Menu.Ok(r5.Message);
                break;

            case "6":
                var ns6   = Menu.Ask("Namespace", "app-prod");
                var name6 = Menu.Ask("CronJob nomi");
                var r6 = await c.CronJob.DeleteAsync(new DeleteCronJobRequest
                {
                    Cluster = cluster, Namespace = ns6, Name = name6,
                    RequestedBy = "test-client", Reason = "test delete", CorrelationId = Guid.NewGuid().ToString()
                }, h);
                Menu.Ok(r6.Message);
                break;
        }
    }
}
