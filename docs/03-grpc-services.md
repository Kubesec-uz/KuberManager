# 03 — gRPC Services: To'liq Spetsifikatsiya

Barcha proto fayllar: `src/KuberManager.Service/Protos/`
C# namespace: `KuberManager.Contracts`
Proto package: `kubermanager`

---

## common.proto

Barcha servislarda ishlatiladi. Har bir mutation operatsiyasi shu `OperationReply` ni qaytaradi.

```protobuf
message OperationReply {
  bool success = 1;
  string message = 2;
  string correlation_id = 3;
}
```

---

## DeploymentService

**Proto fayl:** `deployment.proto`
**C# implementatsiya:** `Grpc/DeploymentGrpcService.cs`
**Manager:** `Application/DeploymentManager.cs`

### RPC metodlar

| Metod | Request | Response | Tur |
|---|---|---|---|
| `GetStatus` | `DeploymentRef` | `DeploymentStatusReply` | Unary |
| `List` | `ListDeploymentsRequest` | `DeploymentListReply` | Unary |
| `Create` | `CreateDeploymentRequest` | `OperationReply` | Unary |
| `Delete` | `DeleteDeploymentRequest` | `OperationReply` | Unary |
| `Scale` | `ScaleDeploymentRequest` | `OperationReply` | Unary |
| `Restart` | `RestartDeploymentRequest` | `OperationReply` | Unary |

### Messageler

**`DeploymentRef`** — bitta deployment ni identifikatsiya qilish:
```protobuf
message DeploymentRef {
  string cluster   = 1;
  string namespace = 2;
  string name      = 3;
}
```

**`CreateDeploymentRequest`** — yangi deployment yaratish:
```protobuf
message CreateDeploymentRequest {
  string cluster        = 1;
  string namespace      = 2;
  string name           = 3;
  string image          = 4;   // masalan: "nginx:1.25"
  int32  replicas       = 5;
  map<string, string> labels   = 6;
  map<string, string> env_vars = 7;
  repeated ContainerPort ports = 8;
  string requested_by   = 9;
  string reason         = 10;
  string correlation_id = 11;
}

message ContainerPort {
  string name           = 1;
  int32  container_port = 2;   // C# da: ContainerPort_ (proto naming conflict)
  string protocol       = 3;   // "TCP" yoki "UDP", default: "TCP"
}
```

**`ScaleDeploymentRequest`**:
```protobuf
message ScaleDeploymentRequest {
  string cluster        = 1;
  string namespace      = 2;
  string name           = 3;
  int32  replicas       = 4;
  string requested_by   = 5;
  string reason         = 6;
  string correlation_id = 7;
}
```

**`DeleteDeploymentRequest`** / **`RestartDeploymentRequest`** — bir xil tuzilma: `cluster + namespace + name + requested_by + reason + correlation_id`

**`DeploymentStatusReply`**:
```protobuf
message DeploymentStatusReply {
  string name                = 1;
  string namespace           = 2;
  int32  desired_replicas    = 3;
  int32  ready_replicas      = 4;
  int32  available_replicas  = 5;
  string strategy            = 6;   // "RollingUpdate", "Recreate"
  string status              = 7;   // k8s condition type
}
```

### Restart qanday ishlaydi?

Kubernetes deployment ni restart qilish uchun to'g'ridan-to'g'ri "restart" buyrug'i yo'q.
`KubernetesFacade` pod template annotatsiyasiga `kubectl.kubernetes.io/restartedAt` qo'shib MergePatch beradi.
K8s bu annotatsiya o'zgarganda yangi podlarni boshlaydi.

```csharp
// KubernetesFacade.PatchDeploymentRestartAsync
var patch = new V1Patch(new {
    spec = new {
        template = new {
            metadata = new {
                annotations = new Dictionary<string, string> {
                    ["kubectl.kubernetes.io/restartedAt"] = DateTimeOffset.UtcNow.ToString("O")
                }
            }
        }
    }
}, V1Patch.PatchType.MergePatch);
```

---

## PodService

**Proto fayl:** `pod.proto`
**C# implementatsiya:** `Grpc/PodGrpcService.cs`
**Manager:** `Application/PodManager.cs`

### RPC metodlar

| Metod | Request | Response | Tur |
|---|---|---|---|
| `List` | `ListPodsRequest` | `PodListReply` | Unary |
| `Get` | `PodRef` | `PodInfoReply` | Unary |
| `GetLogs` | `PodLogsRequest` | `PodLogsReply` | Unary |
| `Delete` | `DeletePodRequest` | `OperationReply` | Unary |
| `Exec` | `ExecRequest` | `stream ExecReply` | Server-streaming |

### Messageler

**`PodInfoReply`**:
```protobuf
message PodInfoReply {
  string name       = 1;
  string namespace  = 2;
  string phase      = 3;       // "Running", "Pending", "Failed"
  string node_name  = 4;
  string pod_ip     = 5;
  string start_time = 6;
  repeated ContainerStatusReply containers = 7;
}

message ContainerStatusReply {
  string name          = 1;
  bool   ready         = 2;
  int32  restart_count = 3;
  string state         = 4;    // "running", "waiting", "terminated"
}
```

**`ExecRequest`**:
```protobuf
message ExecRequest {
  string cluster        = 1;
  string namespace      = 2;
  string pod_name       = 3;
  string container_name = 4;
  repeated string command = 5;   // masalan: ["ls", "-la", "/app"]
  string requested_by   = 6;
  string correlation_id = 7;
}
```

**`ExecReply`** — streaming javob:
```protobuf
message ExecReply {
  string output    = 1;   // stdout yoki stderr satri
  bool   is_stderr = 2;   // true bo'lsa stderr
  bool   is_done   = 3;   // oxirgi chunk
  int32  exit_code = 4;   // faqat is_done=true bo'lganda ishonchli
}
```

### Exec qanday ishlaydi?

`KubernetesFacade.ExecInPodAsync` WebSocket orqali ishlaydi:

```
1. WebSocketNamespacedPodExecAsync → k8s WebSocket connection
2. StreamDemuxer.Start() → stdout va stderr channellarni ajratadi
3. GetStream(ChannelIndex.StdOut) va GetStream(ChannelIndex.StdErr)
4. Ikki stream parallel o'qiladi (ReadStreamAsync)
5. Task.WhenAll → natijalar birlashtirilib IAsyncEnumerable orqali chiqariladi
6. PodGrpcService → har bir (output, isStderr) → ExecReply stream ga yoziladi
```

`PodGrpcService.Exec`:
```csharp
public override async Task Exec(ExecRequest request, IServerStreamWriter<ExecReply> stream, ServerCallContext context)
{
    await foreach (var (output, isStderr) in _manager.ExecAsync(..., context.CancellationToken))
    {
        await stream.WriteAsync(new ExecReply {
            Output = output,
            IsStderr = isStderr
        });
    }
    await stream.WriteAsync(new ExecReply { IsDone = true, ExitCode = 0 });
}
```

**`PodLogsRequest`**:
```protobuf
message PodLogsRequest {
  string cluster        = 1;
  string namespace      = 2;
  string pod_name       = 3;
  string container_name = 4;   // bo'sh bo'lsa birinchi container
  int32  tail_lines     = 5;   // default 100
}
```

---

## NamespaceService

**Proto fayl:** `namespace.proto`
**C# implementatsiya:** `Grpc/NamespaceGrpcService.cs`
**Manager:** `Application/NamespaceManager.cs`

### RPC metodlar

| Metod | Request | Response | Tur |
|---|---|---|---|
| `List` | `ListNamespacesRequest` | `NamespaceListReply` | Unary |
| `Get` | `NamespaceRef` | `NamespaceInfoReply` | Unary |
| `Create` | `CreateNamespaceRequest` | `OperationReply` | Unary |
| `Delete` | `DeleteNamespaceRequest` | `OperationReply` | Unary |

### Messageler

```protobuf
message NamespaceInfoReply {
  string name       = 1;
  string status     = 2;       // "Active", "Terminating"
  string created_at = 3;
  map<string, string> labels = 4;
}

message CreateNamespaceRequest {
  string cluster        = 1;
  string name           = 2;
  map<string, string> labels = 3;
  string requested_by   = 4;
  string reason         = 5;
  string correlation_id = 6;
}
```

---

## ConfigMapService

**Proto fayl:** `configmap.proto`
**C# implementatsiya:** `Grpc/ConfigMapGrpcService.cs`
**Manager:** `Application/ConfigMapManager.cs`

### RPC metodlar

| Metod | Request | Response | Tur |
|---|---|---|---|
| `List` | `ListConfigMapsRequest` | `ConfigMapListReply` | Unary |
| `Get` | `ConfigMapRef` | `ConfigMapReply` | Unary |
| `Create` | `CreateConfigMapRequest` | `OperationReply` | Unary |
| `Update` | `UpdateConfigMapRequest` | `OperationReply` | Unary |
| `Delete` | `DeleteConfigMapRequest` | `OperationReply` | Unary |

### Messageler

```protobuf
message ConfigMapReply {
  string name       = 1;
  string namespace  = 2;
  map<string, string> data = 3;   // key-value ma'lumotlar
  string created_at = 4;
}

message CreateConfigMapRequest {
  string cluster        = 1;
  string namespace      = 2;
  string name           = 3;
  map<string, string> data = 4;
  string requested_by   = 5;
  string reason         = 6;
  string correlation_id = 7;
}
```

**Update vs Create farqi:** `Update` mavjud ConfigMap ni to'liq almashtiradi (`ReplaceNamespacedConfigMap`). Qisman yangilash yo'q.

---

## ClusterHealthService

**Proto fayl:** `health.proto`
**C# implementatsiya:** `Grpc/ClusterHealthGrpcService.cs`

### RPC metodlar

| Metod | Request | Response | Tur |
|---|---|---|---|
| `Ping` | `PingRequest` | `PingReply` | Unary |
| `ClusterHealth` | `ClusterHealthRequest` | `ClusterHealthReply` | Unary |
| `CanAccessNamespace` | `NamespaceAccessRequest` | `NamespaceAccessReply` | Unary |

### Messageler

```protobuf
message ClusterHealthReply {
  string cluster        = 1;
  bool   reachable      = 2;
  string server_version = 3;   // masalan: "1.28"
  string message        = 4;
}

message NamespaceAccessReply {
  bool   can_access = 1;
  string message    = 2;
}
```

`Ping` — KuberManager servisining o'zi tirik yoki yo'qligini tekshiradi.
`ClusterHealth` — berilgan cluster ga ulanish va server versiyasini tekshiradi.
`CanAccessNamespace` — Policy orqali namespace ruxsatini tekshiradi (k8s ga murojaat qilmaydi).

---

## Proto → C# Naming

Proto snake_case field lari C# da PascalCase ga aylanadi:

| Proto | C# |
|---|---|
| `requested_by` | `RequestedBy` |
| `correlation_id` | `CorrelationId` |
| `ready_replicas` | `ReadyReplicas` |
| `container_port` | `ContainerPort_` ⚠️ |
| `is_stderr` | `IsStderr` |
| `is_done` | `IsDone` |
| `exit_code` | `ExitCode` |

⚠️ `ContainerPort` — class nomi bilan to'qnashadi, shuning uchun Protobuf `ContainerPort_` (trailing underscore) deb generatsiya qiladi.
