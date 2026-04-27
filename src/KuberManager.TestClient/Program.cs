using Grpc.Core;
using Grpc.Net.Client;
using KuberManager.Contracts;
using KuberManager.TestClient;

// ── Config ────────────────────────────────────────────────────────────────────
var address = Environment.GetEnvironmentVariable("KUBERMANAGER_ADDRESS") ?? "http://localhost:30501";
var apiKey  = Environment.GetEnvironmentVariable("KUBERMANAGER_APIKEY")  ?? "change-me";
var cluster = Environment.GetEnvironmentVariable("KUBERMANAGER_CLUSTER") ?? "default";

Console.WriteLine($"KuberManager Test Client");
Console.WriteLine($"Address : {address}");
Console.WriteLine($"Cluster : {cluster}");
Console.WriteLine();

var channel = GrpcChannel.ForAddress(address, new GrpcChannelOptions
{
    HttpHandler = new HttpClientHandler()
});

var headers = new Metadata { new Metadata.Entry("authorization", $"Bearer {apiKey}") };

var clients = new Clients(channel);

// ── Main menu ─────────────────────────────────────────────────────────────────
while (true)
{
    Menu.PrintMain();
    var choice = Console.ReadLine()?.Trim();

    try
    {
        switch (choice)
        {
            case "0": await HealthTests.Run(clients, headers, cluster); break;
            case "1": await DeploymentTests.Run(clients, headers, cluster); break;
            case "2": await PodTests.Run(clients, headers, cluster); break;
            case "3": await NamespaceTests.Run(clients, headers, cluster); break;
            case "4": await ConfigMapTests.Run(clients, headers, cluster); break;
            case "5": await SecretTests.Run(clients, headers, cluster); break;
            case "6": await ServiceTests.Run(clients, headers, cluster); break;
            case "7": await IngressTests.Run(clients, headers, cluster); break;
            case "8": await StatefulSetTests.Run(clients, headers, cluster); break;
            case "9": await DaemonSetTests.Run(clients, headers, cluster); break;
            case "10": await JobTests.Run(clients, headers, cluster); break;
            case "11": await NodeTests.Run(clients, headers, cluster); break;
            case "12": await PvcTests.Run(clients, headers, cluster); break;
            case "13": await HpaTests.Run(clients, headers, cluster); break;
            case "14": await RbacTests.Run(clients, headers, cluster); break;
            case "15": await ServiceAccountTests.Run(clients, headers, cluster); break;
            case "16": await NetworkPolicyTests.Run(clients, headers, cluster); break;
            case "q": case "Q": Console.WriteLine("Bye!"); return;
            default: Console.WriteLine("Noto'g'ri tanlov."); break;
        }
    }
    catch (RpcException ex)
    {
        Console.ForegroundColor = ConsoleColor.Red;
        Console.WriteLine($"[gRPC Error] {ex.Status.StatusCode}: {ex.Status.Detail}");
        Console.ResetColor();
    }
    catch (Exception ex)
    {
        Console.ForegroundColor = ConsoleColor.Red;
        Console.WriteLine($"[Error] {ex.Message}");
        Console.ResetColor();
    }

    Console.WriteLine();
}
