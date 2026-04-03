using Grpc.Core;
using Grpc.Core.Interceptors;
using KuberManager.Service.Infrastructure.Options;
using Microsoft.Extensions.Options;

namespace KuberManager.Service.Infrastructure.Auth;

public sealed class ApiKeyInterceptor : Interceptor
{
    private readonly ApiKeyOptions _options;
    private readonly ILogger<ApiKeyInterceptor> _logger;

    private const string AuthHeader = "authorization";
    private const string BearerPrefix = "Bearer ";

    public ApiKeyInterceptor(IOptions<ApiKeyOptions> options, ILogger<ApiKeyInterceptor> logger)
    {
        _options = options.Value;
        _logger = logger;

        var emptyKeys = _options.Keys.Where(kv => string.IsNullOrWhiteSpace(kv.Value)).Select(kv => kv.Key).ToList();
        if (emptyKeys.Count > 0)
            throw new InvalidOperationException($"ApiKey configuration error: the following clients have empty keys: {string.Join(", ", emptyKeys)}. Set values via environment variables (ApiKey__Keys__<name>=<secret>) or appsettings.");
    }

    public override async Task<TResponse> UnaryServerHandler<TRequest, TResponse>(
        TRequest request,
        ServerCallContext context,
        UnaryServerMethod<TRequest, TResponse> continuation)
    {
        Authorize(context);
        return await continuation(request, context);
    }

    public override async Task ServerStreamingServerHandler<TRequest, TResponse>(
        TRequest request,
        IServerStreamWriter<TResponse> responseStream,
        ServerCallContext context,
        ServerStreamingServerMethod<TRequest, TResponse> continuation)
    {
        Authorize(context);
        await continuation(request, responseStream, context);
    }

    private void Authorize(ServerCallContext context)
    {
        // Health check endpoint ni o'tkazib yuboramiz
        if (context.Method.Contains("grpc.health"))
            return;

        var authHeader = context.RequestHeaders
            .FirstOrDefault(h => h.Key == AuthHeader)?.Value;

        if (string.IsNullOrEmpty(authHeader) || !authHeader.StartsWith(BearerPrefix, StringComparison.OrdinalIgnoreCase))
        {
            _logger.LogWarning("gRPC auth failed: no bearer token. Method={Method}", context.Method);
            throw new RpcException(new Status(StatusCode.Unauthenticated, "API key required."));
        }

        var providedKey = authHeader[BearerPrefix.Length..].Trim();

        var matched = _options.Keys.FirstOrDefault(kv =>
            string.Equals(kv.Value, providedKey, StringComparison.Ordinal));

        if (matched.Key is null)
        {
            _logger.LogWarning("gRPC auth failed: invalid API key. Method={Method}", context.Method);
            throw new RpcException(new Status(StatusCode.Unauthenticated, "Invalid API key."));
        }

        _logger.LogDebug("gRPC auth OK: client={Client}, Method={Method}", matched.Key, context.Method);
    }
}
