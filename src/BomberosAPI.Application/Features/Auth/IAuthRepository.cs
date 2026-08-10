using BomberosAPI.Domain.Entities;

namespace BomberosAPI.Application.Features.Auth;

public interface IAuthRepository
{
    Task<User?> FindUserByEmailAsync(string email, CancellationToken ct = default);
    Task<UserCredential?> FindCredentialByUserIdAsync(Guid userId, CancellationToken ct = default);
    Task<IReadOnlyList<string>> GetActiveRoleCodesByUserIdAsync(Guid userId, CancellationToken ct = default);
    Task UpdateCredentialAsync(UserCredential credential, CancellationToken ct = default);
    Task<PasswordResetToken?> FindValidResetTokenByHashAsync(string tokenHash, CancellationToken ct = default);
    Task AddPasswordResetTokenAsync(PasswordResetToken token, CancellationToken ct = default);
    Task UpdatePasswordResetTokenAsync(PasswordResetToken token, CancellationToken ct = default);

    Task<AccountActivationToken?> FindValidActivationTokenByHashAsync(string tokenHash, CancellationToken ct = default);
    Task AddActivationTokenAsync(AccountActivationToken token, CancellationToken ct = default);
    Task UpdateActivationTokenAsync(AccountActivationToken token, CancellationToken ct = default);
    Task AddCredentialAsync(UserCredential credential, CancellationToken ct = default);
    Task<User?> FindUserByIdAsync(Guid userId, CancellationToken ct = default);
    Task UpdateUserAsync(User user, CancellationToken ct = default);
}
