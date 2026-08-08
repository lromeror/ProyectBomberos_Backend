using BomberosAPI.Domain.Entities;
using BomberosAPI.Domain.Repositories;
using BomberosAPI.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace BomberosAPI.Infrastructure.Repositories;

public class DSARRequestRepository : IDSARRequestRepository
{
    private readonly AppDbContext _db;

    public DSARRequestRepository(AppDbContext db) => _db = db;

    public async Task<IReadOnlyList<DSARRequest>> GetAllAsync(CancellationToken ct = default) =>
        await _db.DSARRequests.AsNoTracking().OrderByDescending(r => r.RequestedAt).ToListAsync(ct);

    public async Task<IReadOnlyList<DSARRequest>> GetByUserIdAsync(Guid userId, CancellationToken ct = default) =>
        await _db.DSARRequests.AsNoTracking()
            .Where(r => r.UserId == userId)
            .OrderByDescending(r => r.RequestedAt)
            .ToListAsync(ct);

    public Task<DSARRequest?> GetByIdAsync(Guid id, CancellationToken ct = default) =>
        _db.DSARRequests.FirstOrDefaultAsync(r => r.DSARRequestId == id, ct);

    public async Task AddAsync(DSARRequest request, CancellationToken ct = default)
    {
        _db.DSARRequests.Add(request);
        await _db.SaveChangesAsync(ct);
    }

    public async Task UpdateAsync(DSARRequest request, CancellationToken ct = default)
    {
        _db.DSARRequests.Update(request);
        await _db.SaveChangesAsync(ct);
    }
}
