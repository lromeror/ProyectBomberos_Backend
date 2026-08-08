using BomberosAPI.API.Common.Responses;
using BomberosAPI.Application.Common.Constants;
using BomberosAPI.Application.Features.Auth;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace BomberosAPI.API.Controllers;

/// <summary>Visibilidad de sesiones de login (quién entró, desde dónde y cuándo). Solo SYSTEM_ADMIN.</summary>
[ApiController]
[Route("api/auth-sessions")]
[Authorize(Roles = Roles.SystemAdmin)]
public class AuthSessionsController : ControllerBase
{
    private readonly AuthSessionService _service;

    public AuthSessionsController(AuthSessionService service) => _service = service;

    [HttpGet]
    [ProducesResponseType(typeof(ApiResponse<IReadOnlyList<AuthSessionDto>>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetAll([FromQuery] Guid? userId, CancellationToken ct)
    {
        var items = await _service.GetRecentAsync(userId, ct);
        return Ok(ApiResponse<IReadOnlyList<AuthSessionDto>>.Ok(items));
    }
}
