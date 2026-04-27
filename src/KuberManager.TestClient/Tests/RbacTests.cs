using Grpc.Core;
using KuberManager.Contracts;

namespace KuberManager.TestClient;

internal static class RbacTests
{
    public static async Task Run(Clients c, Metadata h, string cluster)
    {
        Menu.SubMenu("RBAC", ["List Roles", "List ClusterRoles", "List RoleBindings", "List ClusterRoleBindings", "Delete Role", "Delete ClusterRole"]);
        switch (Console.ReadLine()?.Trim())
        {
            case "1":
                var ns = Menu.Ask("Namespace", "app-prod");
                var roles = await c.Rbac.ListRolesAsync(new ListRolesRequest { Cluster = cluster, Namespace = ns }, h);
                foreach (var r in roles.Items)
                    Menu.Print($"{r.Name}  rules={r.Rules.Count}");
                if (roles.Items.Count == 0) Menu.Print("(bo'sh)");
                break;

            case "2":
                var crs = await c.Rbac.ListClusterRolesAsync(new ListClusterRolesRequest { Cluster = cluster }, h);
                foreach (var cr in crs.Items)
                    Menu.Print($"{cr.Name}  rules={cr.Rules.Count}");
                break;

            case "3":
                var ns3 = Menu.Ask("Namespace", "app-prod");
                var rbs = await c.Rbac.ListRoleBindingsAsync(new ListRoleBindingsRequest { Cluster = cluster, Namespace = ns3 }, h);
                foreach (var rb in rbs.Items)
                    Menu.Print($"{rb.Name}  roleKind={rb.RoleKind}  subjects={rb.Subjects.Count}");
                if (rbs.Items.Count == 0) Menu.Print("(bo'sh)");
                break;

            case "4":
                var crbs = await c.Rbac.ListClusterRoleBindingsAsync(new ListClusterRoleBindingsRequest { Cluster = cluster }, h);
                foreach (var crb in crbs.Items)
                    Menu.Print($"{crb.Name}  subjects={crb.Subjects.Count}");
                break;

            case "5":
                var ns5   = Menu.Ask("Namespace", "app-prod");
                var name5 = Menu.Ask("Role nomi");
                var r5 = await c.Rbac.DeleteRoleAsync(new DeleteRoleRequest
                {
                    Cluster = cluster, Namespace = ns5, Name = name5,
                    RequestedBy = "test-client", Reason = "test delete", CorrelationId = Guid.NewGuid().ToString()
                }, h);
                Menu.Ok(r5.Message);
                break;

            case "6":
                var name6 = Menu.Ask("ClusterRole nomi");
                var r6 = await c.Rbac.DeleteClusterRoleAsync(new DeleteClusterRoleRequest
                {
                    Cluster = cluster, Name = name6,
                    RequestedBy = "test-client", Reason = "test delete", CorrelationId = Guid.NewGuid().ToString()
                }, h);
                Menu.Ok(r6.Message);
                break;
        }
    }
}
