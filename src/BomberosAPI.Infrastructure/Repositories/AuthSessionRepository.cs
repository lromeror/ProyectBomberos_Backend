using BomberosAPI.Domain.Entities;
using BomberosAPI.Domain.Repositories;
using BomberosAPI.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace BomberosAPI.Infrastructure.Repositories;

public class AuthSessionRepository : IAuthSessionRepository
{
    private readonly AppDbContext _db;

    public AuthSessionRepository(AppDbContext db) => _db = db;

    public async Task<IReadOnlyList<AuthSession>> GetActiveByUserIdAsync(Guid userId, CancellationToken ct = default) =>
        await _db.AuthSessions
            .Where(s => s.UserId == userId && s.Status == "Active")
            .ToListAsync(ct);

    public async Task<IReadOnlyList<AuthSession>> GetRecentAsync(Guid? userId, int take, CancellationToken ct = default)
    {
        var query = _db.AuthSessions.AsNoTracking().AsQueryable();

        if (userId is not null)
            query = query.Where(s => s.UserId == userId);

        return await query
            .OrderByDescending(s => s.CreatedAt)
            .Take(take)
            .ToListAsync(ct);
    }

    public async Task AddAsync(AuthSession session, CancellationToken ct = default)
    {
        _db.AuthSessions.Add(session);
        await _db.SaveChangesAsync(ct);
    }

    public async Task UpdateAsync(AuthSession session, CancellationToken ct = default)
    {
        _db.AuthSessions.Update(session);
        await _db.SaveChangesAsync(ct);
    }
}
