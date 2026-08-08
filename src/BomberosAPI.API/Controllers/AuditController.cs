using BomberosAPI.API.Common.Responses;
using BomberosAPI.Application.Common.Constants;
using BomberosAPI.Application.Common.Interfaces;
using BomberosAPI.Application.Features.Audit;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace BomberosAPI.API.Controllers;

[ApiController]
[Route("api/audit")]
[Authorize]
public class AuditController : ControllerBase
{
    private readonly AccessAuditService _service;
    private readonly ChangeAuditService _changeAuditService;
    private readonly ICurrentUserService _currentUser;

    public AuditController(AccessAuditService service, ChangeAuditService changeAuditService, ICurrentUserService currentUser)
    {
        _service = service;
        _changeAuditService = changeAuditService;
        _currentUser = currentUser;
    }

    /// <summary>Registra un evento de acceso a un recurso (llamado por useAuditTrail en el frontend).</summary>
    [HttpPost]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    public async Task<IActionResult> Log([FromBody] LogAccessAuditRequest request, CancellationToken ct)
    {
        await _service.LogAsync(_currentUser.UserId, request, ct);
        return NoContent();
    }

    /// <summary>Lista los eventos de auditoría más recientes. Solo SYSTEM_ADMIN.</summary>
    [HttpGet]
    [Authorize(Roles = Roles.SystemAdmin)]
    [ProducesResponseType(typeof(ApiResponse<IReadOnlyList<AccessAuditDto>>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetAll([FromQuery] string? resourceType, CancellationToken ct)
    {
        var items = await _service.GetRecentAsync(resourceType, ct);
        return Ok(ApiResponse<IReadOnlyList<AccessAuditDto>>.Ok(items));
    }

    /// <summary>Lista los cambios de datos más recientes (quién modificó qué y a qué valor). Solo SYSTEM_ADMIN.</summary>
    [HttpGet("changes")]
    [Authorize(Roles = Roles.SystemAdmin)]
    [ProducesResponseType(typeof(ApiResponse<IReadOnlyList<ChangeAuditDto>>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetChanges([FromQuery] string? entity, CancellationToken ct)
    {
        var items = await _changeAuditService.GetRecentAsync(entity, ct);
        return Ok(ApiResponse<IReadOnlyList<ChangeAuditDto>>.Ok(items));
    }
}
