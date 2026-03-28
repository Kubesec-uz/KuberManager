# 08 — REST API Endpoints (ClusterController)

Base route: `api/cluster`

## DB CRUD (Cluster Registry)

Kubernetes cluster lar ma'lumotlar bazasida saqlanadi. Bu endpoint lar cluster ni ro'yxatga olish uchun.

| Method | Route | Permission | Vazifa |
|---|---|---|---|
| `GET` | `/` | Cluster_Get | Barcha cluster larni olish |
| `GET` | `/{id:guid}` | Cluster_Get | ID bo'yicha bitta cluster |
| `POST` | `/` | Cluster_Create | Yangi cluster qo'shish |
| `PATCH` | `/{id:guid}` | Cluster_Update | Cluster ma'lumotlarini yangilash |
| `DELETE` | `/` | Cluster_Delete | Cluster o'chirish |

---

## Deployments

### List Deployments
```
GET api/cluster/{cluster}/namespaces/{ns}/deployments
Permission: Cluster_Get
Response: DeploymentStatusReply[]
```

### Get Deployment Status
```
GET api/cluster/{cluster}/namespaces/{ns}/deployments/{name}
Permission: Cluster_Get
Response: DeploymentStatusReply
  {
    "name": "api-server",
    "namespace": "app-prod",
    "desiredReplicas": 3,
    "readyReplicas": 3,
    "availableReplicas": 3,
    "strategy": "RollingUpdate",
    "status": "Available"
  }
```

### Create Deployment
```
POST api/cluster/{cluster}/namespaces/{ns}/deployments
Permission: Cluster_Create
Body: CreateDeploymentRequest (proto-generated)
  {
    "name": "my-app",
    "image": "my-registry/my-app:v1.0",
    "replicas": 2,
    "labels": { "app": "my-app", "env": "prod" },
    "envVars": { "DB_HOST": "postgres", "PORT": "8080" },
    "ports": [
      { "name": "http", "containerPort": 8080, "protocol": "TCP" }
    ],
    "reason": "Initial deployment"
  }
  // requestedBy va correlationId controller da to'ldiriladi
Response: OperationReply
```

### Delete Deployment
```
DELETE api/cluster/{cluster}/namespaces/{ns}/deployments/{name}
Permission: Cluster_Delete
Body: { "reason": "Service deprecated" }
Response: OperationReply
```

### Scale Deployment
```
POST api/cluster/{cluster}/namespaces/{ns}/deployments/{name}/scale
Permission: Cluster_Update
Body: { "replicas": 5, "reason": "Traffic spike" }
Response: OperationReply
```

### Restart Deployment
```
POST api/cluster/{cluster}/namespaces/{ns}/deployments/{name}/restart
Permission: Cluster_Update
Body: { "reason": "Config change" }   ← ixtiyoriy
Response: OperationReply
```

---

## Pods

### List Pods
```
GET api/cluster/{cluster}/namespaces/{ns}/pods?app={appLabel}
Permission: Cluster_Get
Query: app — ixtiyoriy, "app={value}" label selector
Response: PodInfoReply[]
```

### Get Pod
```
GET api/cluster/{cluster}/namespaces/{ns}/pods/{podName}
Permission: Cluster_Get
Response: PodInfoReply
  {
    "name": "api-server-abc123",
    "namespace": "app-prod",
    "phase": "Running",
    "nodeName": "worker-1",
    "podIp": "10.0.0.5",
    "startTime": "2026-03-29T08:00:00Z",
    "containers": [
      { "name": "api", "ready": true, "restartCount": 0, "state": "running" }
    ]
  }
```

### Get Pod Logs
```
GET api/cluster/{cluster}/namespaces/{ns}/pods/{podName}/logs?container={name}&tail={n}
Permission: Cluster_Get
Query:
  container — ixtiyoriy, birinchi container default
  tail      — ixtiyoriy, default 100
Response: PodLogsReply
  {
    "podName": "api-server-abc123",
    "content": "2026-03-29 INFO Server started on port 8080\n..."
  }
```

### Delete Pod
```
DELETE api/cluster/{cluster}/namespaces/{ns}/pods/{podName}
Permission: Cluster_Delete
Body: { "reason": "Pod stuck" }
Response: OperationReply
```

### Exec in Pod
```
POST api/cluster/{cluster}/namespaces/{ns}/pods/{podName}/exec
Permission: Cluster_Update
Content-Type: application/json
Body:
  {
    "container": "app",
    "command": ["ls", "-la", "/app"]
  }

Response: text/plain (streaming)
  drwxr-xr-x 2 app app 4096 Mar 29 10:00 .
  drwxr-xr-x 8 app app 4096 Mar 29 09:00 ..
  -rw-r--r-- 1 app app 1234 Mar 29 09:30 app.dll

Headers:
  X-Accel-Buffering: no   ← nginx buffering o'chirilgan
```

Output real-time stream bo'lib keladi. Har bir `flush` dan keyin mijoz yangi qatorlarni oladi.

---

## Namespaces

### List Namespaces
```
GET api/cluster/{cluster}/namespaces
Permission: Cluster_Get
Response: NamespaceInfoReply[]
  [
    { "name": "app-prod", "status": "Active", "createdAt": "...", "labels": {...} },
    { "name": "app-stage", "status": "Active", "createdAt": "...", "labels": {...} }
  ]
```

### Get Namespace
```
GET api/cluster/{cluster}/namespaces/{name}
Permission: Cluster_Get
Response: NamespaceInfoReply
```

### Create Namespace
```
POST api/cluster/{cluster}/namespaces
Permission: Cluster_Create
Body:
  {
    "name": "app-testing",
    "labels": { "env": "testing", "team": "backend" },
    "reason": "New environment for QA"
  }
Response: OperationReply
```

### Delete Namespace
```
DELETE api/cluster/{cluster}/namespaces/{name}
Permission: Cluster_Delete
Body: { "reason": "Environment decommissioned" }
Response: OperationReply

⚠️ DIQQAT: Namespace o'chirilsa ichidagi BARCHA resurslar yo'q bo'ladi.
```

---

## ConfigMaps

### List ConfigMaps
```
GET api/cluster/{cluster}/namespaces/{ns}/configmaps
Permission: Cluster_Get
Response: ConfigMapReply[]
```

### Get ConfigMap
```
GET api/cluster/{cluster}/namespaces/{ns}/configmaps/{name}
Permission: Cluster_Get
Response: ConfigMapReply
  {
    "name": "app-config",
    "namespace": "app-prod",
    "data": {
      "DATABASE_URL": "postgres://...",
      "CACHE_TTL": "300",
      "LOG_LEVEL": "info"
    },
    "createdAt": "2026-01-15T10:00:00Z"
  }
```

### Create ConfigMap
```
POST api/cluster/{cluster}/namespaces/{ns}/configmaps
Permission: Cluster_Create
Body:
  {
    "name": "app-config",
    "data": {
      "key1": "value1",
      "key2": "value2"
    },
    "reason": "Initial config"
  }
Response: OperationReply
```

### Update ConfigMap
```
PUT api/cluster/{cluster}/namespaces/{ns}/configmaps/{name}
Permission: Cluster_Update
Body:
  {
    "name": "app-config",
    "data": {
      "key1": "new-value1",
      "key2": "value2",
      "key3": "value3"
    },
    "reason": "Config update"
  }
Response: OperationReply

⚠️ To'liq almashtirish (Replace). Avvalgi barcha keylar o'chadi, yangilari yoziladi.
```

### Delete ConfigMap
```
DELETE api/cluster/{cluster}/namespaces/{ns}/configmaps/{name}
Permission: Cluster_Delete
Body: { "reason": "Cleanup" }
Response: OperationReply
```

---

## Health

### Cluster Health
```
GET api/cluster/{cluster}/health
Permission: Cluster_Get
Response: ClusterHealthReply
  {
    "cluster": "prod-1",
    "reachable": true,
    "serverVersion": "1.28",
    "message": ""
  }
```

---

## Xato Response lari

| gRPC StatusCode | HTTP Status | Holat |
|---|---|---|
| `NotFound` | 404 | Resurs topilmadi |
| `PermissionDenied` | 403 | Policy yoki RBAC xatosi |
| `InvalidArgument` | 400 | Noto'g'ri parametr |
| Boshqa | 502 | KuberManager dan xatolik |

```json
// 404 misoli:
"Deployment 'my-app' not found."

// 403 misoli:
"Access to namespace 'kube-system' is not allowed."
```

---

## Request Record lar (Controller da ishlatiladi)

```csharp
// Scale uchun:
public sealed record DeploymentScaleRequest(int Replicas, string? Reason);

// Faqat reason talab qiladigan operatsiyalar uchun:
public sealed record ReasonRequest(string? Reason);

// Namespace yaratish:
public sealed record CreateNamespaceRequest(string Name, Dictionary<string, string>? Labels, string? Reason);

// ConfigMap yaratish/yangilash:
public sealed record ConfigMapWriteRequest(string Name, Dictionary<string, string> Data, string? Reason);

// Pod exec:
public sealed record ExecRequest(string Container, IEnumerable<string> Command);
```
