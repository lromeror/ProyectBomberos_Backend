using BomberosAPI.API.Common.Responses;
using BomberosAPI.Application.Common.Constants;
using BomberosAPI.Application.Common.Interfaces;
using BomberosAPI.Application.Features.SessionResults;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace BomberosAPI.API.Controllers;

[ApiController]
[Route("api/session-results")]
[Authorize]
public class SessionResultsController : ControllerBase
{
    private const string ManageRoles = Roles.Medical + "," + Roles.Admin + "," + Roles.SystemAdmin;
    private const string ViewRoles = ManageRoles + "," + Roles.Capacitator + "," + Roles.FireChief;

    private readonly SessionResultService _service;
    private readonly ICurrentUserService _currentUser;

    public SessionResultsController(SessionResultService service, ICurrentUserService currentUser)
    {
        _service = service;
        _currentUser = currentUser;
    }

    [HttpPost]
    [Authorize(Roles = ManageRoles)]
    [ProducesResponseType(typeof(ApiResponse<SessionResultDto>), StatusCodes.Status201Created)]
    public async Task<IActionResult> Create([FromBody] CreateSessionResultRequest request, CancellationToken ct)
    {
        var result = await _service.CreateAsync(request, _currentUser.UserId, ct);
        return CreatedAtAction(nameof(GetByParticipant), new { participantId = result.SessionParticipantId },
            ApiResponse<SessionResultDto>.Created(result));
    }

    [HttpGet("by-participant/{participantId:guid}")]
    [Authorize(Roles = ViewRoles)]
    [ProducesResponseType(typeof(ApiResponse<IReadOnlyList<SessionResultDto>>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetByParticipant(Guid participantId, CancellationToken ct)
    {
        var items = await _service.GetByParticipantAsync(participantId, ct);
        return Ok(ApiResponse<IReadOnlyList<SessionResultDto>>.Ok(items));
    }
}
