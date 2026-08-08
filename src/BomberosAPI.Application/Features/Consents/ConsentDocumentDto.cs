namespace BomberosAPI.Application.Features.Consents;

public record ConsentDocumentDto(
    Guid ConsentDocumentId,
    string ConsentType,
    string Version,
    string? TextContent,
    DateTime ValidFrom,
    DateTime? ValidUntil
);

public record CreateConsentDocumentRequest(
    string ConsentType,
    string Version,
    string? TextContent,
    DateTime ValidFrom,
    DateTime? ValidUntil
);
