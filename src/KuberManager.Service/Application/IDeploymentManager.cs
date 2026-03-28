using KuberManager.Service.Application.DTOs;
using KuberManager.Service.Domain;

namespace KuberManager.Service.Application;

public interface IDeploymentManager
{
    Task<DeploymentStatusDto> GetStatusAsync(string cluster, string ns, string name, CancellationToken ct = default);
    Task<IReadOnlyList<DeploymentStatusDto>> ListAsync(string cluster, string ns, CancellationToken ct = default);
    Task<OperationResult> CreateAsync(string cluster, string ns, CreateDeploymentDto dto, CancellationToken ct = default);
    Task<OperationResult> DeleteAsync(string cluster, string ns, string name, string requestedBy, string reason, string correlationId, CancellationToken ct = default);
    Task<OperationResult> ScaleAsync(string cluster, string ns, string name, int replicas, string requestedBy, string reason, string correlationId, CancellationToken ct = default);
    Task<OperationResult> RestartAsync(string cluster, string ns, string name, string requestedBy, string reason, string correlationId, CancellationToken ct = default);
}
