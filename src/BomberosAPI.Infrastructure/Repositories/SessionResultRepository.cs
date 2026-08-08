using BomberosAPI.Domain.Entities;
using BomberosAPI.Domain.Repositories;
using BomberosAPI.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace BomberosAPI.Infrastructure.Repositories;

public class SessionResultRepository : ISessionResultRepository
{
    private readonly AppDbContext _db;

    public SessionResultRepository(AppDbContext db) => _db = db;

    public async Task<IReadOnlyList<SessionResult>> GetByParticipantAsync(Guid sessionParticipantId, CancellationToken ct = default) =>
        await _db.SessionResults.AsNoTracking()
            .Where(r => r.SessionParticipantId == sessionParticipantId)
            .OrderByDescending(r => r.GeneratedAt)
            .ToListAsync(ct);

    public Task<SessionResult?> GetByIdAsync(Guid id, CancellationToken ct = default) =>
        _db.SessionResults.FirstOrDefaultAsync(r => r.SessionResultId == id, ct);

    public async Task AddAsync(SessionResult result, CancellationToken ct = default)
    {
        _db.SessionResults.Add(result);
        await _db.SaveChangesAsync(ct);
    }
}
