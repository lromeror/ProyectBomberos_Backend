using BomberosAPI.Application.Features.Auth;
using BomberosAPI.Domain.Entities;
using BomberosAPI.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace BomberosAPI.Infrastructure.Repositories;

public class AuthRepository : IAuthRepository
{
    private readonly AppDbContext _db;

    public AuthRepository(AppDbContext db) => _db = db;

    public Task<User?> FindUserByEmailAsync(string email, CancellationToken ct = default) =>
        _db.Users.AsNoTracking().FirstOrDefaultAsync(u => u.Email == email, ct);

    public Task<UserCredential?> FindCredentialByUserIdAsync(Guid userId, CancellationToken ct = default) =>
        _db.UserCredentials.AsNoTracking().FirstOrDefaultAsync(c => c.UserId == userId, ct);

    public async Task<IReadOnlyList<string>> GetActiveRoleCodesByUserIdAsync(Guid userId, CancellationToken ct = default)
    {
        var today = DateTime.UtcNow;
        return await _db.UserRoles
            .AsNoTracking()
            .Where(ur => ur.UserId == userId
                      && ur.IsActive
                      && ur.StartDate <= today
                      && (ur.EndDate == null || ur.EndDate >= today))
            .Join(_db.Roles, ur => ur.RoleId, r => r.RoleId, (_, r) => r.Code)
            .ToListAsync(ct);
    }

    public async Task UpdateCredentialAsync(UserCredential credential, CancellationToken ct = default)
    {
        _db.UserCredentials.Update(credential);
        await _db.SaveChangesAsync(ct);
    }

    public Task<PasswordResetToken?> FindValidResetTokenByHashAsync(string tokenHash, CancellationToken ct = default) =>
        _db.PasswordResetTokens
           .FirstOrDefaultAsync(t => t.TokenHash == tokenHash
                                  && t.Status == "pending"
                                  && t.ExpiresAt > DateTime.UtcNow, ct);

    public async Task AddPasswordResetTokenAsync(PasswordResetToken token, CancellationToken ct = default)
    {
        _db.PasswordResetTokens.Add(token);
        await _db.SaveChangesAsync(ct);
    }

    public async Task UpdatePasswordResetTokenAsync(PasswordResetToken token, CancellationToken ct = default)
    {
        _db.PasswordResetTokens.Update(token);
        await _db.SaveChangesAsync(ct);
    }

    public Task<AccountActivationToken?> FindValidActivationTokenByHashAsync(string tokenHash, CancellationToken ct = default) =>
        _db.AccountActivationTokens
           .FirstOrDefaultAsync(t => t.TokenHash == tokenHash
                                  && t.Status == "pending"
                                  && t.ExpiresAt > DateTime.UtcNow, ct);

    public async Task AddActivationTokenAsync(AccountActivationToken token, CancellationToken ct = default)
    {
        _db.AccountActivationTokens.Add(token);
        await _db.SaveChangesAsync(ct);
    }

    public async Task UpdateActivationTokenAsync(AccountActivationToken token, CancellationToken ct = default)
    {
        _db.AccountActivationTokens.Update(token);
        await _db.SaveChangesAsync(ct);
    }

    public async Task AddCredentialAsync(UserCredential credential, CancellationToken ct = default)
    {
        _db.UserCredentials.Add(credential);
        await _db.SaveChangesAsync(ct);
    }

    public Task<User?> FindUserByIdAsync(Guid userId, CancellationToken ct = default) =>
        _db.Users.FirstOrDefaultAsync(u => u.UserId == userId, ct);

    public async Task UpdateUserAsync(User user, CancellationToken ct = default)
    {
        _db.Users.Update(user);
        await _db.SaveChangesAsync(ct);
    }
}
