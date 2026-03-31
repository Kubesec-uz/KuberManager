using KuberManager.Service.Application;
using KuberManager.Service.Grpc;
using KuberManager.Service.Infrastructure.Audit;
using KuberManager.Service.Infrastructure.Kubernetes;
using KuberManager.Service.Infrastructure.Options;
using KuberManager.Service.Infrastructure.Policy;
using Serilog;

Log.Logger = new LoggerConfiguration()
    .WriteTo.Console()
    .CreateBootstrapLogger();

try
{
    var builder = WebApplication.CreateBuilder(args);

    builder.Host.UseSerilog((ctx, services, config) =>
        config.ReadFrom.Configuration(ctx.Configuration)
              .ReadFrom.Services(services)
              .Enrich.FromLogContext()
              .WriteTo.Console());

    // Options
    builder.Services.Configure<KubernetesOptions>(
        builder.Configuration.GetSection(KubernetesOptions.Section));
    builder.Services.Configure<PolicyOptions>(
        builder.Configuration.GetSection(PolicyOptions.Section));

    // Infrastructure
    builder.Services.AddSingleton<IKubernetesClientFactory, KubernetesClientFactory>();
    builder.Services.AddSingleton<IKubernetesFacadeFactory, KubernetesFacadeFactory>();
    builder.Services.AddSingleton<IOperationPolicy, OperationPolicy>();
    builder.Services.AddSingleton<IAuditService, LogAuditService>();

    // Application
    builder.Services.AddScoped<IDeploymentManager, DeploymentManager>();
    builder.Services.AddScoped<IPodManager, PodManager>();
    builder.Services.AddScoped<INamespaceManager, NamespaceManager>();
    builder.Services.AddScoped<IConfigMapManager, ConfigMapManager>();

    // gRPC
    builder.Services.AddGrpc(options =>
    {
        options.EnableDetailedErrors = builder.Environment.IsDevelopment();
    });

    builder.Services.AddGrpcHealthChecks();
    builder.Services.AddHealthChecks();

    var app = builder.Build();

    app.MapGrpcService<DeploymentGrpcService>();
    app.MapGrpcService<PodGrpcService>();
    app.MapGrpcService<NamespaceGrpcService>();
    app.MapGrpcService<ConfigMapGrpcService>();
    app.MapGrpcService<ClusterHealthGrpcService>();
    app.MapGrpcHealthChecksService();
    app.MapHealthChecks("/healthz");

    app.Run();
}
catch (Exception ex)
{
    Log.Fatal(ex, "Application start-up failed");
}
finally
{
    Log.CloseAndFlush();
}
