using KuberManager.Service.Application.DTOs;
using KuberManager.Service.Domain;

namespace KuberManager.Service.Application;

public interface IServiceManager
{
    Task<IReadOnlyList<ServiceDto>> ListAsync(string cluster, string ns, CancellationToken ct = default);
    Task<ServiceDto> GetAsync(string cluster, string ns, string name, CancellationToken ct = default);
    Task<OperationResult> CreateAsync(CreateServiceDto dto, CancellationToken ct = default);
    Task<OperationResult> DeleteAsync(string cluster, string ns, string name, string requestedBy, string reason, string correlationId, CancellationToken ct = default);
}
