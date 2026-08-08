using BomberosAPI.Domain.Entities;

namespace BomberosAPI.Domain.Repositories;

/// <summary>
/// Repository for bioimpedance measurements (weight, fat/muscle/water %, metabolic
/// age, and — for research sessions — lactate and Stroop test markers).
/// </summary>
public interface IBioimpedanceMeasurementRepository : IRepository<BioimpedanceMeasurement>
{
    Task<IEnumerable<BioimpedanceMeasurement>> GetByParticipantAsync(Guid sessionParticipantId, CancellationToken cancellationToken = default);

    /// Returns all measurements for a trainee (joined through SessionParticipant).
    Task<IEnumerable<BioimpedanceMeasurement>> GetByTraineeAsync(Guid traineeFirefighterId, CancellationToken cancellationToken = default);
}
