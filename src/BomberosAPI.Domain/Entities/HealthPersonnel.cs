namespace BomberosAPI.Domain.Entities;

public class HealthPersonnel
{
    public Guid HealthPersonnelId { get; set; }
    public Guid UserId { get; set; }
    public string? Profession { get; set; }
    public string? Specialty { get; set; }
    public string? LicenseNumber { get; set; }
    public bool CanApproveDischarges { get; set; } = false;
    public bool IsActive { get; set; } = true;

    public User? User { get; set; }
}
