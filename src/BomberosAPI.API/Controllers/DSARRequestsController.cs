using BomberosAPI.API.Common.Responses;
using BomberosAPI.Application.Common.Constants;
using BomberosAPI.Application.Common.Interfaces;
using BomberosAPI.Application.Features.DSARRequests;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace BomberosAPI.API.Controllers;

/// <summary>Solicitudes de un usuario para ejercer derechos sobre sus propios datos (acceso, corrección, eliminación, portabilidad).</summary>
[ApiController]
[Route("api/dsar-requests")]
[Authorize]
public class DSARRequestsController : ControllerBase
{
    private readonly DSARRequestService _service;
    private readonly ICurrentUserService _currentUser;

    public DSARRequestsController(DSARRequestService service, ICurrentUserService currentUser)
    {
        _service = service;
        _currentUser = currentUser;
    }

    [HttpPost]
    [ProducesResponseType(typeof(ApiResponse<DSARRequestDto>), StatusCodes.Status201Created)]
    public async Task<IActionResult> Create([FromBody] CreateDSARRequestRequest request, CancellationToken ct)
    {
        var created = await _service.CreateAsync(_currentUser.UserId, request, ct);
        return CreatedAtAction(nameof(GetMine), null, ApiResponse<DSARRequestDto>.Created(created));
    }

    [HttpGet("mine")]
    [ProducesResponseType(typeof(ApiResponse<IReadOnlyList<DSARRequestDto>>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetMine(CancellationToken ct)
    {
        var items = await _service.GetMineAsync(_currentUser.UserId, ct);
        return Ok(ApiResponse<IReadOnlyList<DSARRequestDto>>.Ok(items));
    }

    [HttpGet]
    [Authorize(Roles = Roles.SystemAdmin)]
    [ProducesResponseType(typeof(ApiResponse<IReadOnlyList<DSARRequestDto>>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetAll(CancellationToken ct)
    {
        var items = await _service.GetAllAsync(ct);
        return Ok(ApiResponse<IReadOnlyList<DSARRequestDto>>.Ok(items));
    }

    [HttpPatch("{id:guid}/respond")]
    [Authorize(Roles = Roles.SystemAdmin)]
    [ProducesResponseType(typeof(ApiResponse<DSARRequestDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse<object?>), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Respond(Guid id, [FromBody] RespondDSARRequestRequest request, CancellationToken ct)
    {
        var updated = await _service.RespondAsync(id, _currentUser.UserId, request, ct);
        return Ok(ApiResponse<DSARRequestDto>.Ok(updated));
    }
}
