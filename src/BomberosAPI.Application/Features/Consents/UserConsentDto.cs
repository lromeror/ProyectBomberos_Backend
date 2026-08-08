namespace BomberosAPI.Application.Features.Consents;

public record UserConsentDto(
    Guid UserConsentId,
    Guid UserId,
    Guid InstitutionId,
    Guid ConsentDocumentId,
    string Status,
    DateTime GrantedAt,
    DateTime? RevokedAt,
    DateTime? ExpiresAt
);
