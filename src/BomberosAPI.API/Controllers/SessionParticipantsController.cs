using BomberosAPI.API.Common.Responses;
using BomberosAPI.Application.Common.Constants;
using BomberosAPI.Application.Features.Participants;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace BomberosAPI.API.Controllers;

[ApiController]
[Route("api/session-participants")]
[Authorize]
public class SessionParticipantsController : ControllerBase
{
    private const string ManageParticipantRoles = Roles.Admin + "," + Roles.SystemAdmin + "," + Roles.Capacitator + "," + Roles.FireChief;

    private readonly SessionParticipantService _service;

    public SessionParticipantsController(SessionParticipantService service)
    {
        _service = service;
    }

    [HttpGet]
    [ProducesResponseType(typeof(ApiResponse<IReadOnlyList<SessionParticipantDto>>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetAll(CancellationToken ct)
    {
        var items = await _service.GetAllAsync(ct);
        return Ok(ApiResponse<IReadOnlyList<SessionParticipantDto>>.Ok(items));
    }

    [HttpGet("{id:guid}")]
    [ProducesResponseType(typeof(ApiResponse<SessionParticipantDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse<object?>), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetById(Guid id, CancellationToken ct)
    {
        var participant = await _service.GetByIdAsync(id, ct);
        return Ok(ApiResponse<SessionParticipantDto>.Ok(participant));
    }

    [HttpPost]
    [Authorize(Roles = ManageParticipantRoles)]
    [ProducesResponseType(typeof(ApiResponse<SessionParticipantDto>), StatusCodes.Status201Created)]
    [ProducesResponseType(typeof(ApiResponse<object?>), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ApiResponse<object?>), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Create([FromBody] CreateSessionParticipantRequest request, CancellationToken ct)
    {
        var participant = await _service.CreateAsync(request, ct);
        return CreatedAtAction(nameof(GetById), new { id = participant.SessionParticipantId },
            ApiResponse<SessionParticipantDto>.Created(participant));
    }

    [HttpPost("{id:guid}/check-in")]
    [Authorize(Roles = ManageParticipantRoles)]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(typeof(ApiResponse<object?>), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> CheckIn(Guid id, CancellationToken ct)
    {
        await _service.CheckInAsync(id, ct);
        return NoContent();
    }
}