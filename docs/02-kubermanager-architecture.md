# 02 — KuberManager: Arxitektura

## Loyiha haqida

**KuberManager** — ASP.NET Core gRPC server (.NET 9.0).
Kubernetes clusterlarini boshqarishning yagona entry point i.

```
kubernets-manager/
├── src/
│   └── KuberManager.Service/
│       ├── Application/           ← Biznes logika
│       ├── Domain/                ← Domain modellari
│       ├── Grpc/                  ← gRPC handler lar
│       ├── Infrastructure/        ← Infratuzilma
│       │   ├── Audit/
│       │   ├── Kubernetes/
│       │   ├── Options/
│       │   └── Policy/
│       ├── Protos/                ← .proto fayllar
│       └── Program.cs
└── deploy/
    ├── deployment.yaml
    └── rbac/
```

## Layerlar va mas'uliyatlar

### 1. Grpc Layer — `Grpc/*.cs`

gRPC so'rovlarini qabul qiladi. Proto message ↔ DTO konvertatsiya. Exception → RpcException mapping.

```
Grpc/
├── DeploymentGrpcService.cs
├── PodGrpcService.cs
├── NamespaceGrpcService.cs
├── ConfigMapGrpcService.cs
└── ClusterHealthGrpcService.cs
```

**Mas'uliyat:**
- Proto request → DTO ga aylantirish
- Manager ni chaqirish
- `PolicyViolationException` → `StatusCode.PermissionDenied`
- `HttpOperationException(404)` → `StatusCode.NotFound`
- Boshqa exception → `StatusCode.Internal` + log

### 2. Application Layer — `Application/*.cs`

Biznes logika. Policy va Audit ni muvofiqlashtiradi. Facade orqali k8s ga murojaat qiladi.

```
Application/
├── DeploymentManager.cs     IDeploymentManager
├── PodManager.cs            IPodManager
├── NamespaceManager.cs      INamespaceManager
├── ConfigMapManager.cs      IConfigMapManager
└── DTOs/
    ├── CreateDeploymentDto.cs
    ├── DeploymentStatusDto.cs
    ├── NamespaceInfoDto.cs
    ├── ConfigMapDto.cs
    └── PodInfoDto.cs
```

**Har bir Manager metodi:**
1. `_policy.Ensure*()` — ruxsat tekshiradi
2. `_k8s.*()` — Kubernetes API ga murojaat
3. `_audit.RecordAsync()` — operatsiyani log ga yozadi
4. `OperationResult.Ok/Fail` — natija qaytaradi

### 3. Infrastructure/Kubernetes — `Infrastructure/Kubernetes/`

Kubernetes API bilan ishlash. `KubernetesClient` (.NET) kutubxonasi orqali.

```
Infrastructure/Kubernetes/
├── IKubernetesClientFactory.cs
├── KubernetesClientFactory.cs   ← Singleton, cluster bo'yicha cache
├── IKubernetesFacade.cs
└── KubernetesFacade.cs          ← Barcha k8s operatsiyalar
```

### 4. Infrastructure/Policy — `Infrastructure/Policy/`

Operatsiyalar ustidan cheklovlar.

```
Infrastructure/Policy/
├── IOperationPolicy.cs
├── OperationPolicy.cs
└── PolicyViolationException.cs
```

**3 ta metod:**
- `EnsureNamespaceAllowed(ns)` — namespace whitelist
- `EnsureDeploymentAllowed(ns, name)` — bloklangan deployment lar
- `EnsureReplicaLimit(replicas)` — maksimal replica soni

### 5. Infrastructure/Audit — `Infrastructure/Audit/`

Har bir mutation operatsiyasini yozib boradi.

```
Infrastructure/Audit/
├── IAuditService.cs
├── LogAuditService.cs   ← Serilog structured log
└── AuditEntry.cs
```

### 6. Domain Layer — `Domain/`

```
Domain/
├── OperationResult.cs   ← Ok(correlationId) / Fail(correlationId, error)
└── ResourceRef.cs       ← cluster + namespace + name
```

## DI Registration (Program.cs)

```csharp
// Infrastructure
builder.Services.AddSingleton<IKubernetesClientFactory, KubernetesClientFactory>();
builder.Services.AddSingleton<IKubernetesFacadeFactory, KubernetesFacadeFactory>();
builder.Services.AddSingleton<IOperationPolicy, OperationPolicy>();
builder.Services.AddSingleton<IAuditService, LogAuditService>();

// Application
builder.Services.AddScoped<IDeploymentManager, DeploymentManager>();
builder.Services.AddScoped<IPodManager, PodManager>();
builder.Services.AddScoped<INamespaceManager, NamespaceManager>();
builder.Services.AddScoped<IConfigMapManager, ConfigMapManager>();

// gRPC
builder.Services.AddGrpc();
builder.Services.AddGrpcHealthChecks();
```

## Nima Singleton, nima Scoped?

| Servis | Lifetime | Sabab |
|---|---|---|
| `KubernetesClientFactory` | Singleton | Cluster client lar cached, bir marta yaratiladi |
| `OperationPolicy` | Singleton | Config dan o'qiladi, o'zgarmaydi |
| `AuditService` | Singleton | Stateless logger wrapper |
| `KubernetesFacadeFactory` | Singleton | Turli xil logikalar uchun facade yaratuvchi factory (har bir klaster bo'yicha) |
| `*Manager` | Scoped | FacadeFactory yordamida `IKubernetesFacade` yaratib, k8s bilan ishlaydi |
