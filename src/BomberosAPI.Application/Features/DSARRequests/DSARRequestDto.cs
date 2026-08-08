namespace BomberosAPI.Application.Features.DSARRequests;

public record DSARRequestDto(
    Guid DSARRequestId,
    Guid UserId,
    Guid? ManagedByUserId,
    string RightType,
    string Status,
    string? Description,
    string? Response,
    DateTime RequestedAt,
    DateTime? RespondedAt,
    DateTime? LegalDeadlineAt
);

public record CreateDSARRequestRequest(string RightType, string? Description);

public record RespondDSARRequestRequest(string Status, string? Response);
