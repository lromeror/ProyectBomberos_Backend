using BomberosAPI.Domain.Entities;
using BomberosAPI.Domain.Repositories;
using BomberosAPI.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace BomberosAPI.Infrastructure.Repositories;

public class BioimpedanceMeasurementRepository : IBioimpedanceMeasurementRepository
{
    private readonly AppDbContext _db;

    public BioimpedanceMeasurementRepository(AppDbContext db) => _db = db;

    public async Task<IEnumerable<BioimpedanceMeasurement>> GetAllAsync(CancellationToken ct = default) =>
        await _db.BioimpedanceMeasurements.AsNoTracking().ToListAsync(ct);

    public Task<BioimpedanceMeasurement?> GetByIdAsync(Guid id, CancellationToken ct = default) =>
        _db.BioimpedanceMeasurements.FirstOrDefaultAsync(b => b.BioimpedanceMeasurementId == id, ct);

    // Ver el mismo razonamiento en VitalSignsMeasurementRepository: sin ORDER BY el
    // orden de retorno no está garantizado.
    public async Task<IEnumerable<BioimpedanceMeasurement>> GetByParticipantAsync(Guid sessionParticipantId, CancellationToken ct = default) =>
        await _db.BioimpedanceMeasurements
            .AsNoTracking()
            .Where(b => b.SessionParticipantId == sessionParticipantId)
            .OrderBy(b => b.TakenAt)
            .ToListAsync(ct);

    public async Task<IEnumerable<BioimpedanceMeasurement>> GetByTraineeAsync(Guid traineeFirefighterId, CancellationToken ct = default) =>
        await (from b in _db.BioimpedanceMeasurements
               join sp in _db.SessionParticipants on b.SessionParticipantId equals sp.SessionParticipantId
               where sp.TraineeFirefighterId == traineeFirefighterId
               select b)
            .AsNoTracking()
            .OrderBy(b => b.TakenAt)
            .ToListAsync(ct);

    public async Task AddAsync(BioimpedanceMeasurement measurement, CancellationToken ct = default)
    {
        _db.BioimpedanceMeasurements.Add(measurement);
        await _db.SaveChangesAsync(ct);
    }

    public async Task UpdateAsync(BioimpedanceMeasurement measurement, CancellationToken ct = default)
    {
        _db.BioimpedanceMeasurements.Update(measurement);
        await _db.SaveChangesAsync(ct);
    }

    public void Update(BioimpedanceMeasurement measurement) => _db.BioimpedanceMeasurements.Update(measurement);

    public void Delete(BioimpedanceMeasurement measurement) => _db.BioimpedanceMeasurements.Remove(measurement);
}
