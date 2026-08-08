namespace BomberosAPI.Application.Features.Audit;

public record ChangeAuditDto(
    Guid ChangeAuditId,
    Guid ActorUserId,
    string Entity,
    Guid EntityId,
    string Operation,
    string? PreviousValuesJson,
    string? NewValuesJson,
    DateTime OccurredAt
);
