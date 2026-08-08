using BomberosAPI.Domain.Entities;

namespace BomberosAPI.Domain.Repositories;

public interface IConsentDocumentRepository
{
    Task<IReadOnlyList<ConsentDocument>> GetActiveAsync(CancellationToken cancellationToken = default);
    Task<ConsentDocument?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);
    Task AddAsync(ConsentDocument document, CancellationToken cancellationToken = default);
}
