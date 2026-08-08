using BomberosAPI.Domain.Entities;

namespace BomberosAPI.Domain.Repositories;

/// <summary>
/// Repository for the data-access audit log (who accessed/changed what resource and when).
/// </summary>
public interface IAccessAuditRepository
{
    /// Most recent entries first, optionally filtered by resource type, capped at `take`.
    Task<IReadOnlyList<AccessAudit>> GetRecentAsync(string? resourceType, int take, CancellationToken cancellationToken = default);
    Task AddAsync(AccessAudit entry, CancellationToken cancellationToken = default);
}
