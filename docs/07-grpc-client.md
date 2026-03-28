# 07 — KubeManager gRPC Client (Kubesec.Auth tomon)

## Faylllar

```
Kubesec.Auth/KubeManager/
├── IKubeManagerClient.cs    ← Interface
├── KubeManagerClient.cs     ← Implementatsiya
└── KubeManagerOptions.cs    ← Config

Kubesec.Auth/Protos/         ← Proto fayllar (client-side)
├── common.proto
├── deployment.proto
├── pod.proto
├── namespace.proto
├── configmap.proto
└── health.proto
```

## KubeManagerOptions

```csharp
public sealed class KubeManagerOptions
{
    public const string Section = "KubeManager";
    public string Address { get; set; } = "";
}
```

**appsettings.json (production):**
```json
{
  "KubeManager": {
    "Address": "http://kubermanager.platform.svc.cluster.local:5001"
  }
}
```

**appsettings.Development.json:**
```json
{
  "KubeManager": {
    "Address": "http://localhost:5001"
  }
}
```

## KubeManagerClient

**Lifetime:** Singleton
**5 ta gRPC typed client:**

```csharp
private readonly GrpcChannel _channel;
private readonly DeploymentService.DeploymentServiceClient  _deployment;
private readonly PodService.PodServiceClient                _pod;
private readonly NamespaceService.NamespaceServiceClient    _namespace;
private readonly ConfigMapService.ConfigMapServiceClient    _configMap;
private readonly ClusterHealthService.ClusterHealthServiceClient _health;
```

Constructor da bir marta yaratiladi, dispose bo'lguncha ishlatiladi.

## Deployment metodlar

```csharp
Task<DeploymentStatusReply> GetDeploymentStatusAsync(string cluster, string ns, string name, CancellationToken ct)

Task<DeploymentListReply> ListDeploymentsAsync(string cluster, string ns, CancellationToken ct)

Task<OperationReply> CreateDeploymentAsync(string cluster, string ns, CreateDeploymentRequest request, CancellationToken ct)
// Eslatma: request.Cluster va request.Namespace client da to'ldiriladi

Task<OperationReply> DeleteDeploymentAsync(string cluster, string ns, string name, string requestedBy, string reason, string correlationId, CancellationToken ct)

Task<OperationReply> ScaleDeploymentAsync(string cluster, string ns, string name, int replicas, string requestedBy, string reason, string correlationId, CancellationToken ct)

Task<OperationReply> RestartDeploymentAsync(string cluster, string ns, string name, string requestedBy, string reason, string correlationId, CancellationToken ct)
```

## Pod metodlar

```csharp
Task<PodListReply> ListPodsAsync(string cluster, string ns, string? appLabel, CancellationToken ct)
// appLabel null bo'lsa "" uzatiladi

Task<PodInfoReply> GetPodAsync(string cluster, string ns, string podName, CancellationToken ct)

Task<PodLogsReply> GetPodLogsAsync(string cluster, string ns, string podName, string? container, int tailLines, CancellationToken ct)
// container null bo'lsa "" uzatiladi

Task<OperationReply> DeletePodAsync(string cluster, string ns, string podName, string requestedBy, string reason, string correlationId, CancellationToken ct)

IAsyncEnumerable<ExecReply> ExecInPodAsync(
    string cluster, string ns, string podName, string container,
    IEnumerable<string> command, string requestedBy, string correlationId,
    CancellationToken ct)
// Server-streaming → IAsyncEnumerable orqali chunk lar keladi
```

## Namespace metodlar

```csharp
Task<NamespaceListReply> ListNamespacesAsync(string cluster, CancellationToken ct)

Task<NamespaceInfoReply> GetNamespaceAsync(string cluster, string name, CancellationToken ct)

Task<OperationReply> CreateNamespaceAsync(string cluster, string name, Dictionary<string, string>? labels, string requestedBy, string reason, string correlationId, CancellationToken ct)

Task<OperationReply> DeleteNamespaceAsync(string cluster, string name, string requestedBy, string reason, string correlationId, CancellationToken ct)
```

## ConfigMap metodlar

```csharp
Task<ConfigMapListReply> ListConfigMapsAsync(string cluster, string ns, CancellationToken ct)

Task<ConfigMapReply> GetConfigMapAsync(string cluster, string ns, string name, CancellationToken ct)

Task<OperationReply> CreateConfigMapAsync(string cluster, string ns, string name, Dictionary<string, string> data, string requestedBy, string reason, string correlationId, CancellationToken ct)

Task<OperationReply> UpdateConfigMapAsync(string cluster, string ns, string name, Dictionary<string, string> data, string requestedBy, string reason, string correlationId, CancellationToken ct)

Task<OperationReply> DeleteConfigMapAsync(string cluster, string ns, string name, string requestedBy, string reason, string correlationId, CancellationToken ct)
```

## Health metodlar

```csharp
Task<ClusterHealthReply> GetClusterHealthAsync(string cluster, CancellationToken ct)
```

## DI Registration

`Extensions/ServiceExtension.cs` da:
```csharp
builder.Services.Configure<KubeManagerOptions>(
    builder.Configuration.GetSection(KubeManagerOptions.Section));

builder.Services.AddSingleton<IKubeManagerClient, KubeManagerClient>();
```

## Exec streaming — REST tomoni

`ClusterController.ExecInPodAsync` gRPC stream ni HTTP response ga uzatadi:

```csharp
[HttpPost("{cluster}/namespaces/{ns}/pods/{podName}/exec")]
public async Task ExecInPodAsync(...)
{
    Response.ContentType = "text/plain";
    Response.Headers.Append("X-Accel-Buffering", "no");  // nginx buffer o'chiriladi

    await foreach (var chunk in _kube.ExecInPodAsync(..., ct))
    {
        if (!string.IsNullOrEmpty(chunk.Output))
            await Response.WriteAsync(chunk.Output, ct);
        await Response.Body.FlushAsync(ct);    // har chunk da flush
    }
}
```

`X-Accel-Buffering: no` — nginx ga buffering qilmaslikni aytadi, shunda output real-time keladi.

## Proto import konfiguratsiyasi

`Kubesec.Auth.csproj` da proto fayllar client sifatida registratsiya:

```xml
<Protobuf Include="Protos\common.proto"    GrpcServices="None" />
<Protobuf Include="Protos\deployment.proto" GrpcServices="Client" />
<Protobuf Include="Protos\pod.proto"        GrpcServices="Client" />
<Protobuf Include="Protos\health.proto"     GrpcServices="Client" />
<Protobuf Include="Protos\namespace.proto"  GrpcServices="Client" />
<Protobuf Include="Protos\configmap.proto"  GrpcServices="Client" />
```

`GrpcServices="None"` — `common.proto` uchun, chunki unda service yo'q, faqat `OperationReply` message bor.
`GrpcServices="Client"` — faqat client stub generatsiya qilinadi (server stub emas).
