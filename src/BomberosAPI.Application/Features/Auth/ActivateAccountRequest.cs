namespace BomberosAPI.Application.Features.Auth;

/// <summary>Completa la primera contraseña de una cuenta creada directamente (ej. "Agregar Personal").</summary>
public record ActivateAccountRequest(string Token, string Password);
