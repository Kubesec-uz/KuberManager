# 05 — Policy va Audit

## OperationPolicy

**Fayl:** `Infrastructure/Policy/OperationPolicy.cs`
**Interface:** `IOperationPolicy`
**Lifetime:** Singleton

Har bir mutating operatsiyadan oldin `Manager` lar shu classni chaqiradi.
Xatolik bo'lsa `PolicyViolationException` throw qiladi → gRPC `PermissionDenied`.

### Metodlar

#### `EnsureNamespaceAllowed(string ns)`

`AllowedNamespaces` ro'yxati bo'sh bo'lsa — hammasi ruxsat.
Ro'yxat bor bo'lsa — faqat ro'yxatdagilar ruxsat.

```csharp
if (_options.AllowedNamespaces.Count > 0 && !_options.AllowedNamespaces.Contains(ns))
    throw new PolicyViolationException($"Access to namespace '{ns}' is not allowed.");
```

#### `EnsureDeploymentAllowed(string ns, string deployment)`

`BlockedDeployments` ro'yxatida `"ns/deployment-name"` formatida saqlanadi.

```csharp
var key = $"{ns}/{deployment}";
if (_options.BlockedDeployments.Contains(key))
    throw new PolicyViolationException($"Operations on deployment '{deployment}' in namespace '{ns}' are not allowed.");
```

Misol: `"production/critical-db"` → bu deployment ga hech kim tegolmaydi.

#### `EnsureReplicaLimit(int replicas)`

```csharp
if (replicas < 0)
    throw new PolicyViolationException("Replica count cannot be negative.");

if (replicas > _options.MaxReplicas)
    throw new PolicyViolationException($"Replica count {replicas} exceeds the maximum allowed limit of {_options.MaxReplicas}.");
```

### PolicyOptions konfiguratsiyasi

`appsettings.json`:
```json
{
  "Policy": {
    "AllowedNamespaces": ["app-prod", "app-stage"],
    "BlockedDeployments": ["app-prod/core-db", "app-prod/auth-service"],
    "MaxReplicas": 20
  }
}
```

Kubernetes deployment da env var orqali:
```yaml
- name: Policy__AllowedNamespaces__0
  value: "app-prod"
- name: Policy__AllowedNamespaces__1
  value: "app-stage"
- name: Policy__MaxReplicas
  value: "20"
```

### Qaysi Manager qaysi tekshiruvni qiladi?

| Manager metodi | EnsureNamespace | EnsureDeployment | EnsureReplica |
|---|---|---|---|
| `GetStatus` | ✓ | — | — |
| `List` | ✓ | — | — |
| `Create` | ✓ | — | ✓ |
| `Delete` | ✓ | ✓ | — |
| `Scale` | ✓ | ✓ | ✓ |
| `Restart` | ✓ | ✓ | — |
| Pod `List/Get/Logs` | ✓ | — | — |
| Pod `Delete/Exec` | ✓ | — | — |
| Namespace `*` | — | — | — |
| ConfigMap `*` | ✓ | — | — |

### PolicyViolationException

```csharp
public sealed class PolicyViolationException : Exception
{
    public PolicyViolationException(string message) : base(message) { }
}
```

gRPC layer da ushlanadi:
```csharp
catch (PolicyViolationException ex)
{
    throw new RpcException(new Status(StatusCode.PermissionDenied, ex.Message));
}
```

---

## AuditService

**Fayl:** `Infrastructure/Audit/LogAuditService.cs`
**Interface:** `IAuditService`
**Lifetime:** Singleton

Hozirgi implementatsiya Serilog orqali structured log yozadi.
Keyinchalik DB yoki event bus ga almashtirilishi mumkin — faqat `IAuditService` ni implement qilish kifoya.

### AuditEntry modeli

```csharp
public sealed class AuditEntry
{
    public string Id            { get; init; } = Guid.NewGuid().ToString();
    public string CorrelationId { get; init; } = "";
    public string RequestedBy   { get; init; } = "";
    public string Action        { get; init; } = "";    // "ScaleDeployment", "CreateDeployment", ...
    public string Cluster       { get; init; } = "";
    public string Namespace     { get; init; } = "";
    public string ResourceType  { get; init; } = "";    // "Deployment", "Pod", "Namespace", "ConfigMap"
    public string ResourceName  { get; init; } = "";
    public string Reason        { get; init; } = "";
    public bool   Success       { get; init; }
    public string? ErrorMessage { get; init; }
    public DateTimeOffset Timestamp { get; init; } = DateTimeOffset.UtcNow;
}
```

### Log output misoli

```
[AUDIT] Id=abc-123 CorrelationId=xyz-456 RequestedBy=user-uuid-789
        Action=ScaleDeployment Cluster=prod-1 Namespace=app-prod
        ResourceType=Deployment ResourceName=api-server
        Reason="Traffic spike" Success=True Error=
```

### Action nomlari

| Operatsiya | Action string |
|---|---|
| Deployment yaratish | `CreateDeployment` |
| Deployment o'chirish | `DeleteDeployment` |
| Scale | `ScaleDeployment` |
| Restart | `RestartDeployment` |
| Pod o'chirish | `DeletePod` |
| Pod exec | `ExecPod` |
| Namespace yaratish | `CreateNamespace` |
| Namespace o'chirish | `DeleteNamespace` |
| ConfigMap yaratish | `CreateConfigMap` |
| ConfigMap yangilash | `UpdateConfigMap` |
| ConfigMap o'chirish | `DeleteConfigMap` |

### Success va Fail pattern

Har bir Manager metodi try/catch da audit yozadi:

```csharp
try
{
    await _k8s.SomeOperation(...);
    await _audit.RecordAsync(new AuditEntry { ..., Success = true });
    return OperationResult.Ok(correlationId);
}
catch (Exception ex)
{
    _logger.LogError(ex, "Failed: {Namespace}/{Name}", ns, name);
    await _audit.RecordAsync(new AuditEntry { ..., Success = false, ErrorMessage = ex.Message });
    return OperationResult.Fail(correlationId, ex.Message);
}
```

**Muhim:** Exception re-throw qilinmaydi — `OperationResult.Fail` qaytariladi.
Bu `Grpc layer` da `OperationReply { Success: false, Message: "..." }` bo'lib keladi.

---

## CorrelationId

Har bir so'rov uchun `Guid.NewGuid().ToString()` — Kubesec.Auth da yaratiladi.
`KuberManagerClient` → `Manager` → `AuditEntry` → log gacha bir xil `CorrelationId` boradi.
Bu orqali bir so'rovga tegishli barcha log yozuvlarni topish mumkin.

```
Serilog filter: CorrelationId = "abc-123"
→ barcha log satrlar shu so'rovga tegishli
```
