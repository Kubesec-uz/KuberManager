namespace KuberManager.TestClient;

internal static class Menu
{
    public static void PrintMain()
    {
        Console.WriteLine("══════════════════════════════════════");
        Console.WriteLine(" 0  Health Check");
        Console.WriteLine(" 1  Deployments");
        Console.WriteLine(" 2  Pods");
        Console.WriteLine(" 3  Namespaces");
        Console.WriteLine(" 4  ConfigMaps");
        Console.WriteLine(" 5  Secrets");
        Console.WriteLine(" 6  Services");
        Console.WriteLine(" 7  Ingresses");
        Console.WriteLine(" 8  StatefulSets");
        Console.WriteLine(" 9  DaemonSets");
        Console.WriteLine(" 10 Jobs / CronJobs");
        Console.WriteLine(" 11 Nodes");
        Console.WriteLine(" 12 PersistentVolumeClaims");
        Console.WriteLine(" 13 HPA");
        Console.WriteLine(" 14 RBAC");
        Console.WriteLine(" 15 ServiceAccounts");
        Console.WriteLine(" 16 NetworkPolicies");
        Console.WriteLine(" q  Chiqish");
        Console.WriteLine("══════════════════════════════════════");
        Console.Write("Tanlang: ");
    }

    public static string Ask(string prompt, string? defaultValue = null)
    {
        Console.Write(defaultValue is null ? $"  {prompt}: " : $"  {prompt} [{defaultValue}]: ");
        var input = Console.ReadLine()?.Trim();
        return string.IsNullOrEmpty(input) ? (defaultValue ?? "") : input;
    }

    public static void Ok(string msg)
    {
        Console.ForegroundColor = ConsoleColor.Green;
        Console.WriteLine($"  ✓ {msg}");
        Console.ResetColor();
    }

    public static void Print(object obj)
    {
        Console.ForegroundColor = ConsoleColor.Cyan;
        Console.WriteLine($"  {obj}");
        Console.ResetColor();
    }

    public static void SubMenu(string title, string[] items)
    {
        Console.WriteLine($"\n── {title} ──");
        for (int i = 0; i < items.Length; i++)
            Console.WriteLine($"  {i + 1}. {items[i]}");
        Console.Write("  Tanlang: ");
    }
}
