# Kubesec Platform — Full Infrastructure Documentation

## Overview

Kubesec is a Kubernetes security and learning platform built on two core backend services:

| Service | Type | Port | Role |
|---|---|---|---|
| **KuberManager** | gRPC microservice | 5001 | Kubernetes resource management |
| **Kubesec.Auth** | REST API | 4001 | Auth, RBAC, learning platform, and API gateway |

They communicate over gRPC. `Kubesec.Auth` never talks to Kubernetes directly — all cluster operations go through `KuberManager`.

---

## Architecture Diagram

```
Browser / Mobile
      │
      ▼
Kubesec.Auth REST API (port 4001)
      │  JWT / Cookie auth
      │  Permission check (RBAC)
      │
      │ gRPC (port 5001)
      ▼
KuberManager gRPC Service
      │  Policy enforcement
      │  Audit log
      │
      ▼
Kubernetes Clusters (via k8s API)
  cluster-1  cluster-2  cluster-3 ...
```

---

## Repository: KuberManager

> **Path:** `kubernets-manager/`
> **Type:** ASP.NET Core gRPC Server (.NET 9.0)
> **Port:** 5001

### Purpose

KuberManager is the secure gateway to Kubernetes. It:
- Exposes gRPC endpoints for Deployments, Pods, Namespaces, ConfigMaps, and Cluster Health
- Enforces operation policies (namespace whitelist, replica limits)
- Writes structured audit logs for every mutation
- Manages connections to multiple Kubernetes clusters from a single instance

### Project Structure

```
kubernets-manager/
├── src/
│   └── KuberManager.Service/
│       ├── Application/           ← Business logic (Managers + DTOs)
│       ├── Domain/                ← Domain models (OperationResult, ResourceRef)
│       ├── Grpc/                  ← gRPC service handlers
│       ├── Infrastructure/
│       │   ├── Audit/             ← Audit logging
│       │   ├── Kubernetes/        ← k8s client factory + facade
│       │   ├── Options/           ← Config binding
│       │   └── Policy/            ← Operation policy guard
│       ├── Protos/                ← .proto contract files
│       └── Program.cs
└── deploy/
    ├── deployment.yaml            ← K8s Deployment + Service + NetworkPolicy
    └── rbac/
        ├── serviceaccount.yaml
        ├── role.yaml
        └── rolebinding.yaml
```

### Layer Responsibilities

| Layer | Files | Responsibility |
|---|---|---|
| **Grpc** | `*GrpcService.cs` | Receive gRPC calls, map proto ↔ DTO, catch errors |
| **Application** | `*Manager.cs` | Business logic, coordinate policy + facade |
| **Infrastructure/Kubernetes** | `KubernetesFacade.cs` | Call k8s API via `KubernetesClient` library |
| **Infrastructure/Policy** | `OperationPolicy.cs` | Enforce namespace whitelist, replica limits |
| **Infrastructure/Audit** | `LogAuditService.cs` | Write structured audit log entries |
| **Domain** | `OperationResult.cs` | Shared result wrapper |

### gRPC Services

#### DeploymentService
| Method | Type | Description |
|---|---|---|
| `GetStatus` | Unary | Get deployment status and replica counts |
| `List` | Unary | List all deployments in a namespace |
| `Create` | Unary | Create a new deployment |
| `Delete` | Unary | Delete a deployment |
| `Scale` | Unary | Scale replicas up/down |
| `Restart` | Unary | Rolling restart (patch annotation) |

#### PodService
| Method | Type | Description |
|---|---|---|
| `List` | Unary | List pods, optional app label filter |
| `Get` | Unary | Get single pod info |
| `GetLogs` | Unary | Fetch pod logs (tail N lines) |
| `Delete` | Unary | Delete/evict a pod |
| `Exec` | Server-streaming | Execute command inside container, stream output |

#### NamespaceService
| Method | Type | Description |
|---|---|---|
| `List` | Unary | List all namespaces |
| `Get` | Unary | Get namespace details |
| `Create` | Unary | Create namespace with labels |
| `Delete` | Unary | Delete namespace |

#### ConfigMapService
| Method | Type | Description |
|---|---|---|
| `List` | Unary | List all configmaps in namespace |
| `Get` | Unary | Get configmap data |
| `Create` | Unary | Create configmap with key-value data |
| `Update` | Unary | Replace configmap data |
| `Delete` | Unary | Delete configmap |

#### ClusterHealthService
| Method | Type | Description |
|---|---|---|
| `Ping` | Unary | Check if cluster is reachable |
| `ClusterHealth` | Unary | Get node/pod health summary |
| `CanAccessNamespace` | Unary | Check access permission |

### Proto Files

All in `src/KuberManager.Service/Protos/`:

| File | Contains |
|---|---|
| `common.proto` | `OperationReply` (shared by all services) |
| `deployment.proto` | `DeploymentService`, `DeploymentStatusReply`, `CreateDeploymentRequest`, etc. |
| `pod.proto` | `PodService`, `PodInfoReply`, `ExecRequest`, `ExecReply`, etc. |
| `namespace.proto` | `NamespaceService`, `NamespaceInfoReply`, etc. |
| `configmap.proto` | `ConfigMapService`, `ConfigMapReply`, etc. |
| `health.proto` | `ClusterHealthService`, `ClusterHealthReply`, etc. |

### Kubernetes Connection

`KubernetesClientFactory` builds a `IKubernetes` client per cluster name:

```
cluster name → config resolution order:
  1. KubernetesOptions.Clusters[name].KubeconfigPath   (explicit path)
  2. InClusterConfig                                    (if running inside k8s)
  3. Default (~/.kube/config)                          (local dev)
```

The factory caches clients as singletons per cluster name.

### Policy Enforcement

`OperationPolicy` reads `PolicyOptions` from config:

```json
{
  "Policy": {
    "AllowedNamespaces": ["default", "staging", "production"],
    "MaxReplicas": 20
  }
}
```

Any request violating these rules throws `PolicyViolationException` → gRPC `PermissionDenied`.

### Audit Logging

Every mutating operation (Create, Delete, Scale, Restart, Exec) writes an `AuditEntry`:

```json
{
  "Action": "Scale",
  "Cluster": "prod-1",
  "Namespace": "production",
  "Resource": "api-server",
  "RequestedBy": "user-uuid",
  "Reason": "Manual scale",
  "CorrelationId": "abc-123",
  "Timestamp": "2026-03-29T10:00:00Z"
}
```

Currently backed by `LogAuditService` (Serilog structured log). Can be swapped for DB or event bus.

### Kubernetes RBAC

The service runs with a dedicated `ServiceAccount`. Required permissions (in `deploy/rbac/role.yaml`):

```
Verbs: get, list, watch, create, update, patch, delete
Resources: deployments, pods, pods/log, pods/exec, namespaces, configmaps
```

### Configuration

`appsettings.json`:
```json
{
  "Kubernetes": {
    "Clusters": {
      "prod-1": { "KubeconfigPath": "/etc/kubeconfig/prod-1" },
      "staging": { "KubeconfigPath": "/etc/kubeconfig/staging" }
    }
  },
  "Policy": {
    "AllowedNamespaces": ["default", "staging"],
    "MaxReplicas": 10
  }
}
```

`appsettings.Development.json`:
```json
{
  "Kubernetes": {
    "Clusters": {}
  }
}
```

### Kubernetes Deployment

`deploy/deployment.yaml` creates:
- **Deployment** — 1 replica, port 5001, resource limits: `500m` CPU / `256Mi` memory
- **Service** — ClusterIP, port 5001
- **NetworkPolicy** — ingress only from `api` namespace (where Kubesec.Auth runs)

---

## Repository: Kubesec.Auth

> **Path:** `kubesec-backend/`
> **Type:** ASP.NET Core REST API (.NET 9.0)
> **Port:** 4001

### Purpose

Kubesec.Auth is the main platform backend. It handles:
- JWT and Google OAuth authentication
- RBAC permission model (Roles → Permissions → Actions)
- Learning paths, labs, exams, quizzes, courses, articles
- Kubernetes cluster registry and management (via KuberManager gRPC)
- Docker Swarm service management
- Telegram bot verification
- Redis caching, DDoS/rate limiting protection
- Audit logging

### Solution Projects

| Project | Role |
|---|---|
| `Kubesec.Auth` | REST API layer — controllers, middleware, DI setup |
| `Kubesec.Auth.Service` | Business logic — services, DTOs, mappers |
| `Kubesec.Auth.Domain` | Domain entities and enums |
| `Kubesec.Auth.DataAccess` | EF Core DbContext, repositories, migrations |
| `Kubesec.Auth.Tests` | Unit and integration tests |
| `Kubesec.Auth.Bot` | Independent Telegram bot service |

### Project Structure

```
kubesec-backend/
├── Kubesec.Auth/
│   ├── Controllers/           ← 27 REST controllers
│   ├── BackgroundServices/    ← ActivitySync, RateLimitCleanup
│   ├── Middleware/            ← RateLimit, DDoS, TokenValidation, ActivityTracking
│   ├── Extensions/            ← ServiceExtension (DI registration), GoogleAuth
│   ├── KubeManager/           ← gRPC client wrapper
│   │   ├── IKubeManagerClient.cs
│   │   ├── KubeManagerClient.cs
│   │   └── KubeManagerOptions.cs
│   ├── Protos/                ← Proto files (client-side, GrpcServices="Client")
│   └── Program.cs
│
├── Kubesec.Auth.Service/
│   ├── IServices/             ← 37 service interfaces
│   ├── Services/              ← 28+ service implementations
│   ├── DTOs/                  ← 26 DTO subdirectories
│   ├── Mappers/               ← AutoMapper profiles
│   ├── Validators/
│   ├── Helpers/
│   ├── Seeders/
│   └── WebSocket/
│
├── Kubesec.Auth.Domain/
│   └── Entities/              ← 57 entity classes across 15 categories
│
├── Kubesec.Auth.DataAccess/
│   ├── AppDbContexts/
│   ├── Repository/
│   └── Migrations/
│
└── Kubesec.Auth.Bot/
    ├── Handlers/
    ├── Services/
    └── Data/
```

### REST API Endpoints (ClusterController)

Base route: `api/cluster`

#### Cluster Registry (Database CRUD)
| Method | Route | Permission |
|---|---|---|
| `GET` | `/` | Cluster_Get |
| `GET` | `/{id}` | Cluster_Get |
| `POST` | `/` | Cluster_Create |
| `PATCH` | `/{id}` | Cluster_Update |
| `DELETE` | `/` | Cluster_Delete |

#### Deployments
| Method | Route | Permission |
|---|---|---|
| `GET` | `/{cluster}/namespaces/{ns}/deployments` | Cluster_Get |
| `GET` | `/{cluster}/namespaces/{ns}/deployments/{name}` | Cluster_Get |
| `POST` | `/{cluster}/namespaces/{ns}/deployments` | Cluster_Create |
| `DELETE` | `/{cluster}/namespaces/{ns}/deployments/{name}` | Cluster_Delete |
| `POST` | `/{cluster}/namespaces/{ns}/deployments/{name}/scale` | Cluster_Update |
| `POST` | `/{cluster}/namespaces/{ns}/deployments/{name}/restart` | Cluster_Update |

#### Pods
| Method | Route | Permission |
|---|---|---|
| `GET` | `/{cluster}/namespaces/{ns}/pods` | Cluster_Get |
| `GET` | `/{cluster}/namespaces/{ns}/pods/{podName}` | Cluster_Get |
| `GET` | `/{cluster}/namespaces/{ns}/pods/{podName}/logs` | Cluster_Get |
| `DELETE` | `/{cluster}/namespaces/{ns}/pods/{podName}` | Cluster_Delete |
| `POST` | `/{cluster}/namespaces/{ns}/pods/{podName}/exec` | Cluster_Update |

#### Namespaces
| Method | Route | Permission |
|---|---|---|
| `GET` | `/{cluster}/namespaces` | Cluster_Get |
| `GET` | `/{cluster}/namespaces/{name}` | Cluster_Get |
| `POST` | `/{cluster}/namespaces` | Cluster_Create |
| `DELETE` | `/{cluster}/namespaces/{name}` | Cluster_Delete |

#### ConfigMaps
| Method | Route | Permission |
|---|---|---|
| `GET` | `/{cluster}/namespaces/{ns}/configmaps` | Cluster_Get |
| `GET` | `/{cluster}/namespaces/{ns}/configmaps/{name}` | Cluster_Get |
| `POST` | `/{cluster}/namespaces/{ns}/configmaps` | Cluster_Create |
| `PUT` | `/{cluster}/namespaces/{ns}/configmaps/{name}` | Cluster_Update |
| `DELETE` | `/{cluster}/namespaces/{ns}/configmaps/{name}` | Cluster_Delete |

#### Health
| Method | Route | Permission |
|---|---|---|
| `GET` | `/{cluster}/health` | Cluster_Get |

### Exec Endpoint

`POST /{cluster}/namespaces/{ns}/pods/{podName}/exec`

Streams command output as `text/plain` with `X-Accel-Buffering: no`.
Request body:
```json
{
  "container": "app",
  "command": ["ls", "-la", "/app"]
}
```
Output is streamed chunk by chunk until the command completes.

### KubeManager gRPC Client

`KubeManagerClient` wraps all gRPC calls. Registered as `IKubeManagerClient` singleton.

Config:
```json
{
  "KubeManager": {
    "Address": "http://kubermanager.platform.svc.cluster.local:5001"
  }
}
```

Development:
```json
{
  "KubeManager": {
    "Address": "http://localhost:5001"
  }
}
```

### Domain Entities (57 total)

| Category | Entities |
|---|---|
| Users | AppUser, UserProfile, UserActivity, UserFollow |
| Auth | Session, OauthAccount, Role, Permission |
| Labs | LabTemplate, LabInstance, LabEvent, LabAccessRule, LabUserAction, LabTestResult |
| Paths | Path, PathStep, PathEnrollment, PathStepProgress, PathAccessRule, PathStepAccessRule, PathStepLabTemplate |
| Exams | Exam, ExamQuestion, ExamAttempt, ExamSchedule, ExamQuestionAnswer |
| Quizzes | Quiz, QuizQuestion, QuizAttempt, QuizQuestionAnswer + 1 more |
| Courses | Course, CourseModule, CourseEnrollment, Lesson, LessonProgress + 1 more |
| Articles | Article, ArticleComment, ArticleLike |
| Coins | UserCoin, PathCoinReward, LabCoinReward |
| Groups | StudentGroup, StudentGroupMember |
| Servers | 2 server management entities |
| Security | IpBlacklist, RateLimitLog |
| Notifications | 2 notification entities |
| Bot | 2 bot-related entities |
| Docs | 3 documentation entities |

### Middleware Pipeline

| Middleware | Function |
|---|---|
| `DDoSProtectionMiddleware` | Block IPs exceeding burst thresholds |
| `RateLimitMiddleware` | Per-user/IP rate limiting with Redis |
| `TokenValidationMiddleware` | JWT validation and claims extraction |
| `ActivityTrackingMiddleware` | Record user activity for analytics |
| `ExceptionHandlerMiddleware` | Global exception → structured error response |

### Background Services

| Service | Schedule | Purpose |
|---|---|---|
| `ActivitySyncBackgroundService` | Periodic | Flush in-memory activity buffer to DB |
| `RateLimitCleanupService` | Periodic | Purge expired rate limit records from Redis |
| `ExamSchedulerBackgroundService` | Periodic | Auto-start/end scheduled exams |
| `LabCleanupBackgroundService` | Periodic | Terminate expired lab instances |
| `ServiceCleanupBackgroundService` | Periodic | General resource cleanup |

### Key Dependencies

| Package | Version | Use |
|---|---|---|
| AutoMapper | 14.0.0 | Entity ↔ DTO mapping |
| Grpc.Net.Client | 2.76.0 | gRPC calls to KuberManager |
| Swashbuckle.AspNetCore | 9.0.3 | Swagger UI |
| JwtBearer | 9.0.8 | JWT authentication |
| Authentication.Google | 9.0.9 | Google OAuth |
| StackExchange.Redis | 2.9.24 | Caching, rate limiting, data protection keys |
| Docker.DotNet | 3.125.15 | Docker Swarm management |
| Serilog.AspNetCore | 9.0.0 | Structured logging |
| Wangkanai.Detection | 8.20.0 | Browser/device detection |
| EF Core | 9.0.8 | PostgreSQL ORM |

### Telegram Bot (Kubesec.Auth.Bot)

Independent service. Responsibilities:
- Verify user accounts via Telegram messages
- Send notifications (lab started/stopped, exam reminders)
- Handle bot commands

Runs as a separate Docker container with its own database.

---

## Docker & Deployment

### Docker Images

| Image | Dockerfile | Base | Port |
|---|---|---|---|
| `kubesec-auth` | `kubesec-backend/Dockerfile` | .NET 9.0 Alpine (multi-stage) | 4001 |
| `kubesec-bot` | `kubesec-backend/Kubesec.Auth.Bot/Dockerfile.bot` | .NET 9.0 Alpine | — |
| `kubermanager` | (in kubernets-manager) | .NET 9.0 Alpine | 5001 |

### Docker Compose Files

| File | Purpose |
|---|---|
| `docker-compose.yml` | Base configuration |
| `docker-compose.local.yml` | Local development (hot-reload, local DB) |
| `docker-compose.prod.yml` | Production (resource limits, restart policies) |

### CI/CD

`.github/workflows/deploy.yml` — GitHub Actions pipeline:
1. Build Docker image
2. Push to container registry
3. Deploy to Kubernetes cluster

### Kubernetes Manifests

Located in `kubernets-manager/deploy/`:

```
deploy/
├── deployment.yaml     ← Deployment + Service + NetworkPolicy for KuberManager
└── rbac/
    ├── serviceaccount.yaml
    ├── role.yaml         ← ClusterRole: get/list/watch/create/update/patch/delete
    └── rolebinding.yaml
```

KuberManager is deployed in the `platform` namespace. The NetworkPolicy restricts inbound connections to only the `api` namespace where Kubesec.Auth runs.

---

## Request Flow Example — Scale a Deployment

```
User → POST /api/cluster/prod-1/namespaces/production/api-server/scale
         { "replicas": 5, "reason": "Traffic spike" }

Kubesec.Auth:
  1. TokenValidationMiddleware → extract userId from JWT
  2. ClusterController.ScaleDeploymentAsync
  3. HasPermission("Cluster_Update") check
  4. _kube.ScaleDeploymentAsync(cluster, ns, name, replicas, userId, reason, correlationId)

KubeManagerClient (gRPC):
  5. _deployment.ScaleAsync(ScaleDeploymentRequest { ... })

KuberManager:
  6. DeploymentGrpcService.Scale
  7. DeploymentManager.ScaleAsync
  8. OperationPolicy.Enforce → check namespace whitelist + replica limit
  9. KubernetesFacade.ScaleDeploymentAsync
 10. k8s API → PATCH /apis/apps/v1/namespaces/production/deployments/api-server/scale
 11. AuditService.Log(action: "Scale", ...)

Response flows back:
  OperationReply { Success: true, Message: "Scaled to 5", CorrelationId: "..." }
  → HTTP 200 { success: true, message: "Scaled to 5" }
```

---

## Environment Variables

### Kubesec.Auth

| Variable | Description |
|---|---|
| `ConnectionStrings__DefaultConnection` | PostgreSQL connection string |
| `ConnectionStrings__Redis` | Redis connection string |
| `Jwt__SecretKey` | JWT signing key |
| `Jwt__Issuer` | JWT issuer |
| `Jwt__Audience` | JWT audience |
| `Google__ClientId` | Google OAuth client ID |
| `Google__ClientSecret` | Google OAuth client secret |
| `KubeManager__Address` | KuberManager gRPC address |
| `Email__Host`, `Email__Port`, etc. | SMTP config |

### KuberManager

| Variable | Description |
|---|---|
| `Kubernetes__Clusters__<name>__KubeconfigPath` | Path to kubeconfig file |
| `Policy__AllowedNamespaces__0` | Allowed namespace (array) |
| `Policy__MaxReplicas` | Maximum allowed replicas |
