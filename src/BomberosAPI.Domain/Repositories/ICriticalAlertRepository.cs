using BomberosAPI.Domain.Entities;

namespace BomberosAPI.Domain.Repositories;

public interface ICriticalAlertRepository
{
    Task<IReadOnlyList<CriticalAlert>> GetAllAsync(string? status, CancellationToken cancellationToken = default);
    Task<CriticalAlert?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);
    Task AddAsync(CriticalAlert alert, CancellationToken cancellationToken = default);
    Task UpdateAsync(CriticalAlert alert, CancellationToken cancellationToken = default);
}
