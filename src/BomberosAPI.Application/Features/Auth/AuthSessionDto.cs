namespace BomberosAPI.Application.Features.Auth;

public record AuthSessionDto(
    Guid AuthSessionId,
    Guid UserId,
    string? Device,
    string? Ip,
    string? UserAgent,
    string Status,
    DateTime CreatedAt,
    DateTime ExpiresAt,
    DateTime? ClosedAt
);
