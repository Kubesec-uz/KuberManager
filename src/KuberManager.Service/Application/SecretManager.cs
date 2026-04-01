using k8s.Models;
using KuberManager.Service.Application.DTOs;
using KuberManager.Service.Domain;
using KuberManager.Service.Infrastructure.Audit;
using KuberManager.Service.Infrastructure.Kubernetes;
using KuberManager.Service.Infrastructure.Policy;
using System.Text;

namespace KuberManager.Service.Application;

public sealed class SecretManager : ISecretManager
{
    private readonly IKubernetesFacadeFactory _k8sFactory;
    private readonly IOperationPolicy _policy;
    private readonly IAuditService _audit;
    private readonly ILogger<SecretManager> _logger;

    public SecretManager(IKubernetesFacadeFactory k8sFactory, IOperationPolicy policy, IAuditService audit, ILogger<SecretManager> logger)
    {
        _k8sFactory = k8sFactory;
        _policy = policy;
        _audit = audit;
        _logger = logger;
    }

    public async Task<IReadOnlyList<SecretDto>> ListAsync(string cluster, string ns, CancellationToken ct = default)
    {
        _policy.EnsureNamespaceAllowed(ns);
        var list = await _k8sFactory.For(cluster).ListSecretsAsync(ns, ct);
        return list.Select(MapToDto).ToList();
    }

    public async Task<SecretDto> GetAsync(string cluster, string ns, string name, CancellationToken ct = default)
    {
        _policy.EnsureNamespaceAllowed(ns);
        var secret = await _k8sFactory.For(cluster).GetSecretAsync(ns, name, ct);
        return MapToDto(secret);
    }

    public async Task<OperationResult> CreateAsync(CreateSecretDto dto, CancellationToken ct = default)
    {
        _policy.EnsureNamespaceAllowed(dto.Namespace);
        _logger.LogInformation("Creating secret {Namespace}/{Name} by {RequestedBy} [{CorrelationId}]", dto.Namespace, dto.Name, dto.RequestedBy, dto.CorrelationId);
        try
        {
            var secretData = new Dictionary<string, byte[]>();
            foreach (var kvp in dto.StringData)
            {
                secretData[kvp.Key] = Encoding.UTF8.GetBytes(kvp.Value);
            }

            var secret = new V1Secret
            {
                Metadata = new V1ObjectMeta { Name = dto.Name, NamespaceProperty = dto.Namespace },
                Type = dto.Type,
                Data = secretData
            };

            await _k8sFactory.For(dto.Cluster).CreateSecretAsync(dto.Namespace, secret, ct);

            await _audit.RecordAsync(new AuditEntry
            {
                CorrelationId = dto.CorrelationId, RequestedBy = dto.RequestedBy,
                Action = "CreateSecret", Cluster = dto.Cluster, Namespace = dto.Namespace,
                ResourceType = "Secret", ResourceName = dto.Name, Reason = dto.Reason, Success = true
            }, ct);

            return OperationResult.Ok(dto.CorrelationId);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to create secret {Namespace}/{Name}", dto.Namespace, dto.Name);
            await _audit.RecordAsync(new AuditEntry
            {
                CorrelationId = dto.CorrelationId, RequestedBy = dto.RequestedBy,
                Action = "CreateSecret", Cluster = dto.Cluster, Namespace = dto.Namespace,
                ResourceType = "Secret", ResourceName = dto.Name, Reason = dto.Reason,
                Success = false, ErrorMessage = ex.Message
            }, ct);
            return OperationResult.Fail(dto.CorrelationId, ex.Message);
        }
    }

    public async Task<OperationResult> DeleteAsync(string cluster, string ns, string name, string requestedBy, string reason, string correlationId, CancellationToken ct = default)
    {
        _policy.EnsureNamespaceAllowed(ns);
        _logger.LogInformation("Deleting secret {Namespace}/{Name} by {RequestedBy} [{CorrelationId}]", ns, name, requestedBy, correlationId);
        try
        {
            await _k8sFactory.For(cluster).DeleteSecretAsync(ns, name, ct);

            await _audit.RecordAsync(new AuditEntry
            {
                CorrelationId = correlationId, RequestedBy = requestedBy,
                Action = "DeleteSecret", Cluster = cluster, Namespace = ns,
                ResourceType = "Secret", ResourceName = name, Reason = reason, Success = true
            }, ct);

            return OperationResult.Ok(correlationId);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to delete secret {Namespace}/{Name}", ns, name);
            await _audit.RecordAsync(new AuditEntry
            {
                CorrelationId = correlationId, RequestedBy = requestedBy,
                Action = "DeleteSecret", Cluster = cluster, Namespace = ns,
                ResourceType = "Secret", ResourceName = name, Reason = reason,
                Success = false, ErrorMessage = ex.Message
            }, ct);
            return OperationResult.Fail(correlationId, ex.Message);
        }
    }

    private static SecretDto MapToDto(V1Secret secret)
    {
        var stringData = new Dictionary<string, string>();
        if (secret.Data != null)
        {
            foreach (var kvp in secret.Data)
            {
                stringData[kvp.Key] = Encoding.UTF8.GetString(kvp.Value);
            }
        }

        return new SecretDto
        {
            Name = secret.Metadata.Name,
            Namespace = secret.Metadata.NamespaceProperty,
            Type = secret.Type ?? string.Empty,
            Data = stringData,
            CreatedAt = secret.Metadata.CreationTimestamp?.ToString("O") ?? string.Empty
        };
    }
}
