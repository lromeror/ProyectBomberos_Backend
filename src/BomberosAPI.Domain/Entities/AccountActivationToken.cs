namespace BomberosAPI.Domain.Entities;

/// <summary>
/// Token para que una cuenta creada directamente (ej. "Agregar Personal") active su
/// contraseña por primera vez — sin este token, esas cuentas quedaban sin ninguna
/// credencial y sin forma de iniciar sesión ni de recuperar contraseña.
/// </summary>
public class AccountActivationToken
{
    public Guid AccountActivationTokenId { get; set; }
    public Guid UserId { get; set; }
    public string TokenHash { get; set; } = null!;
    public string Status { get; set; } = "pending";
    public DateTime ExpiresAt { get; set; }
    public DateTime? UsedAt { get; set; }
    public DateTime CreatedAt { get; set; }
}
