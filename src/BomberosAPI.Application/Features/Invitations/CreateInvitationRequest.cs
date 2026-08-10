namespace BomberosAPI.Application.Features.Invitations;

public record CreateInvitationRequest(
    Guid? TargetUserId,
    Guid? TrainingSessionId,
    // Código de rol (ej. "MEDICAL", "FIREFIGHTER_TRAINEE") en vez de un Guid — el
    // frontend ya trabaja con códigos de rol en todos lados (ver /users/{id}/roles),
    // así que no necesita resolverlos a un id él mismo. Solo tiene efecto cuando
    // TargetUserId es null: es lo que RegistrationService.CompleteRegistrationAsync usa
    // para saber qué tipo de cuenta crear cuando la invitación se acepta.
    string? TargetRoleCode,
    string TargetEmail,
    DateTime ExpiresAt
);