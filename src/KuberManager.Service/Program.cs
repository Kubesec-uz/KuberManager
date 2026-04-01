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
    builder.Services.AddScoped<IServiceManager, ServiceManager>();
    builder.Services.AddScoped<ISecretManager, SecretManager>();
    builder.Services.AddScoped<IIngressManager, IngressManager>();
    // Phase 2
    builder.Services.AddScoped<IStatefulSetManager, StatefulSetManager>();
    builder.Services.AddScoped<IDaemonSetManager, DaemonSetManager>();
    builder.Services.AddScoped<IJobManager, JobManager>();
    builder.Services.AddScoped<ICronJobManager, CronJobManager>();
    // Phase 3
    builder.Services.AddScoped<INodeManager, NodeManager>();
    builder.Services.AddScoped<IPvcManager, PvcManager>();
    builder.Services.AddScoped<IHpaManager, HpaManager>();
    // Security
    builder.Services.AddScoped<IServiceAccountManager, ServiceAccountManager>();
    builder.Services.AddScoped<IRbacManager, RbacManager>();
    builder.Services.AddScoped<INetworkPolicyManager, NetworkPolicyManager>();

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
    app.MapGrpcService<ServiceGrpcService>();
    app.MapGrpcService<SecretGrpcService>();
    app.MapGrpcService<IngressGrpcService>();
    // Phase 2
    app.MapGrpcService<StatefulSetGrpcService>();
    app.MapGrpcService<DaemonSetGrpcService>();
    app.MapGrpcService<JobGrpcService>();
    app.MapGrpcService<CronJobGrpcService>();
    // Phase 3
    app.MapGrpcService<NodeGrpcService>();
    app.MapGrpcService<PvcGrpcService>();
    app.MapGrpcService<HpaGrpcService>();
    // Security
    app.MapGrpcService<ServiceAccountGrpcService>();
    app.MapGrpcService<RbacGrpcService>();
    app.MapGrpcService<NetworkPolicyGrpcService>();
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
