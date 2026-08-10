namespace BomberosAPI.Application.Features.Auth;

/// <summary>
/// Completa el registro de una cuenta nueva a partir de una invitación por correo.
/// Los campos de aspirante/personal médico solo son obligatorios según el rol de la
/// invitación (ver RegistrationService.CompleteRegistrationAsync).
/// </summary>
public record CompleteRegistrationRequest(
    string Token,
    string FirstName,
    string LastName,
    string? Phone,
    string Password,
    // Aspirante a bombero (rol FIREFIGHTER_TRAINEE)
    string? ApplicantCode,
    DateOnly? BirthDate,
    string? Sex,
    string? BloodType,
    string? EmergencyContactName,
    string? EmergencyContactPhone,
    // Personal médico (rol MEDICAL)
    string? Profession,
    string? Specialty,
    string? LicenseNumber
);

public record InvitationPreviewResult(string TargetEmail, string? RoleCode);
