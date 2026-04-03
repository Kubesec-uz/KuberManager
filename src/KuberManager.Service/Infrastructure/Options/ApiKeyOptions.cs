namespace KuberManager.Service.Infrastructure.Options;

public sealed class ApiKeyOptions
{
    public const string Section = "ApiKey";

    /// <summary>
    /// Allowed API keys. Key = client name (for logging), Value = secret key.
    /// </summary>
    public Dictionary<string, string> Keys { get; init; } = new();
}
