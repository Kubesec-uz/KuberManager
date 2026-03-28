# 12 — Paketlar va Bog'liqliklar

## KuberManager.Service (.csproj)

```xml
<TargetFramework>net9.0</TargetFramework>
```

### gRPC

| Paket | Versiya | Vazifa |
|---|---|---|
| `Grpc.AspNetCore` | 2.76.0 | ASP.NET Core gRPC server |
| `Grpc.AspNetCore.HealthChecks` | 2.76.0 | gRPC health check service |
| `Grpc.Tools` | 2.78.0 | Proto → C# kod generatsiya (build vaqtida) |
| `Google.Protobuf` | 3.34.1 | Protobuf runtime |

### Kubernetes

| Paket | Versiya | Vazifa |
|---|---|---|
| `KubernetesClient` | 19.0.2 | Rasmiy k8s .NET client |

Bu paket ichida:
- `k8s.IKubernetes` — API client interface
- `k8s.Models.*` — `V1Deployment`, `V1Pod`, `V1Namespace`, `V1ConfigMap` va boshqalar
- `k8s.StreamDemuxer` — WebSocket exec stream demultiplexer
- `k8s.Autorest.HttpOperationException` — HTTP xatolari uchun

### Logging

| Paket | Versiya | Vazifa |
|---|---|---|
| `Serilog.AspNetCore` | 10.0.0 | Structured logging |

### Framework

| Paket | Versiya | Vazifa |
|---|---|---|
| `Microsoft.Extensions.Logging` | 10.0.5 | Logging abstraksiya |
| `Microsoft.AspNetCore.Diagnostics.HealthChecks` | (implicit) | `/healthz` endpoint |

---

## Kubesec.Auth (.csproj)

```xml
<TargetFramework>net9.0</TargetFramework>
```

### gRPC Client

| Paket | Versiya | Vazifa |
|---|---|---|
| `Grpc.Net.Client` | 2.76.0 | gRPC HTTP/2 client |
| `Grpc.Tools` | 2.78.0 | Proto → C# client stub generatsiya |
| `Google.Protobuf` | 3.34.1 | Protobuf runtime |

### Authentication & Security

| Paket | Versiya | Vazifa |
|---|---|---|
| `Microsoft.AspNetCore.Authentication.JwtBearer` | 9.0.8 | JWT Bearer autentifikatsiya |
| `Microsoft.AspNetCore.Authentication.Google` | 9.0.9 | Google OAuth2 |
| `Microsoft.AspNetCore.Authentication.Cookies` | 2.3.0 | Cookie asosida auth |
| `Microsoft.AspNetCore.Authentication` | 2.3.0 | Auth abstraksiyalar |
| `System.Security.Claims` | 4.3.0 | Claims va identity |

### Caching & Session

| Paket | Versiya | Vazifa |
|---|---|---|
| `StackExchange.Redis` | 2.9.24 | Redis asosiy client |
| `Microsoft.Extensions.Caching.StackExchangeRedis` | 10.0.1 | IDistributedCache → Redis |
| `Microsoft.AspNetCore.DataProtection.StackExchangeRedis` | 9.0.9 | Data protection key lar Redis da |

### Database

| Paket | Versiya | Vazifa |
|---|---|---|
| `Microsoft.EntityFrameworkCore.Design` | 9.0.8 | EF migration tool (build vaqtida) |

### API & Mapping

| Paket | Versiya | Vazifa |
|---|---|---|
| `AutoMapper` | 14.0.0 | Entity ↔ DTO mapping |
| `Swashbuckle.AspNetCore` | 9.0.3 | Swagger UI |
| `Microsoft.AspNetCore.OpenApi` | 9.0.4 | OpenAPI generatsiya |

### Infrastructure

| Paket | Versiya | Vazifa |
|---|---|---|
| `Docker.DotNet` | 3.125.15 | Docker API client (Swarm) |
| `Serilog.AspNetCore` | 9.0.0 | Structured logging |
| `Wangkanai.Detection` | 8.20.0 | Browser/device detection |

---

## Kubesec.Auth.Bot (.csproj)

| Paket | Versiya | Vazifa |
|---|---|---|
| `Telegram.Bot` | ~ | Telegram Bot API |
| `Microsoft.EntityFrameworkCore.*` | 9.x | Bot DB uchun EF Core |

---

## Proto fayl import chain

```
KuberManager build vaqtida:
  common.proto       → OperationReply.cs
  deployment.proto   → DeploymentService.cs, DeploymentServiceBase.cs, ...
  pod.proto          → PodService.cs, PodServiceBase.cs, ...
  namespace.proto    → NamespaceService.cs, ...
  configmap.proto    → ConfigMapService.cs, ...
  health.proto       → ClusterHealthService.cs, ...

Kubesec.Auth build vaqtida (GrpcServices="Client"):
  common.proto       → OperationReply.cs (faqat message)
  deployment.proto   → DeploymentService.DeploymentServiceClient.cs
  pod.proto          → PodService.PodServiceClient.cs
  namespace.proto    → NamespaceService.NamespaceServiceClient.cs
  configmap.proto    → ConfigMapService.ConfigMapServiceClient.cs
  health.proto       → ClusterHealthService.ClusterHealthServiceClient.cs
```

---

## .NET version

Ikki loyiha ham **.NET 9.0** — oxirgi LTS emas, lekin joriy release.
Nullable reference types yoqilgan (`<Nullable>enable</Nullable>`).
Implicit usings yoqilgan (`<ImplicitUsings>enable</ImplicitUsings>`).
