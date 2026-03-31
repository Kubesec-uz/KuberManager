# 01 — Platform Overview

## Nima bu?

Kubesec — Kubernetes muhitlarini boshqarish va xavfsizligini ta'minlash uchun qurilgan platforma.

Ikki asosiy backend servisi bor:

| Servis | Turi | Port | Rol |
|---|---|---|---|
| **KuberManager** | gRPC microservice | 5001 | Kubernetes cluster operatsiyalari |
| **Kubesec.Auth** | REST API | 4001 | Auth, RBAC, ta'lim platformasi, API gateway |

## Umumiy arxitektura

```
Brauzer / Mobil ilova
        │
        ▼
┌─────────────────────────────┐
│   Kubesec.Auth REST API     │  port 4001
│                             │
│  JWT / Cookie auth          │
│  RBAC permission check      │
│  Rate limiting / DDoS       │
└────────────┬────────────────┘
             │
             │  gRPC (port 5001)
             ▼
┌─────────────────────────────┐
│   KuberManager gRPC Service │  port 5001
│                             │
│  Policy enforcement         │
│  Audit logging              │
│  Multi-cluster management   │
└────────────┬────────────────┘
             │
             │  Kubernetes API
             ▼
     ┌───────┴────────┐
     │                │
  cluster-1        cluster-2  ...
```

## Asosiy qoidalar

1. `Kubesec.Auth` hech qachon Kubernetes API ga to'g'ridan-to'g'ri murojaat qilmaydi
2. Barcha cluster operatsiyalari `KuberManager` orqali o'tadi
3. `KuberManager` har bir operatsiyani policy tekshiruvidan o'tkazadi
4. Har bir mutation (yaratish, o'chirish, scale) audit logga yoziladi
5. gRPC xatosi → REST `HandleRpcError` → HTTP status code

## Xatolar mapping

| gRPC StatusCode | HTTP |
|---|---|
| `NotFound` | 404 |
| `PermissionDenied` | 403 |
| `InvalidArgument` | 400 |
| Boshqa | 502 |

## Request hayot davri (misol: Scale)

```
1. POST /api/cluster/prod/namespaces/production/api-server/scale
   { "replicas": 5, "reason": "Traffic spike" }

2. TokenValidationMiddleware → JWT dan userId olish

3. ClusterController.ScaleDeploymentAsync
   → HasPermission("Cluster_Update") tekshirish

4. _kube.ScaleDeploymentAsync(cluster, ns, name, replicas, userId, reason, correlationId)
   → KubeManagerClient gRPC call

5. KuberManager: DeploymentGrpcService.Scale
   → DeploymentManager.ScaleAsync
   → OperationPolicy.EnsureNamespaceAllowed("production")
   → OperationPolicy.EnsureDeploymentAllowed("production", "api-server")
   → OperationPolicy.EnsureReplicaLimit(5)
   → KubernetesFacadeFactory.For(cluster).PatchDeploymentReplicasAsync
   → k8s API: PATCH /apis/apps/v1/namespaces/production/deployments/api-server/scale
   → AuditService.RecordAsync(action: "ScaleDeployment", success: true)

6. OperationReply { Success: true, Message: "...", CorrelationId: "abc-123" }
   → HTTP 200 { success: true, correlationId: "abc-123" }
```
