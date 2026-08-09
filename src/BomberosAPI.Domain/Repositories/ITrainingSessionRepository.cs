using BomberosAPI.Domain.Entities;

namespace BomberosAPI.Domain.Repositories;

/// <summary>
/// Repository for training sessions.
/// </summary>
public interface ITrainingSessionRepository : IRepository<TrainingSession>
{
    /// Lists the sessions of a given institution.
    Task<IEnumerable<TrainingSession>> GetByInstitutionAsync(Guid institutionId, CancellationToken cancellationToken = default);

    /// Finds a session by its unique session code.
    Task<TrainingSession?> GetBySessionCodeAsync(string sessionCode, CancellationToken cancellationToken = default);
}