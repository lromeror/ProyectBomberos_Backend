using BomberosAPI.Domain.Entities;

namespace BomberosAPI.Domain.Repositories;

/// <summary>
/// Repository for login-session records (device/IP/user-agent per login, for
/// visibility into who's connected from where — not used for auth itself, which
/// stays a stateless JWT).
/// </summary>
public interface IAuthSessionRepository
{
    Task<IReadOnlyList<AuthSession>> GetActiveByUserIdAsync(Guid userId, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<AuthSession>> GetRecentAsync(Guid? userId, int take, CancellationToken cancellationToken = default);
    Task AddAsync(AuthSession session, CancellationToken cancellationToken = default);
    Task UpdateAsync(AuthSession session, CancellationToken cancellationToken = default);
}
