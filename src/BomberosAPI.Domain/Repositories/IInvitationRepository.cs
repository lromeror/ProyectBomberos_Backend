using BomberosAPI.Domain.Entities;

namespace BomberosAPI.Domain.Repositories;

/// <summary>
/// Repository for invitations to the system or to specific sessions.
/// </summary>
public interface IInvitationRepository : IRepository<Invitation>
{
    /// Finds an invitation by the hash of its token (used to validate acceptance).
    Task<Invitation?> GetByTokenHashAsync(string tokenHash, CancellationToken cancellationToken = default);

    /// Lists the pending invitations of a specific session.
    Task<IEnumerable<Invitation>> GetPendingBySessionAsync(Guid sessionId, CancellationToken cancellationToken = default);
}