using BomberosAPI.Domain.Entities;
using BomberosAPI.Domain.Repositories;
using BomberosAPI.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace BomberosAPI.Infrastructure.Repositories;

public class SymptomReportRepository : ISymptomReportRepository
{
    private readonly AppDbContext _db;

    public SymptomReportRepository(AppDbContext db) => _db = db;

    public async Task<IEnumerable<SymptomReport>> GetAllAsync(CancellationToken ct = default) =>
        await _db.SymptomReports.AsNoTracking().ToListAsync(ct);

    public Task<SymptomReport?> GetByIdAsync(Guid id, CancellationToken ct = default) =>
        _db.SymptomReports.FirstOrDefaultAsync(s => s.SymptomReportId == id, ct);

    // Ver el mismo razonamiento en VitalSignsMeasurementRepository: sin ORDER BY el
    // orden de retorno no está garantizado.
    public async Task<IEnumerable<SymptomReport>> GetByParticipantAsync(Guid sessionParticipantId, CancellationToken ct = default) =>
        await _db.SymptomReports
            .AsNoTracking()
            .Where(s => s.SessionParticipantId == sessionParticipantId)
            .OrderBy(s => s.ReportedAt)
            .ToListAsync(ct);

    public async Task<IEnumerable<SymptomReport>> GetByTraineeAsync(Guid traineeFirefighterId, CancellationToken ct = default) =>
        await (from s in _db.SymptomReports
               join sp in _db.SessionParticipants on s.SessionParticipantId equals sp.SessionParticipantId
               where sp.TraineeFirefighterId == traineeFirefighterId
               select s)
            .AsNoTracking()
            .OrderBy(s => s.ReportedAt)
            .ToListAsync(ct);

    public async Task AddAsync(SymptomReport report, CancellationToken ct = default)
    {
        _db.SymptomReports.Add(report);
        await _db.SaveChangesAsync(ct);
    }

    public async Task UpdateAsync(SymptomReport report, CancellationToken ct = default)
    {
        _db.SymptomReports.Update(report);
        await _db.SaveChangesAsync(ct);
    }

    public void Update(SymptomReport report) => _db.SymptomReports.Update(report);

    public void Delete(SymptomReport report) => _db.SymptomReports.Remove(report);
}
