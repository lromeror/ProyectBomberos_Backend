namespace BomberosAPI.Application.Features.HealthPersonnel;

public record HealthPersonnelDto(
    Guid   HealthPersonnelId,
    Guid   UserId,
    string FirstName,
    string LastName,
    string Email,
    string? Phone,
    string? Profession,
    string? Specialty,
    string? LicenseNumber,
    bool   CanApproveDischarges,
    bool   IsActive
);