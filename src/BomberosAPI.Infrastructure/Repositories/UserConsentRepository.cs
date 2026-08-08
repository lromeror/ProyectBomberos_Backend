using BomberosAPI.Domain.Entities;
using BomberosAPI.Domain.Repositories;
using BomberosAPI.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace BomberosAPI.Infrastructure.Repositories;

public class UserConsentRepository : IUserConsentRepository
{
    private readonly AppDbContext _db;

    public UserConsentRepository(AppDbContext db) => _db = db;

    public async Task<IReadOnlyList<UserConsent>> GetByUserIdAsync(Guid userId, CancellationToken ct = default) =>
        await _db.UserConsents
            .AsNoTracking()
            .Where(c => c.UserId == userId)
            .OrderByDescending(c => c.GrantedAt)
            .ToListAsync(ct);

    public Task<UserConsent?> GetActiveByUserAndDocumentAsync(Guid userId, Guid consentDocumentId, CancellationToken ct = default) =>
        _db.UserConsents.FirstOrDefaultAsync(
            c => c.UserId == userId && c.ConsentDocumentId == consentDocumentId && c.Status == "active", ct);

    public async Task AddAsync(UserConsent consent, CancellationToken ct = default)
    {
        _db.UserConsents.Add(consent);
        await _db.SaveChangesAsync(ct);
    }

    public async Task UpdateAsync(UserConsent consent, CancellationToken ct = default)
    {
        _db.UserConsents.Update(consent);
        await _db.SaveChangesAsync(ct);
    }
}
