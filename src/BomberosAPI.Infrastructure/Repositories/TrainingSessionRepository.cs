using BomberosAPI.Domain.Entities;
using BomberosAPI.Domain.Repositories;
using BomberosAPI.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace BomberosAPI.Infrastructure.Repositories;

public class TrainingSessionRepository : ITrainingSessionRepository
{
    private readonly AppDbContext _db;

    public TrainingSessionRepository(AppDbContext db) => _db = db;

    public async Task<IEnumerable<TrainingSession>> GetAllAsync(CancellationToken cancellationToken = default) =>
        await _db.TrainingSessions.AsNoTracking().ToListAsync(cancellationToken);

    public Task<TrainingSession?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default) =>
        _db.TrainingSessions.FirstOrDefaultAsync(s => s.TrainingSessionId == id, cancellationToken);

    public async Task<IEnumerable<TrainingSession>> GetByInstitutionAsync(Guid institutionId, CancellationToken cancellationToken = default) =>
        await _db.TrainingSessions.AsNoTracking().Where(s => s.InstitutionId == institutionId).ToListAsync(cancellationToken);

    public Task<TrainingSession?> GetBySessionCodeAsync(string sessionCode, CancellationToken cancellationToken = default) =>
        _db.TrainingSessions.AsNoTracking().FirstOrDefaultAsync(s => s.SessionCode == sessionCode, cancellationToken);

    public async Task AddAsync(TrainingSession entity, CancellationToken cancellationToken = default)
    {
        _db.TrainingSessions.Add(entity);
        await _db.SaveChangesAsync(cancellationToken);
    }

    public void Update(TrainingSession entity) => _db.TrainingSessions.Update(entity);

    public async Task UpdateAsync(TrainingSession entity, CancellationToken cancellationToken = default)
    {
        _db.TrainingSessions.Update(entity);
        await _db.SaveChangesAsync(cancellationToken);
    }

    public void Delete(TrainingSession entity) => _db.TrainingSessions.Remove(entity);
}
