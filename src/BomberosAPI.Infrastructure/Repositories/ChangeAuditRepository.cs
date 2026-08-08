using BomberosAPI.Domain.Entities;
using BomberosAPI.Domain.Repositories;
using BomberosAPI.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace BomberosAPI.Infrastructure.Repositories;

public class ChangeAuditRepository : IChangeAuditRepository
{
    private readonly AppDbContext _db;

    public ChangeAuditRepository(AppDbContext db) => _db = db;

    public async Task<IReadOnlyList<ChangeAudit>> GetRecentAsync(string? entity, int take, CancellationToken ct = default)
    {
        var query = _db.ChangeAudits.AsNoTracking().AsQueryable();

        if (!string.IsNullOrWhiteSpace(entity))
            query = query.Where(a => a.Entity == entity);

        return await query
            .OrderByDescending(a => a.OccurredAt)
            .Take(take)
            .ToListAsync(ct);
    }

    public async Task AddAsync(ChangeAudit entry, CancellationToken ct = default)
    {
        _db.ChangeAudits.Add(entry);
        await _db.SaveChangesAsync(ct);
    }
}
