using BomberosAPI.Domain.Entities;

namespace BomberosAPI.Domain.Repositories;

/// <summary>
/// Repository for the data-change audit log (who modified which record, and its
/// before/after values) — complements IAccessAuditRepository, which tracks reads.
/// </summary>
public interface IChangeAuditRepository
{
    /// Most recent entries first, optionally filtered by entity name, capped at `take`.
    Task<IReadOnlyList<ChangeAudit>> GetRecentAsync(string? entity, int take, CancellationToken cancellationToken = default);
    Task AddAsync(ChangeAudit entry, CancellationToken cancellationToken = default);
}
