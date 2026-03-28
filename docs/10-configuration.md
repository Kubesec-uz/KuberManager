# 10 — Konfiguratsiya

## KuberManager konfiguratsiyasi

### appsettings.json (production bazasi)

```json
{
  "Serilog": {
    "MinimumLevel": {
      "Default": "Information",
      "Override": {
        "Microsoft": "Warning",
        "Grpc": "Warning"
      }
    }
  },
  "Kubernetes": {
    "UseInClusterConfig": false,
    "Clusters": {}
  },
  "Policy": {
    "AllowedNamespaces": [],
    "BlockedDeployments": [],
    "MaxReplicas": 50
  }
}
```

### appsettings.Development.json

```json
{
  "Serilog": {
    "MinimumLevel": {
      "Default": "Debug"
    }
  },
  "Kubernetes": {
    "UseInClusterConfig": false,
    "Clusters": {
      "local": "",
      "minikube": ""
    }
  },
  "Policy": {
    "AllowedNamespaces": [],
    "MaxReplicas": 100
  }
}
```

### Kubernetes deployment uchun environment variables

```yaml
env:
  # ASP.NET Core
  - name: ASPNETCORE_ENVIRONMENT
    value: "Production"
  - name: ASPNETCORE_URLS
    value: "http://+:5001"

  # Kubernetes ulanish
  - name: Kubernetes__UseInClusterConfig
    value: "true"

  # Policy
  - name: Policy__AllowedNamespaces__0
    value: "app-prod"
  - name: Policy__AllowedNamespaces__1
    value: "app-stage"
  - name: Policy__MaxReplicas
    value: "20"

  # Bloklangan deployment lar (ixtiyoriy)
  - name: Policy__BlockedDeployments__0
    value: "app-prod/core-db"
```

---

## Kubesec.Auth konfiguratsiyasi

### appsettings.json (production bazasi)

```json
{
  "ConnectionStrings": {
    "DefaultConnection": "Host=postgres;Port=5432;Database=kubesec;Username=kubesec;Password=...",
    "Redis": "redis:6379,password=..."
  },
  "Jwt": {
    "SecretKey": "...",
    "Issuer": "kubesec.auth",
    "Audience": "kubesec.client",
    "ExpireMinutes": 60
  },
  "KubeManager": {
    "Address": "http://kubermanager.platform.svc.cluster.local:5001"
  },
  "Email": {
    "Host": "smtp.gmail.com",
    "Port": 587,
    "Username": "...",
    "Password": "...",
    "From": "noreply@kubesec.io"
  },
  "Google": {
    "ClientId": "...",
    "ClientSecret": "..."
  },
  "Serilog": {
    "MinimumLevel": {
      "Default": "Information"
    }
  }
}
```

### appsettings.Development.json

```json
{
  "KubeManager": {
    "Address": "http://localhost:5001"
  },
  "ConnectionStrings": {
    "DefaultConnection": "Host=localhost;...",
    "Redis": "localhost:6379"
  }
}
```

### appsettings.Production.json

```json
{
  "Serilog": {
    "MinimumLevel": {
      "Default": "Warning",
      "Override": {
        "Kubesec": "Information"
      }
    }
  }
}
```

---

## Options klasslar

### KubernetesOptions (KuberManager)

```csharp
public sealed class KubernetesOptions
{
    public const string Section = "Kubernetes";
    public bool UseInClusterConfig { get; set; }
    public Dictionary<string, string> Clusters { get; set; } = new();
    // Key: cluster nomi, Value: kubeconfig path (bo'sh = default)
}
```

### PolicyOptions (KuberManager)

```csharp
public sealed class PolicyOptions
{
    public const string Section = "Policy";
    public List<string> AllowedNamespaces { get; set; } = new();
    public List<string> BlockedDeployments { get; set; } = new();
    // Format: "namespace/deployment-name"
    public int MaxReplicas { get; set; } = 50;
}
```

### KubeManagerOptions (Kubesec.Auth)

```csharp
public sealed class KubeManagerOptions
{
    public const string Section = "KubeManager";
    public string Address { get; set; } = "";
}
```

---

## gRPC muloqot

### Ikkala service bir xil proto ishlatishi uchun

1. `KuberManager.Service/Protos/*.proto` — asosiy (server GrpcServices="Server")
2. `Kubesec.Auth/Protos/*.proto` — nusxa (client GrpcServices="Client")

Proto fayllar o'zgarganda ikki joyda ham yangilanishi kerak.

### gRPC kanal sozlamalari

`KubeManagerClient` default `GrpcChannel` ishlatadi.
Production da TLS, timeout, retry sozlamalari qo'shilishi mumkin:

```csharp
_channel = GrpcChannel.ForAddress(address, new GrpcChannelOptions
{
    // MaxReceiveMessageSize = 16 * 1024 * 1024,  // 16MB
    // HttpHandler = ...
});
```

---

## DetailedErrors sozlamasi

```csharp
// Program.cs (KuberManager):
builder.Services.AddGrpc(options =>
{
    options.EnableDetailedErrors = builder.Environment.IsDevelopment();
});
```

Development da gRPC xato tafsilotlari mijozga keladi.
Production da faqat "Internal error." keladi (stack trace yo'q).
