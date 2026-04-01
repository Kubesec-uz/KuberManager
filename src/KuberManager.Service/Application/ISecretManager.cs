using KuberManager.Service.Application.DTOs;
using KuberManager.Service.Domain;

namespace KuberManager.Service.Application;

public interface ISecretManager
{
    Task<IReadOnlyList<SecretDto>> ListAsync(string cluster, string ns, CancellationToken ct = default);
    Task<SecretDto> GetAsync(string cluster, string ns, string name, CancellationToken ct = default);
    Task<OperationResult> CreateAsync(CreateSecretDto dto, CancellationToken ct = default);
    Task<OperationResult> DeleteAsync(string cluster, string ns, string name, string requestedBy, string reason, string correlationId, CancellationToken ct = default);
}
