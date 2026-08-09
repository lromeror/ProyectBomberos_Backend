using BomberosAPI.Domain.Entities;
using BomberosAPI.Domain.Repositories;
using BomberosAPI.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace BomberosAPI.Infrastructure.Repositories;

public class InvitationRepository : IInvitationRepository
{
    private readonly AppDbContext _db;

    public InvitationRepository(AppDbContext db) => _db = db;

    public async Task<IEnumerable<Invitation>> GetAllAsync(CancellationToken ct = default) =>
        await _db.Invitations.AsNoTracking().ToListAsync(ct);

    public Task<Invitation?> GetByIdAsync(Guid id, CancellationToken ct = default) =>
        _db.Invitations.FirstOrDefaultAsync(i => i.InvitationId == id, ct);

    public Task<Invitation?> GetByTokenHashAsync(string tokenHash, CancellationToken ct = default) =>
        _db.Invitations.FirstOrDefaultAsync(i => i.InvitationTokenHash == tokenHash, ct);

    public async Task<IEnumerable<Invitation>> GetPendingBySessionAsync(Guid sessionId, CancellationToken ct = default) =>
        await _db.Invitations
            .AsNoTracking()
            .Where(i => i.TrainingSessionId == sessionId && i.Status == "Pending")
            .ToListAsync(ct);

    public async Task AddAsync(Invitation invitation, CancellationToken ct = default)
    {
        _db.Invitations.Add(invitation);
        await _db.SaveChangesAsync(ct);
    }

    public async Task UpdateAsync(Invitation invitation, CancellationToken ct = default)
    {
        _db.Invitations.Update(invitation);
        await _db.SaveChangesAsync(ct);
    }

    public void Update(Invitation invitation) => _db.Invitations.Update(invitation);

    public void Delete(Invitation invitation) => _db.Invitations.Remove(invitation);
}