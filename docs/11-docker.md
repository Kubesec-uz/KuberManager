# 11 — Docker va CI/CD

## Kubesec.Auth Dockerfile

**Fayl:** `kubesec-backend/Dockerfile`

Multi-stage build, Alpine Linux asosida:

```dockerfile
# Stage 1: Build
FROM mcr.microsoft.com/dotnet/sdk:9.0-alpine AS build
WORKDIR /src
COPY . .
RUN dotnet restore
RUN dotnet publish -c Release -o /app/publish

# Stage 2: Runtime
FROM mcr.microsoft.com/dotnet/aspnet:9.0-alpine AS runtime
WORKDIR /app
COPY --from=build /app/publish .
ENV ASPNETCORE_URLS=http://+:4001
ENV TZ=Asia/Tashkent
EXPOSE 4001
ENTRYPOINT ["dotnet", "Kubesec.Auth.dll"]
```

**Alpine tanlanishi sababi:** Image hajmi kichik (~50MB runtime vs ~200MB default).
**Timezone:** `Asia/Tashkent` — log vaqtlari to'g'ri ko'rinishi uchun.

## Bot Dockerfile

**Fayl:** `kubesec-backend/Kubesec.Auth.Bot/Dockerfile.bot`

Xuddi shunga o'xshash multi-stage, lekin Bot project build qilinadi.

---

## Docker Compose fayllar

### docker-compose.yml (bazaviy)

Asosiy service lar ta'rifi:
- `kubesec-auth` — REST API (port 4001)
- `postgres` — PostgreSQL database
- `redis` — Redis cache

### docker-compose.local.yml (mahalliy dev)

```yaml
# Qo'shimcha sozlamalar:
# - Volume mount (hot-reload)
# - ASPNETCORE_ENVIRONMENT=Development
# - Debug port (5005)
# - PostgreSQL va Redis mahalliy portlari ochiq
```

Ishlatish:
```bash
docker-compose -f docker-compose.yml -f docker-compose.local.yml up
```

### docker-compose.prod.yml (production)

```yaml
# Production sozlamalar:
# - restart: always
# - Resource limits (CPU/memory)
# - Logging driver
# - Health check lar
# - Secrets orqali env variables
```

Ishlatish:
```bash
docker-compose -f docker-compose.yml -f docker-compose.prod.yml up -d
```

---

## GitHub Actions CI/CD

**Fayl:** `.github/workflows/deploy.yml`

Pipeline bosqichlari:

```yaml
on:
  push:
    branches: [main]

jobs:
  build-and-deploy:
    steps:
      # 1. Checkout
      - uses: actions/checkout@v4

      # 2. Docker login
      - name: Login to Registry
        uses: docker/login-action@v3

      # 3. Build va push
      - name: Build and Push
        uses: docker/build-push-action@v5
        with:
          push: true
          tags: registry/kubesec-auth:${{ github.sha }}

      # 4. Kubernetes deploy
      - name: Deploy to K8s
        run: |
          kubectl set image deployment/kubesec-auth \
            kubesec-auth=registry/kubesec-auth:${{ github.sha }}
          kubectl rollout status deployment/kubesec-auth
```

---

## KuberManager Docker image (mahalliy build)

KuberManager uchun Dockerfile (agar alohida bo'lsa):

```dockerfile
FROM mcr.microsoft.com/dotnet/sdk:9.0-alpine AS build
WORKDIR /src
COPY src/ .
RUN dotnet publish KuberManager.Service/KuberManager.Service.csproj \
    -c Release -o /app/publish

FROM mcr.microsoft.com/dotnet/aspnet:9.0-alpine AS runtime
WORKDIR /app
COPY --from=build /app/publish .
EXPOSE 5001
ENTRYPOINT ["dotnet", "KuberManager.Service.dll"]
```

Build va push:
```bash
docker build -t your-registry/kubermanager:latest .
docker push your-registry/kubermanager:latest
```

Keyin `deployment.yaml` dagi image ni yangilash:
```yaml
image: your-registry/kubermanager:latest
```

---

## Local Development Setup

### Faqat KuberManager ishga tushirish

```bash
cd kubernets-manager
dotnet run --project src/KuberManager.Service
# port: 5001
```

### Faqat Kubesec.Auth ishga tushirish

```bash
cd kubesec-backend
dotnet run --project Kubesec.Auth
# port: 4001
```

### Ikkisini birga

```bash
# Terminal 1:
cd kubernets-manager && dotnet run --project src/KuberManager.Service

# Terminal 2:
cd kubesec-backend && dotnet run --project Kubesec.Auth
```

`appsettings.Development.json` da KubeManager address `http://localhost:5001` — to'g'ri ulangan bo'ladi.

---

## Environment Variables (production)

### Kubesec.Auth

```bash
# Database
ConnectionStrings__DefaultConnection="Host=...;Database=kubesec;..."
ConnectionStrings__Redis="redis:6379"

# JWT
Jwt__SecretKey="<256-bit random key>"
Jwt__Issuer="kubesec.auth"
Jwt__Audience="kubesec.client"

# Google OAuth
Google__ClientId="..."
Google__ClientSecret="..."

# KuberManager
KubeManager__Address="http://kubermanager.platform.svc.cluster.local:5001"

# SMTP
Email__Host="smtp.gmail.com"
Email__Port="587"
Email__Username="..."
Email__Password="..."
```

### KuberManager

```bash
ASPNETCORE_ENVIRONMENT=Production

# Kubernetes
Kubernetes__UseInClusterConfig=true

# Policy
Policy__AllowedNamespaces__0=app-prod
Policy__AllowedNamespaces__1=app-stage
Policy__MaxReplicas=20
```
