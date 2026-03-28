using KuberManager.Service.Application.DTOs;
using KuberManager.Service.Domain;

namespace KuberManager.Service.Application;

public interface INamespaceManager
{
    Task<IReadOnlyList<NamespaceInfoDto>> ListAsync(string cluster, CancellationToken ct = default);
    Task<NamespaceInfoDto> GetAsync(string cluster, string name, CancellationToken ct = default);
    Task<OperationResult> CreateAsync(string cluster, string name, Dictionary<string, string> labels, string requestedBy, string reason, string correlationId, CancellationToken ct = default);
    Task<OperationResult> DeleteAsync(string cluster, string name, string requestedBy, string reason, string correlationId, CancellationToken ct = default);
}
