namespace BomberosAPI.Application.Features.HealthPersonnel;

public record UpdateHealthPersonnelRequest(
    string? Profession,
    string? Specialty,
    string? LicenseNumber,
    bool CanApproveDischarges
);
