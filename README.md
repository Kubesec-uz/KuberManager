# KuberManager

**KuberManager** — Kubernetes muhitlarini xavfsiz boshqarish va nazorat qilish uchun mo'ljallangan markaziy gRPC mikroservis (ASP.NET Core / .NET 9.0). 
U kubernetes klasterlari bilan ishlashda barcha kiruvchi so'rovlarni (Deployment, Pods, Namespaces kabi) qabul qiladi, siyosiy qoidalarga (policies) muvofiqligini tekshiradi va barcha o'zgartirishlarni audit logiga yozib boradi.

Bu loyiha **Kubesec Platform** ning orqa qismidagi muhim arxitektura bo'lagi bo'lib, REST API (`Kubesec.Auth`) va Kubernetes klasterlari o'rtasida "darvozabon" (gateway) vazifasini bajaradi.

## Asosiy imkoniyatlar (Features)
- **gRPC orqali muloqot:** Protokol asosida tez va xavfsiz ma'lumot almashinuvi (`Pods`, `Deployments`, `Namespaces`, `ConfigMaps`).
- **Xavfsizlik Siyosati (Operation Policy):** Ruxsat etilgan *namespace*'lar ro'yxati va *replica* limitlarini markaziy tekshirish.
- **Audit Logging:** Klasterga qilingan barcha tranzaktsiyalar (Create, Delete, Scale, Restart) va komandalar (Exec) tarixini yuritish.
- **Multi-cluster:** Bitta instansiyadan turib bir nechta turli Kubernetes klasterlarni boshqarish.
- **Pod Exec:** Container ichida komandalarni server-streaming orqali real vaqt rejimida yurgizish.

## Batafsil Hujjatlar (Documentation)
Loyiha tuzilishi va arxitektura bo'yicha to'liq qo'llanmalar **[`docs/`](docs/)** papkasida keltirilgan:

- 📖 **[Platforma haqida to'liq inglizcha qo'llanma (Full Guide)](docs/README.md)** 
- 🏛 **[01 — Umumiy Arxitektura / Overview](docs/01-overview.md)** 
- 🏗 **[02 — KuberManager Ichki Arxitekturasi (DI va Layerlar)](docs/02-kubermanager-architecture.md)** 
- 🔌 **[03 — Barcha gRPC xizmatlari (Services)](docs/03-grpc-services.md)** 
- 🌐 **[08 — REST API Endpoints](docs/08-rest-api-endpoints.md)** 
- 🚀 **[09 — K8s ga deploy qilish va RBAC](docs/09-deployment-and-kubernetes.md)** 
- 🐳 **[11 — Docker orqali local ishga tushirish](docs/11-docker.md)** 
- 📜 **[To'liq fayllar indeksi](docs/INDEX.md)**

## Qisqacha ishga tushirish (Local Dev)
Loyihani mahalliy (local) muhitda ishga tushirish uchun .NET 9.0 o'rnatilgan bo'lishi va `appsettings.Development.json` da to'g'ri `kubeconfig` yo'llari ko'rsatilishi kerak.

```bash
cd src/KuberManager.Service
dotnet run
```
Servis standart holda `http://0.0.0.0:5001` portida gRPC orqali javob bera boshlaydi.

Kubesec CLI yoki boshqa `KubeManagerClient` yordamida ulanib resurslarni boshqaring.
