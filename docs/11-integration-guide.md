# 11 - KuberManager bilan Integratsiya Qollanmasi (Integration Guide)

Ushbu hujjat boshqa kross-platformali xizmatlarning KuberManager bilan qanday integratsiya qilinishi bo'yicha ko'rsatmalarni o'z ichiga oladi.

## 1. Proto fayllarni ulash (Protos)

KuberManager bilan ishlashning asosi gRPC. Klientlar API ga murojaat qilish uchun `.proto` kengaytmali fayllarga ehtiyoj sezishadi. Loyihalar tarkibiga KuberManager ning `Protos` jildidagi quyidagi fayllarini qo'shing:

- `common.proto` (ma'lumotlarni identifikatsiya qilish va umumiy modellar)
- `deployment.proto`, `pod.proto`, `namespace.proto`, `configmap.proto` va hk.

**Misol uchun `.NET` mijozida:**

`.csproj` fayliga quyidagi ItemGroup-ni qo'shing:
```xml
<ItemGroup>
  <Protobuf Include="Protos\*.proto" GrpcServices="Client" />
</ItemGroup>
```

## 2. Channel va Client yaratish

### .NET (C#)

```csharp
using Grpc.Net.Client;
using KuberManager.Contracts; // namespace proto dagi package va option ga bog'liq

var channel = GrpcChannel.ForAddress("http://kubermanager.platform.svc.cluster.local:5001");
var client = new DeploymentService.DeploymentServiceClient(channel);
```

### Go (Golang)

```go
import (
    "google.golang.org/grpc"
    pb "path/to/generated/kubermanager/contracts"
)

conn, err := grpc.Dial("kubermanager.platform.svc.cluster.local:5001", grpc.WithInsecure())
client := pb.NewDeploymentServiceClient(conn)
```

## 3. Klaster ID ni uzatish (Headers / Metadata)

Aksariyat request modellarida `cluster` degan maydon mavjud. 
`KuberManager` da bir nechta Kubernetes klasterlarini boshqarish mumkin.
Shu sababli har qanday requestda klaster nomini (masalan `default`, yoki `prod-cluster`) va `namespace` ni belgilash zarur.

```csharp
var reply = await client.GetStatusAsync(new DeploymentRef 
{
    Cluster = "default",
    Namespace = "app-prod",
    Name = "auth-service"
});
```

## 4. Xatoliklarni boshqarish (Error Handling)

KuberManager gRPC `StatusCode` lari orqali ma'lum xatolarni bildirishi mumkin:

- **PermissionDenied (7)**: `PolicyViolationException` ro'y berganida qaytadi. Masalan, klient bloklangan namespaceda deployment yaratmoqchi bo'lsa yoki replikalar soni chegaradan oshib ketsa.
- **NotFound (5)**: Kutilgan Kubernetes resursi (pod, ns, dp) topilmasa.
- **Internal (13)**: Noma'lum server tarofidagi xatoliklar.

### C# Catching Exceptions

```csharp
try 
{
    var response = await client.CreateAsync(request);
} 
catch (RpcException ex) when (ex.StatusCode == StatusCode.PermissionDenied) 
{
    Console.WriteLine($"Access Denied: {ex.Status.Detail}");
}
catch (RpcException ex) when (ex.StatusCode == StatusCode.NotFound) 
{
    Console.WriteLine($"Not Found: {ex.Status.Detail}");
}
```

## 5. Audit logs

Har bir `Create`, `Update`, `Delete`, `Scale`, `Restart` operatsiyalari backendada audit-logic ga tushadi. 
Shuning uchun mutate (o'zgartirish) requestlari doim quyidagi field-larga ega ekanligiga e'tibor qarating:
- `requested_by`: Operatsiyani rostan chaqirgan inson emaili yoki user-id si.
- `reason`: Nima uchun (majburiy emas lekin log uchun juda yaxshi).
- `correlation_id`: Log tronsaksiyasini qidirib topish uchun trace id.

## 6. Kelajakda QoS (Quality of Service)
Keyingi rejalarda Rate-Limit va Identity server bilan tasdiqlangan JWT tokenlarni gRPC Metadata larda yuborish kiritilishi mumkin. Klientlar Authorization Headerni uzatishga tayyor bo'lishi kerak.
