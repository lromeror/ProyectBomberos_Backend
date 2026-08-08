using BomberosAPI.Domain.Entities;

namespace BomberosAPI.Domain.Repositories;

/// <summary>
/// Repository for symptom reports raised during a training session.
/// </summary>
public interface ISymptomReportRepository : IRepository<SymptomReport>
{
    Task<IEnumerable<SymptomReport>> GetByParticipantAsync(Guid sessionParticipantId, CancellationToken cancellationToken = default);

    /// Returns all symptom reports for a trainee (joined through SessionParticipant).
    Task<IEnumerable<SymptomReport>> GetByTraineeAsync(Guid traineeFirefighterId, CancellationToken cancellationToken = default);
}
