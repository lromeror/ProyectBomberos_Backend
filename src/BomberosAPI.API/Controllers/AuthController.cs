using BomberosAPI.API.Common.Responses;
using BomberosAPI.Application.Common.Interfaces;
using BomberosAPI.Application.Features.Auth;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace BomberosAPI.API.Controllers;

[ApiController]
[Route("api/auth")]
public class AuthController : ControllerBase
{
    private readonly AuthService _authService;
    private readonly AuthSessionService _authSessionService;
    private readonly ICurrentUserService _currentUser;

    public AuthController(AuthService authService, AuthSessionService authSessionService, ICurrentUserService currentUser)
    {
        _authService = authService;
        _authSessionService = authSessionService;
        _currentUser = currentUser;
    }

    /// <summary>Autentica un usuario y retorna un JWT firmado.</summary>
    [HttpPost("login")]
    [ProducesResponseType(typeof(ApiResponse<LoginResult>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse<object?>), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ApiResponse<object?>), StatusCodes.Status401Unauthorized)]
    public async Task<IActionResult> Login([FromBody] LoginRequest request, CancellationToken ct)
    {
        var ip = HttpContext.Connection.RemoteIpAddress?.ToString();
        var userAgent = Request.Headers.UserAgent.ToString();
        var result = await _authService.LoginAsync(request, ct, ip, string.IsNullOrWhiteSpace(userAgent) ? null : userAgent);
        return Ok(ApiResponse<LoginResult>.Ok(result, "Login successful."));
    }

    /// <summary>Cierra las sesiones activas registradas del usuario autenticado.</summary>
    [HttpPost("logout")]
    [Authorize]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    public async Task<IActionResult> Logout(CancellationToken ct)
    {
        await _authSessionService.CloseAllForUserAsync(_currentUser.UserId, ct);
        return NoContent();
    }

    /// <summary>Retorna la identidad del usuario autenticado desde el token JWT.</summary>
    [HttpGet("me")]
    [Authorize]
    [ProducesResponseType(typeof(ApiResponse<CurrentUserResponse>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse<object?>), StatusCodes.Status401Unauthorized)]
    public IActionResult Me()
    {
        var response = new CurrentUserResponse(
            _currentUser.UserId,
            _currentUser.Email,
            _currentUser.FirstName,
            _currentUser.LastName,
            _currentUser.Roles);

        return Ok(ApiResponse<CurrentUserResponse>.Ok(response));
    }

    /// <summary>Genera un token de restablecimiento de contraseña para el email indicado.</summary>
    [HttpPost("forgot-password")]
    [ProducesResponseType(typeof(ApiResponse<ForgotPasswordResult>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse<object?>), StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> ForgotPassword([FromBody] ForgotPasswordRequest request, CancellationToken ct)
    {
        var result = await _authService.RequestPasswordResetAsync(request, ct);
        return Ok(ApiResponse<ForgotPasswordResult>.Ok(result, "Si el correo está registrado, recibirás las instrucciones."));
    }

    /// <summary>Restablece la contraseña usando el token generado previamente.</summary>
    [HttpPost("reset-password")]
    [ProducesResponseType(typeof(ApiResponse<object?>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse<object?>), StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> ResetPassword([FromBody] ResetPasswordRequest request, CancellationToken ct)
    {
        await _authService.ResetPasswordAsync(request, ct);
        return Ok(ApiResponse<object?>.Ok(null, "Contraseña actualizada exitosamente."));
    }

    /// <summary>Cambia la contraseña del usuario autenticado (requiere la contraseña actual).</summary>
    [HttpPost("change-password")]
    [Authorize]
    [ProducesResponseType(typeof(ApiResponse<object?>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse<object?>), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ApiResponse<object?>), StatusCodes.Status401Unauthorized)]
    public async Task<IActionResult> ChangePassword([FromBody] ChangePasswordRequest request, CancellationToken ct)
    {
        await _authService.ChangePasswordAsync(_currentUser.UserId, request, ct);
        return Ok(ApiResponse<object?>.Ok(null, "Contraseña actualizada exitosamente."));
    }
}

public record CurrentUserResponse(
    Guid UserId,
    string Email,
    string FirstName,
    string LastName,
    IReadOnlyList<string> Roles
);
