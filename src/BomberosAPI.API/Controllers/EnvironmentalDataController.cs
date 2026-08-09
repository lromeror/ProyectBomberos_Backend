using BomberosAPI.API.Common.Responses;
using BomberosAPI.Application.Common.Constants;
using BomberosAPI.Application.Features.EnvironmentalData;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace BomberosAPI.API.Controllers;

[ApiController]
[Route("api/environmental-data")]
[Authorize]
public class EnvironmentalDataController : ControllerBase
{
    // Mismo set de roles que ya podía crear estos registros.
    private const string StaffRoles = Roles.Medical + "," + Roles.Admin + "," + Roles.SystemAdmin
        + "," + Roles.Capacitator + "," + Roles.FireChief;

    private readonly EnvironmentalDataService _service;

    public EnvironmentalDataController(EnvironmentalDataService service)
    {
        _service = service;
    }

    [HttpGet]
    [Authorize(Roles = StaffRoles)]
    [ProducesResponseType(typeof(ApiResponse<IReadOnlyList<EnvironmentalDataDto>>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetAll(CancellationToken ct)
    {
        var items = await _service.GetAllAsync(ct);
        return Ok(ApiResponse<IReadOnlyList<EnvironmentalDataDto>>.Ok(items));
    }

    [HttpGet("{id:guid}")]
    [Authorize(Roles = StaffRoles)]
    [ProducesResponseType(typeof(ApiResponse<EnvironmentalDataDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse<object?>), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetById(Guid id, CancellationToken ct)
    {
        var e = await _service.GetByIdAsync(id, ct);
        return Ok(ApiResponse<EnvironmentalDataDto>.Ok(e));
    }

    [HttpGet("by-session/{sessionId:guid}")]
    [Authorize(Roles = StaffRoles)]
    [ProducesResponseType(typeof(ApiResponse<IReadOnlyList<EnvironmentalDataDto>>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetBySession(Guid sessionId, CancellationToken ct)
    {
        var items = await _service.GetBySessionAsync(sessionId, ct);
        return Ok(ApiResponse<IReadOnlyList<EnvironmentalDataDto>>.Ok(items));
    }

    [HttpPost]
    [Authorize(Roles = Roles.Medical + "," + Roles.Admin + "," + Roles.SystemAdmin + "," + Roles.Capacitator + "," + Roles.FireChief)]
    [ProducesResponseType(typeof(ApiResponse<EnvironmentalDataDto>), StatusCodes.Status201Created)]
    [ProducesResponseType(typeof(ApiResponse<object?>), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ApiResponse<object?>), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Create([FromBody] CreateEnvironmentalDataRequest request, CancellationToken ct)
    {
        var e = await _service.CreateAsync(request, ct);
        return CreatedAtAction(nameof(GetById), new { id = e.EnvironmentalDataId },
            ApiResponse<EnvironmentalDataDto>.Created(e));
    }
}