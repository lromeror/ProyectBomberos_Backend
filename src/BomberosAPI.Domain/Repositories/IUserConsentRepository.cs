using BomberosAPI.Domain.Entities;

namespace BomberosAPI.Domain.Repositories;

public interface IUserConsentRepository
{
    Task<IReadOnlyList<UserConsent>> GetByUserIdAsync(Guid userId, CancellationToken cancellationToken = default);
    Task<UserConsent?> GetActiveByUserAndDocumentAsync(Guid userId, Guid consentDocumentId, CancellationToken cancellationToken = default);
    Task AddAsync(UserConsent consent, CancellationToken cancellationToken = default);
    Task UpdateAsync(UserConsent consent, CancellationToken cancellationToken = default);
}
