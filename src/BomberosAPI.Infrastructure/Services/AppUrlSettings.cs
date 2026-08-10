namespace BomberosAPI.Infrastructure.Services;

/// <summary>URL base de la app web — usada para armar los enlaces de los correos (invitación, activación, reseteo de contraseña).</summary>
public class AppUrlSettings
{
    public const string SectionName = "App";

    public string WebBaseUrl { get; set; } = null!;
}
