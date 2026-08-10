namespace BomberosAPI.Domain.Entities;

public class User
{
    public Guid UserId { get; set; }
    public Guid InstitutionId { get; set; }
    public string Email { get; set; } = null!;
    public string FirstName { get; set; } = null!;
    public string LastName { get; set; } = null!;
    public string? Phone { get; set; }
    public string AccountStatus { get; set; } = "active";
    public bool EmailVerified { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime? LastAccessAt { get; set; }
}
