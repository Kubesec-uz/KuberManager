using KuberManager.Service.Application.DTOs;
using KuberManager.Service.Domain;

namespace KuberManager.Service.Application;

public interface IConfigMapManager
{
    Task<IReadOnlyList<ConfigMapDto>> ListAsync(string cluster, string ns, CancellationToken ct = default);
    Task<ConfigMapDto> GetAsync(string cluster, string ns, string name, CancellationToken ct = default);
    Task<OperationResult> CreateAsync(string cluster, string ns, string name, Dictionary<string, string> data, string requestedBy, string reason, string correlationId, CancellationToken ct = default);
    Task<OperationResult> UpdateAsync(string cluster, string ns, string name, Dictionary<string, string> data, string requestedBy, string reason, string correlationId, CancellationToken ct = default);
    Task<OperationResult> DeleteAsync(string cluster, string ns, string name, string requestedBy, string reason, string correlationId, CancellationToken ct = default);
}
