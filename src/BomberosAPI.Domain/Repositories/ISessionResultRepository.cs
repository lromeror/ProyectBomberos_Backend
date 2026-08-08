using BomberosAPI.Domain.Entities;

namespace BomberosAPI.Domain.Repositories;

public interface ISessionResultRepository
{
    Task<IReadOnlyList<SessionResult>> GetByParticipantAsync(Guid sessionParticipantId, CancellationToken cancellationToken = default);
    Task<SessionResult?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);
    Task AddAsync(SessionResult result, CancellationToken cancellationToken = default);
}
