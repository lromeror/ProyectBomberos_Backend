using BomberosAPI.Domain.Entities;
using BomberosAPI.Domain.Repositories;
using BomberosAPI.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace BomberosAPI.Infrastructure.Repositories;

public class CriticalAlertRepository : ICriticalAlertRepository
{
    private readonly AppDbContext _db;

    public CriticalAlertRepository(AppDbContext db) => _db = db;

    public async Task<IReadOnlyList<CriticalAlert>> GetAllAsync(string? status, CancellationToken ct = default)
    {
        var query = _db.CriticalAlerts.AsNoTracking().AsQueryable();

        if (!string.IsNullOrWhiteSpace(status))
            query = query.Where(a => a.Status == status);

        return await query.OrderByDescending(a => a.GeneratedAt).ToListAsync(ct);
    }

    public Task<CriticalAlert?> GetByIdAsync(Guid id, CancellationToken ct = default) =>
        _db.CriticalAlerts.FirstOrDefaultAsync(a => a.CriticalAlertId == id, ct);

    public async Task AddAsync(CriticalAlert alert, CancellationToken ct = default)
    {
        _db.CriticalAlerts.Add(alert);
        await _db.SaveChangesAsync(ct);
    }

    public async Task UpdateAsync(CriticalAlert alert, CancellationToken ct = default)
    {
        _db.CriticalAlerts.Update(alert);
        await _db.SaveChangesAsync(ct);
    }
}
