# 04 — Kubernetes Ulanish va Facade

## KubernetesClientFactory

**Fayl:** `Infrastructure/Kubernetes/KubernetesClientFactory.cs`
**Lifetime:** Singleton

Har bir cluster uchun bitta `IKubernetes` client yaratadi va cache da saqlaydi.

```
"prod-1"   → IKubernetes (cached)
"staging"  → IKubernetes (cached)
"default"  → IKubernetes (cached)
```

### Config Tanlab Olish Tartibi

```
clusterName so'rov
       │
       ▼
KubernetesOptions.UseInClusterConfig = true?
       │ HA                    │ YO'Q
       ▼                       ▼
InClusterConfig       Clusters[clusterName] bor va path to'liq?
(ServiceAccount         │ HA                │ YO'Q
 token va cert          ▼                   ▼
 /var/run/secrets)  kubeconfig fayldan    ~/.kube/config
                    (BuildConfigFromFile)  (BuildDefaultConfig)
```

**Muhit:**
- **Kubernetes ichida (production):** `UseInClusterConfig=true` — ServiceAccount token dan config oladi
- **Dev (mahalliy):** `Clusters` da path berilgan bo'lsa o'sha, yo'qsa `~/.kube/config`

### appsettings.json konfiguratsiyasi

```json
{
  "Kubernetes": {
    "UseInClusterConfig": false,
    "Clusters": {
      "prod-1":   { "KubeconfigPath": "/etc/kubeconfig/prod-1.yaml" },
      "staging":  { "KubeconfigPath": "/etc/kubeconfig/staging.yaml" },
      "local":    { "KubeconfigPath": "" }
    }
  }
}
```

Kubernetes deployment da environment variable orqali:
```yaml
- name: Kubernetes__UseInClusterConfig
  value: "true"
```

### Thread Safety

`_lock = new Lock()` (C# 13 Lock type) — bir vaqtda bir nechta so'rov kelsa ham xavfsiz.
Client bir marta yaratilganidan keyin faqat o'qiladi → lock faqat birinchi creation da kerak.

---

## KubernetesFacade

**Fayl:** `Infrastructure/Kubernetes/KubernetesFacade.cs`
**Lifetime:** Scoped (har so'rov uchun yangi instance)

`IKubernetes` client ni yashiradi. Barcha Kubernetes API chaqiruvlari shu orqali ketadi.

### Deployment metodlar

| Metod | k8s API |
|---|---|
| `GetDeploymentAsync(ns, name)` | `AppsV1.ReadNamespacedDeployment` |
| `ListDeploymentsAsync(ns)` | `AppsV1.ListNamespacedDeployment` |
| `CreateDeploymentAsync(ns, deployment)` | `AppsV1.CreateNamespacedDeployment` |
| `DeleteDeploymentAsync(ns, name)` | `AppsV1.DeleteNamespacedDeployment` |
| `PatchDeploymentReplicasAsync(ns, name, replicas)` | `AppsV1.PatchNamespacedDeployment` (MergePatch) |
| `PatchDeploymentRestartAsync(ns, name, time)` | `AppsV1.PatchNamespacedDeployment` (MergePatch) |

**Scale patch body:**
```json
{ "spec": { "replicas": 5 } }
```

**Restart patch body:**
```json
{
  "spec": {
    "template": {
      "metadata": {
        "annotations": {
          "kubectl.kubernetes.io/restartedAt": "2026-03-29T10:00:00+00:00"
        }
      }
    }
  }
}
```

### Pod metodlar

| Metod | k8s API |
|---|---|
| `ListPodsAsync(ns, labelSelector?)` | `CoreV1.ListNamespacedPod` |
| `GetPodAsync(ns, podName)` | `CoreV1.ReadNamespacedPod` |
| `GetPodLogsAsync(ns, podName, container?, tailLines?)` | `CoreV1.ReadNamespacedPodLog` |
| `DeletePodAsync(ns, podName)` | `CoreV1.DeleteNamespacedPod` |
| `ExecInPodAsync(ns, podName, container, command[])` | `WebSocketNamespacedPodExec` |

**ListPods label filter:** `app_label` bo'sh bo'lmasa `labelSelector: "app={app_label}"` uzatiladi.

**GetPodLogs:** Stream qaytaradi → `StreamReader` bilan to'liq o'qiladi → string qaytariladi.

### Exec to'liq jarayon

```csharp
// 1. WebSocket connection oching
var webSocket = await _client.WebSocketNamespacedPodExecAsync(
    podName, ns,
    command: new[] { "sh", "-c", "ls -la" },
    container: "app",
    stdout: true, stderr: true, stdin: false, tty: false);

// 2. StreamDemuxer stdout/stderr ni ajratadi
using var demux = new StreamDemuxer(webSocket);
demux.Start();

// 3. Alohida streamlar
using var stdoutStream = demux.GetStream(ChannelIndex.StdOut, null);
using var stderrStream = demux.GetStream(ChannelIndex.StdErr, null);

// 4. Parallel o'qish
var stdoutTask = ReadStreamAsync(stdoutStream, isStderr: false, ct);
var stderrTask = ReadStreamAsync(stderrStream, isStderr: true, ct);
await Task.WhenAll(stdoutTask, stderrTask);

// 5. Birlashtirib yield return
foreach (var item in stdoutTask.Result.Concat(stderrTask.Result))
    yield return item;
```

### Namespace metodlar

| Metod | k8s API |
|---|---|
| `ListNamespacesAsync()` | `CoreV1.ListNamespace` |
| `GetNamespaceAsync(name)` | `CoreV1.ReadNamespace` |
| `CreateNamespaceAsync(V1Namespace)` | `CoreV1.CreateNamespace` |
| `DeleteNamespaceAsync(name)` | `CoreV1.DeleteNamespace` |

### ConfigMap metodlar

| Metod | k8s API |
|---|---|
| `ListConfigMapsAsync(ns)` | `CoreV1.ListNamespacedConfigMap` |
| `GetConfigMapAsync(ns, name)` | `CoreV1.ReadNamespacedConfigMap` |
| `CreateConfigMapAsync(ns, V1ConfigMap)` | `CoreV1.CreateNamespacedConfigMap` |
| `UpdateConfigMapAsync(ns, name, V1ConfigMap)` | `CoreV1.ReplaceNamespacedConfigMap` |
| `DeleteConfigMapAsync(ns, name)` | `CoreV1.DeleteNamespacedConfigMap` |

Update → `Replace` (PUT) — to'liq almashtirish. Qisman patch emas.

### Cluster metodlar

| Metod | k8s API |
|---|---|
| `GetServerVersionAsync()` | `Version.GetCode` |

Natija: `"{Major}.{Minor}"` — masalan `"1.28"`

---

## Create Deployment — V1Deployment qurilishi

`DeploymentManager.CreateAsync` da DTO dan `V1Deployment` obyekti quriladi:

```csharp
var labels = dto.Labels.Count > 0
    ? dto.Labels
    : new Dictionary<string, string> { ["app"] = dto.Name };  // default label

var deployment = new V1Deployment
{
    Metadata = new V1ObjectMeta { Name = dto.Name, NamespaceProperty = ns, Labels = labels },
    Spec = new V1DeploymentSpec
    {
        Replicas = dto.Replicas,
        Selector = new V1LabelSelector { MatchLabels = labels },
        Template = new V1PodTemplateSpec
        {
            Metadata = new V1ObjectMeta { Labels = labels },
            Spec = new V1PodSpec
            {
                Containers = [ new V1Container {
                    Name   = dto.Name,
                    Image  = dto.Image,
                    Env    = dto.EnvVars.Select(e => new V1EnvVar { Name = e.Key, Value = e.Value }).ToList(),
                    Ports  = dto.Ports.Select(p => new V1ContainerPort {
                        Name = p.Name, ContainerPort = p.ContainerPort, Protocol = p.Protocol
                    }).ToList()
                }]
            }
        }
    }
};
```

**Muhim:** `MatchLabels` va pod `Labels` bir xil bo'lishi shart — aks holda Deployment o'z podlarini topa olmaydi.
