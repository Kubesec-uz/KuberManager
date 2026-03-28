using KuberManager.Service.Application.DTOs;
using KuberManager.Service.Domain;

namespace KuberManager.Service.Application;

public interface IPodManager
{
    Task<IReadOnlyList<PodInfoDto>> ListAsync(string cluster, string ns, string? appLabel = null, CancellationToken ct = default);
    Task<PodInfoDto> GetAsync(string cluster, string ns, string podName, CancellationToken ct = default);
    Task<string> GetLogsAsync(string cluster, string ns, string podName, string? container = null, int? tailLines = null, CancellationToken ct = default);
    Task<OperationResult> DeleteAsync(string cluster, string ns, string podName, string requestedBy, string reason, string correlationId, CancellationToken ct = default);
    IAsyncEnumerable<(string output, bool isStderr)> ExecAsync(string cluster, string ns, string podName, string container, string[] command, string requestedBy, string correlationId, CancellationToken ct = default);
}
