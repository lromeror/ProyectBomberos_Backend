namespace BomberosAPI.Application.Features.Audit;

public record AccessAuditDto(
    Guid AccessAuditId,
    Guid UserId,
    string? ResourceType,
    Guid? ResourceId,
    string Event,
    bool Success,
    DateTime OccurredAt
);
