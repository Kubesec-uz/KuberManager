# 09 — Kubernetes Deploy va RBAC

## KuberManager ni Kubernetes ga deploy qilish

Barcha manifest lar: `deploy/`

```
deploy/
├── deployment.yaml
└── rbac/
    ├── serviceaccount.yaml
    ├── role.yaml
    └── rolebinding.yaml
```

### Tartib

```bash
# 1. ServiceAccount
kubectl apply -f deploy/rbac/serviceaccount.yaml

# 2. RBAC rollar
kubectl apply -f deploy/rbac/role.yaml
kubectl apply -f deploy/rbac/rolebinding.yaml

# 3. Asosiy deployment
kubectl apply -f deploy/deployment.yaml
```

---

## deployment.yaml

Uchta Kubernetes resurs:

### 1. Deployment

```yaml
apiVersion: apps/v1
kind: Deployment
metadata:
  name: kubermanager
  namespace: platform
spec:
  replicas: 1
  template:
    spec:
      serviceAccountName: kubermanager-sa
      containers:
        - name: kubermanager
          image: your-registry/kubermanager:latest
          ports:
            - containerPort: 5001
              name: grpc
          env:
            - name: ASPNETCORE_ENVIRONMENT
              value: "Production"
            - name: Kubernetes__UseInClusterConfig
              value: "true"
            - name: Policy__AllowedNamespaces__0
              value: "app-prod"
            - name: Policy__AllowedNamespaces__1
              value: "app-stage"
            - name: Policy__MaxReplicas
              value: "20"
```

**Resource limitleri:**
```yaml
resources:
  requests:
    cpu: "100m"
    memory: "128Mi"
  limits:
    cpu: "500m"
    memory: "256Mi"
```

**Health probe lar:**
```yaml
livenessProbe:
  httpGet:
    path: /healthz
    port: 5001
  initialDelaySeconds: 10
  periodSeconds: 30

readinessProbe:
  httpGet:
    path: /healthz
    port: 5001
  initialDelaySeconds: 5
  periodSeconds: 10
```

**Security Context:**
```yaml
securityContext:
  allowPrivilegeEscalation: false
  readOnlyRootFilesystem: true
  runAsNonRoot: true
  runAsUser: 1000
```

### 2. Service (ClusterIP)

```yaml
apiVersion: v1
kind: Service
metadata:
  name: kubermanager
  namespace: platform
spec:
  type: ClusterIP
  selector:
    app: kubermanager
  ports:
    - name: grpc
      port: 5001
      targetPort: 5001
```

`Kubesec.Auth` shu address ga ulanadi:
```
http://kubermanager.platform.svc.cluster.local:5001
```

### 3. NetworkPolicy

```yaml
apiVersion: networking.k8s.io/v1
kind: NetworkPolicy
metadata:
  name: kubermanager-netpol
  namespace: platform
spec:
  podSelector:
    matchLabels:
      app: kubermanager
  policyTypes:
    - Ingress
  ingress:
    - from:
        - namespaceSelector:
            matchLabels:
              kubernetes.io/metadata.name: api
      ports:
        - protocol: TCP
          port: 5001
```

Faqat `api` namespace dan kelgan traffic qabul qilinadi. Boshqa podlar KuberManager ga ulanolmaydi.

---

## RBAC

### serviceaccount.yaml

```yaml
apiVersion: v1
kind: ServiceAccount
metadata:
  name: kubermanager-sa
  namespace: platform
```

### role.yaml

`app-prod` va `app-stage` namespace lari uchun alohida Role:

```yaml
apiVersion: rbac.authorization.k8s.io/v1
kind: Role
metadata:
  name: kubermanager-role
  namespace: app-prod
rules:
  - apiGroups: [""]
    resources: ["pods", "pods/log", "events"]
    verbs: ["get", "list", "watch", "delete"]
  - apiGroups: ["apps"]
    resources: ["deployments"]
    verbs: ["get", "list", "watch", "patch", "update"]
```

**Qo'shimcha kerak bo'ladigan ruxsatlar** (agar barcha operatsiyalar ishlatilsa):

```yaml
rules:
  # Pods: List, Get, Logs, Delete, Exec
  - apiGroups: [""]
    resources: ["pods", "pods/log", "pods/exec"]
    verbs: ["get", "list", "watch", "delete", "create"]

  # Deployments: CRUD + scale + patch
  - apiGroups: ["apps"]
    resources: ["deployments", "deployments/scale"]
    verbs: ["get", "list", "watch", "create", "update", "patch", "delete"]

  # Namespaces
  - apiGroups: [""]
    resources: ["namespaces"]
    verbs: ["get", "list", "watch", "create", "delete"]

  # ConfigMaps
  - apiGroups: [""]
    resources: ["configmaps"]
    verbs: ["get", "list", "watch", "create", "update", "delete"]
```

**Eslatma:** Namespace CRUD uchun `ClusterRole` kerak (namespace-scoped Role emas):
```yaml
apiVersion: rbac.authorization.k8s.io/v1
kind: ClusterRole
metadata:
  name: kubermanager-cluster-role
rules:
  - apiGroups: [""]
    resources: ["namespaces"]
    verbs: ["get", "list", "watch", "create", "delete"]
```

### rolebinding.yaml

```yaml
apiVersion: rbac.authorization.k8s.io/v1
kind: RoleBinding
metadata:
  name: kubermanager-binding
  namespace: app-prod
subjects:
  - kind: ServiceAccount
    name: kubermanager-sa
    namespace: platform
roleRef:
  kind: Role
  name: kubermanager-role
  apiGroup: rbac.authorization.k8s.io
```

---

## Multi-cluster setup

Bir nechta cluster boshqarilsa, har birining kubeconfig fayli Secret yoki Volume orqali mount qilinadi:

```yaml
# deployment.yaml da:
volumeMounts:
  - name: kubeconfigs
    mountPath: /etc/kubeconfig
    readOnly: true

volumes:
  - name: kubeconfigs
    secret:
      secretName: cluster-kubeconfigs
```

```yaml
# env variables:
- name: Kubernetes__Clusters__prod-1
  value: "/etc/kubeconfig/prod-1.yaml"
- name: Kubernetes__Clusters__staging
  value: "/etc/kubeconfig/staging.yaml"
- name: Kubernetes__UseInClusterConfig
  value: "false"
```

---

## Health Check endpoint

`/healthz` — ASP.NET Core health checks + gRPC health checks ikkalasi ham shu endpointda.

```csharp
// Program.cs da:
app.MapGrpcHealthChecksService();
app.MapHealthChecks("/healthz");
```

`curl http://kubermanager.platform.svc.cluster.local:5001/healthz`
```
Healthy
```
