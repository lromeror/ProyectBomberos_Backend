using BomberosAPI.Domain.Entities;
using BomberosAPI.Domain.Repositories;
using BomberosAPI.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace BomberosAPI.Infrastructure.Repositories;

public class AccessAuditRepository : IAccessAuditRepository
{
    private readonly AppDbContext _db;

    public AccessAuditRepository(AppDbContext db) => _db = db;

    public async Task<IReadOnlyList<AccessAudit>> GetRecentAsync(string? resourceType, int take, CancellationToken ct = default)
    {
        var query = _db.AccessAudits.AsNoTracking().AsQueryable();

        if (!string.IsNullOrWhiteSpace(resourceType))
            query = query.Where(a => a.ResourceType == resourceType);

        return await query
            .OrderByDescending(a => a.OccurredAt)
            .Take(take)
            .ToListAsync(ct);
    }

    public async Task AddAsync(AccessAudit entry, CancellationToken ct = default)
    {
        _db.AccessAudits.Add(entry);
        await _db.SaveChangesAsync(ct);
    }
}
