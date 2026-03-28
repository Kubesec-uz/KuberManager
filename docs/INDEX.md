# Kubesec Platform — Dokumentatsiya

## Fayllar

| # | Fayl | Mavzu |
|---|---|---|
| 01 | [01-overview.md](01-overview.md) | Platform overview, umumiy arxitektura, request hayot davri |
| 02 | [02-kubermanager-architecture.md](02-kubermanager-architecture.md) | KuberManager layer lar, DI, nima singleton nima scoped |
| 03 | [03-grpc-services.md](03-grpc-services.md) | Barcha gRPC service lar: proto message lar, metodlar, Exec mexanizmi |
| 04 | [04-kubernetes-connection.md](04-kubernetes-connection.md) | KubernetesClientFactory, KubernetesFacade, barcha k8s API chaqiruvlari |
| 05 | [05-policy-and-audit.md](05-policy-and-audit.md) | OperationPolicy, AuditService, CorrelationId |
| 06 | [06-kubesec-auth-architecture.md](06-kubesec-auth-architecture.md) | Kubesec.Auth solution loyihalari, controller lar, middleware, entity lar |
| 07 | [07-grpc-client.md](07-grpc-client.md) | KubeManagerClient, barcha metodlar, Exec streaming, proto config |
| 08 | [08-rest-api-endpoints.md](08-rest-api-endpoints.md) | Barcha REST endpoint lar: route, request, response, misollar |
| 09 | [09-deployment-and-kubernetes.md](09-deployment-and-kubernetes.md) | K8s manifests, RBAC, NetworkPolicy, multi-cluster |
| 10 | [10-configuration.md](10-configuration.md) | appsettings, env variables, Options klasslar |
| 11 | [11-docker.md](11-docker.md) | Dockerfile, docker-compose, GitHub Actions, local dev |
| 12 | [12-dependencies.md](12-dependencies.md) | Barcha NuGet paketlar va ularning vazifasi |

## Tezkor yo'l

**gRPC service lar:** → [03](03-grpc-services.md)
**REST endpoint lar:** → [08](08-rest-api-endpoints.md)
**K8s ga deploy:** → [09](09-deployment-and-kubernetes.md)
**Local ishga tushirish:** → [11](11-docker.md#local-development-setup)
**Konfiguratsiya:** → [10](10-configuration.md)
**Policy sozlash:** → [05](05-policy-and-audit.md)
