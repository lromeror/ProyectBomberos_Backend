namespace BomberosAPI.Infrastructure.Services;

/// <summary>
/// Envío de correo vía SMTP con una cuenta de Gmail normal — a diferencia de un
/// servicio de correo transaccional (Resend, SendGrid, etc.), no exige verificar un
/// dominio propio: al ser tu propia cuenta autenticada, puede mandarle a cualquier
/// destinatario sin restricción de sandbox. Requiere una "contraseña de aplicación"
/// de Google (no la contraseña normal de la cuenta), generada con la verificación en
/// dos pasos activada: https://myaccount.google.com/apppasswords
/// </summary>
public class SmtpSettings
{
    public const string SectionName = "Smtp";

    public string Host { get; set; } = "smtp.gmail.com";
    public int Port { get; set; } = 587;
    public string Username { get; set; } = null!;
    public string Password { get; set; } = null!;
    public string FromEmail { get; set; } = null!;
    public string FromName { get; set; } = "FireHealth App";
}
