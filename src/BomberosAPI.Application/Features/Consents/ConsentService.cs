using BomberosAPI.Application.Common.Exceptions;
using BomberosAPI.Domain.Repositories;
using ConsentDocumentEntity = BomberosAPI.Domain.Entities.ConsentDocument;
using UserConsentEntity = BomberosAPI.Domain.Entities.UserConsent;

namespace BomberosAPI.Application.Features.Consents;

public class ConsentService
{
    private readonly IConsentDocumentRepository _documentRepo;
    private readonly IUserConsentRepository _consentRepo;
    private readonly IUserRepository _userRepo;

    public ConsentService(IConsentDocumentRepository documentRepo, IUserConsentRepository consentRepo, IUserRepository userRepo)
    {
        _documentRepo = documentRepo;
        _consentRepo = consentRepo;
        _userRepo = userRepo;
    }

    public async Task<IReadOnlyList<ConsentDocumentDto>> GetActiveDocumentsAsync(CancellationToken ct = default)
    {
        var items = await _documentRepo.GetActiveAsync(ct);
        return items.Select(ToDto).ToList();
    }

    public async Task<ConsentDocumentDto> CreateDocumentAsync(CreateConsentDocumentRequest request, CancellationToken ct = default)
    {
        if (string.IsNullOrWhiteSpace(request.ConsentType) || string.IsNullOrWhiteSpace(request.Version))
            throw new BusinessRuleException("ConsentType and Version are required.");

        var document = new ConsentDocumentEntity
        {
            ConsentDocumentId = Guid.NewGuid(),
            ConsentType = request.ConsentType,
            Version = request.Version,
            TextContent = request.TextContent,
            ValidFrom = request.ValidFrom,
            ValidUntil = request.ValidUntil
        };

        await _documentRepo.AddAsync(document, ct);
        return ToDto(document);
    }

    public async Task<IReadOnlyList<UserConsentDto>> GetMineAsync(Guid userId, CancellationToken ct = default)
    {
        var items = await _consentRepo.GetByUserIdAsync(userId, ct);
        return items.Select(ToDto).ToList();
    }

    public async Task<UserConsentDto> GrantAsync(Guid userId, Guid consentDocumentId, CancellationToken ct = default)
    {
        var document = await _documentRepo.GetByIdAsync(consentDocumentId, ct)
            ?? throw new NotFoundException("ConsentDocument", consentDocumentId);

        var user = await _userRepo.GetByIdAsync(userId, ct)
            ?? throw new NotFoundException("User", userId);

        var existing = await _consentRepo.GetActiveByUserAndDocumentAsync(userId, consentDocumentId, ct);
        if (existing is not null)
            return ToDto(existing);

        var consent = new UserConsentEntity
        {
            UserConsentId = Guid.NewGuid(),
            UserId = userId,
            InstitutionId = user.InstitutionId,
            ConsentDocumentId = consentDocumentId,
            Status = "active",
            GrantedAt = DateTime.UtcNow,
            ExpiresAt = document.ValidUntil
        };

        await _consentRepo.AddAsync(consent, ct);
        return ToDto(consent);
    }

    public async Task RevokeAsync(Guid userId, Guid consentDocumentId, CancellationToken ct = default)
    {
        var existing = await _consentRepo.GetActiveByUserAndDocumentAsync(userId, consentDocumentId, ct)
            ?? throw new NotFoundException("Active consent for this document", consentDocumentId);

        existing.Status = "revoked";
        existing.RevokedAt = DateTime.UtcNow;
        await _consentRepo.UpdateAsync(existing, ct);
    }

    private static ConsentDocumentDto ToDto(ConsentDocumentEntity d) => new(
        d.ConsentDocumentId, d.ConsentType, d.Version, d.TextContent, d.ValidFrom, d.ValidUntil);

    private static UserConsentDto ToDto(UserConsentEntity c) => new(
        c.UserConsentId, c.UserId, c.InstitutionId, c.ConsentDocumentId, c.Status, c.GrantedAt, c.RevokedAt, c.ExpiresAt);
}
