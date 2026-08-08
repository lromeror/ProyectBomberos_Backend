using BomberosAPI.Domain.Entities;

namespace BomberosAPI.Domain.Repositories;

public interface IDSARRequestRepository
{
    Task<IReadOnlyList<DSARRequest>> GetAllAsync(CancellationToken cancellationToken = default);
    Task<IReadOnlyList<DSARRequest>> GetByUserIdAsync(Guid userId, CancellationToken cancellationToken = default);
    Task<DSARRequest?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);
    Task AddAsync(DSARRequest request, CancellationToken cancellationToken = default);
    Task UpdateAsync(DSARRequest request, CancellationToken cancellationToken = default);
}
