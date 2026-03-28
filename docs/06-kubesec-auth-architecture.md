# 06 — Kubesec.Auth: Arxitektura

## Loyiha haqida

**Kubesec.Auth** — ASP.NET Core REST API (.NET 9.0).
Platformaning asosiy backend i. Auth, RBAC, ta'lim, Kubernetes management hammasi shu yerda.

## Solution tuzilmasi

```
kubesec-backend/
├── Kubesec.Auth/               ← REST API (presentation layer)
├── Kubesec.Auth.Service/       ← Biznes logika
├── Kubesec.Auth.Domain/        ← Domain modellari (entity lar)
├── Kubesec.Auth.DataAccess/    ← EF Core, repository lar
├── Kubesec.Auth.Tests/         ← Testlar
└── Kubesec.Auth.Bot/           ← Mustaqil Telegram bot
```

## Kubesec.Auth (API layer)

```
Kubesec.Auth/
├── Controllers/          ← 27 ta REST controller
├── BackgroundServices/   ← Fon jarayonlari
├── Middleware/           ← HTTP pipeline
├── Middlewares/          ← HTTP pipeline (2-papka)
├── Extensions/           ← DI registration
├── KubeManager/          ← gRPC client wrapper
├── Protos/               ← .proto (client-side)
├── Configurations/       ← AutoMapper
└── Program.cs
```

### Controllers (27 ta)

| Controller | Mas'uliyat |
|---|---|
| `AuthController` | Login, logout, token refresh, OAuth |
| `UserController` | Foydalanuvchi CRUD, profil |
| `RoleController` | Role boshqarish |
| `PermissionController` | Permission boshqarish |
| `AccessRuleController` | Access rule lar |
| `ClusterController` | Kubernetes cluster DB va operatsiyalar |
| `PathController` | Ta'lim yo'llari |
| `PathStepController` | Yo'l qadamlari |
| `PathEnrollmentController` | Ro'yxatga olish |
| `LabTemplateController` | Lab shablon lari |
| `LabInstanceController` | Lab instance lar |
| `ExamController` | Imtihon boshqarish |
| `QuizController` | Test lar |
| `ArticleController` | Maqolalar |
| `DashboardController` | Statistika |
| `NotificationController` | Bildirishnomalar |
| `ServerController` | Server boshqarish |
| `SwarmController` | Docker Swarm |
| `DockerTerminalController` | Docker terminal |
| `TerminalMonitorController` | Terminal monitoring |
| `CoinController` | Coin tizimi |
| `StudentGroupController` | Talaba guruhlari |
| `ImageController` | Rasm yuklash |
| `EmailController` | Email yuborish |
| `EnumController` | Enum qiymatlari |
| `BotVerificationController` | Telegram bot verifikatsiya |
| `AuditController` | Audit log lar |

### Middleware Pipeline (tartib bo'yicha)

```
Request keldi
      │
      ▼
DDoSProtectionMiddleware
  → IP ni tekshiradi (Redis da burst counter)
  → Limit oshsa 429 qaytaradi
      │
      ▼
RateLimitMiddleware
  → User/IP bo'yicha rate limiting
  → Redis da sliding window
      │
      ▼
TokenValidationMiddleware
  → JWT ni tekshiradi
  → Claims (userId, permissions) ni HttpContext ga qo'shadi
      │
      ▼
ActivityTrackingMiddleware
  → User faoliyatini yozadi (buffer ga)
      │
      ▼
ExceptionHandlerMiddleware
  → Ilingan exceptionlarni structured error response ga aylantiradi
      │
      ▼
Controller
```

### Background Services

| Servis | Vazifa |
|---|---|
| `ActivitySyncBackgroundService` | Buffer dagi aktivlikni DB ga yozadi |
| `RateLimitCleanupService` | Eskirgan rate limit yozuvlarni tozalaydi |
| `ExamSchedulerBackgroundService` | Belgilangan vaqtda imtihon boshlash/tugatish |
| `LabCleanupBackgroundService` | Muddati o'tgan lab instance larni o'chiradi |
| `ServiceCleanupBackgroundService` | Umumiy tozalash |

---

## Kubesec.Auth.Service (Biznes logika)

```
Kubesec.Auth.Service/
├── IServices/     ← 37 ta interface
├── Services/      ← 28+ implementatsiya
├── DTOs/          ← 26+ subdirectory
├── Mappers/       ← AutoMapper profillari
├── Helpers/       ← Yordamchi funksiyalar
├── Validators/    ← Fluent Validation
├── Exceptions/    ← Custom exception lar
├── Seeders/       ← DB seed ma'lumotlar
└── WebSocket/     ← Real-time
```

### Asosiy servislar

| Servis | Vazifa |
|---|---|
| `AuthService` | Login, JWT yaratish, session boshqarish |
| `AuthorizationService` | Permission tekshirish |
| `UserService` | Foydalanuvchi operatsiyalari |
| `RoleService` / `PermissionService` | RBAC |
| `SessionService` | Session yaratish, tekshirish, o'chirish |
| `ClusterService` | Cluster DB CRUD |
| `PathService` / `PathStepService` | Ta'lim yo'llari |
| `PathEnrollmentService` | Ro'yxatga olish logikasi |
| `LabInstanceService` | Lab instance lifecycle |
| `ExamService` | Imtihon logikasi |
| `QuizService` | Test logikasi |
| `NotificationService` | Bildirishnoma yuborish |
| `EmailService` | SMTP email |
| `RedisCacheService` | Redis cache wrapper |
| `RateLimitService` | Rate limit logikasi |
| `ImageService` | Rasm saqlash/qaytarish |
| `CoinService` | Coin hisoblash |
| `DashboardService` | Statistika yig'ish |
| `AuditLogService` | DB ga audit yozish |
| `BotVerificationService` | Telegram orqali verifikatsiya |

---

## Kubesec.Auth.Domain (Entity lar)

### Entity kategoriyalari (57 ta entity)

```
Entities/
├── Users/          AppUser, UserProfile, UserActivity, UserFollow
├── Auth/           Session, OauthAccount, Role, Permission
├── Labs/           LabTemplate, LabInstance, LabEvent,
│                   LabAccessRule, LabUserAction, LabTestResult
├── Paths/          Path, PathStep, PathEnrollment, PathStepProgress,
│                   PathAccessRule, PathStepAccessRule, PathStepLabTemplate
├── Exams/          Exam, ExamQuestion, ExamAttempt,
│                   ExamSchedule, ExamQuestionAnswer
├── Quizzes/        Quiz, QuizQuestion, QuizAttempt, QuizQuestionAnswer + 1
├── Courses/        Course, CourseModule, CourseEnrollment,
│                   Lesson, LessonProgress + 1
├── Articles/       Article, ArticleComment, ArticleLike
├── Coins/          UserCoin, PathCoinReward, LabCoinReward
├── Groups/         StudentGroup, StudentGroupMember
├── Servers/        2 ta entity
├── Security/       IpBlacklist, RateLimitLog
├── Notifications/  2 ta entity
├── Bot/            2 ta entity
└── DocsEntities/   3 ta entity
```

---

## Kubesec.Auth.DataAccess (Ma'lumot qatlami)

```
Kubesec.Auth.DataAccess/
├── AppDbContexts/
│   └── AppDbContext.cs      ← EF Core DbContext (PostgreSQL)
├── IRepository/
│   ├── IGenericRepository.cs
│   └── IServerRepository.cs  (va boshqalar)
├── Repository/               ← Implementatsiyalar
├── DTOs/
│   └── ServerStatisticsDto.cs
└── Migrations/               ← EF Core migratsiyalar
```

**Database:** PostgreSQL
**ORM:** Entity Framework Core 9.0

---

## Kubesec.Auth.Bot (Mustaqil servis)

```
Kubesec.Auth.Bot/
├── Data/
│   └── BotDbContext.cs           ← Bot uchun alohida DB
├── Handlers/
│   └── BotUpdateHandler.cs       ← Telegram update handler
├── Models/
│   ├── BotUser.cs
│   └── Message.cs
├── Services/
│   ├── IMessageService.cs / MessageService.cs
│   ├── ITelegramNotificationService.cs / TelegramNotificationService.cs
│   └── IVerificationService.cs / VerificationService.cs
├── Program.cs
└── Dockerfile.bot
```

**Vazifasi:**
- Foydalanuvchi akkauntlarini Telegram orqali verifikatsiya
- Lab boshlangan/to'xtatilganda notification
- Imtihon eslatmalari
- Bot buyruqlarini qayta ishlash

Alohida Docker konteynerda ishlaydi, o'z DB si bor.

---

## HttpContextHelper

Controller larda `HttpContextHelper.UserId` va `HttpContextHelper.UserPermission` ishlatiladi.
Bu static helper `TokenValidationMiddleware` da HttpContext ga yozilgan claims ni o'qiydi.

```csharp
// Controller da:
HttpContextHelper.UserId.ToString()          // Guid — joriy user ID
HttpContextHelper.UserPermission             // IEnumerable<string> — permission list
```

```csharp
// HasPermission helper:
private bool HasPermission(string permission)
    => HttpContextHelper.UserPermission.Any(p => p == permission);
```
