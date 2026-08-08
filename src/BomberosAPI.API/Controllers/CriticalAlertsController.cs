using BomberosAPI.API.Common.Responses;
using BomberosAPI.Application.Common.Constants;
using BomberosAPI.Application.Common.Interfaces;
using BomberosAPI.Application.Features.CriticalAlerts;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace BomberosAPI.API.Controllers;

[ApiController]
[Route("api/critical-alerts")]
[Authorize]
public class CriticalAlertsController : ControllerBase
{
    // Mismo criterio ya usado en SymptomReports/EnvironmentalData: personal médico
    // puede generar/atender la alerta; capacitadores y jefes de bomberos (que están
    // presentes en la sesión) necesitan al menos verla.
    private const string ManageRoles = Roles.Medical + "," + Roles.Admin + "," + Roles.SystemAdmin;
    private const string ViewRoles = ManageRoles + "," + Roles.Capacitator + "," + Roles.FireChief;

    private readonly CriticalAlertService _service;
    private readonly ICurrentUserService _currentUser;

    public CriticalAlertsController(CriticalAlertService service, ICurrentUserService currentUser)
    {
        _service = service;
        _currentUser = currentUser;
    }

    [HttpPost]
    [Authorize(Roles = ManageRoles)]
    [ProducesResponseType(typeof(ApiResponse<CriticalAlertDto>), StatusCodes.Status201Created)]
    public async Task<IActionResult> Create([FromBody] CreateCriticalAlertRequest request, CancellationToken ct)
    {
        var alert = await _service.CreateAsync(request, ct);
        return CreatedAtAction(nameof(GetAll), null, ApiResponse<CriticalAlertDto>.Created(alert));
    }

    [HttpGet]
    [Authorize(Roles = ViewRoles)]
    [ProducesResponseType(typeof(ApiResponse<IReadOnlyList<CriticalAlertDto>>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetAll([FromQuery] string? status, CancellationToken ct)
    {
        var items = await _service.GetAllAsync(status, ct);
        return Ok(ApiResponse<IReadOnlyList<CriticalAlertDto>>.Ok(items));
    }

    [HttpPatch("{id:guid}/attend")]
    [Authorize(Roles = ManageRoles)]
    [ProducesResponseType(typeof(ApiResponse<CriticalAlertDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse<object?>), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(ApiResponse<object?>), StatusCodes.Status409Conflict)]
    public async Task<IActionResult> Attend(Guid id, CancellationToken ct)
    {
        var alert = await _service.AttendAsync(id, _currentUser.UserId, ct);
        return Ok(ApiResponse<CriticalAlertDto>.Ok(alert));
    }
}
