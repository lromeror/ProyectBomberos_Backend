using BomberosAPI.API.Common.Responses;
using BomberosAPI.Application.Common.Constants;
using BomberosAPI.Application.Features.TrainingSessions;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace BomberosAPI.API.Controllers;

[ApiController]
[Route("api/training-sessions")]
[Authorize]
public class TrainingSessionsController : ControllerBase
{
    // Mismo conjunto que "createSession" en el guards.js del frontend: quienes pueden
    // crear una sesión también pueden editarla, cancelarla o correr su ciclo de vida.
    private const string ManageSessionRoles = Roles.Admin + "," + Roles.SystemAdmin + "," + Roles.Capacitator + "," + Roles.FireChief;

    private readonly TrainingSessionService _service;

    public TrainingSessionsController(TrainingSessionService service) => _service = service;

    [HttpGet]
    [ProducesResponseType(typeof(ApiResponse<IReadOnlyList<TrainingSessionDto>>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetAll(CancellationToken ct)
    {
        var sessions = await _service.GetAllAsync(ct);
        return Ok(ApiResponse<IReadOnlyList<TrainingSessionDto>>.Ok(sessions));
    }

    [HttpGet("{id:guid}")]
    [ProducesResponseType(typeof(ApiResponse<TrainingSessionDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse<object?>), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetById(Guid id, CancellationToken ct)
    {
        var session = await _service.GetByIdAsync(id, ct);
        return Ok(ApiResponse<TrainingSessionDto>.Ok(session));
    }

    [HttpPost]
    [Authorize(Roles = ManageSessionRoles)]
    [ProducesResponseType(typeof(ApiResponse<TrainingSessionDto>), StatusCodes.Status201Created)]
    [ProducesResponseType(typeof(ApiResponse<object?>), StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> Create([FromBody] CreateTrainingSessionRequest request, CancellationToken ct)
    {
        var session = await _service.CreateAsync(request, ct);
        return CreatedAtAction(nameof(GetById), new { id = session.TrainingSessionId },
            ApiResponse<TrainingSessionDto>.Created(session));
    }

    [HttpPut("{id:guid}")]
    [Authorize(Roles = ManageSessionRoles)]
    [ProducesResponseType(typeof(ApiResponse<TrainingSessionDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse<object?>), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(ApiResponse<object?>), StatusCodes.Status422UnprocessableEntity)]
    public async Task<IActionResult> Update(Guid id, [FromBody] UpdateTrainingSessionRequest request, CancellationToken ct)
    {
        var session = await _service.UpdateAsync(id, request, ct);
        return Ok(ApiResponse<TrainingSessionDto>.Ok(session));
    }

    [HttpPatch("{id:guid}/status")]
    [Authorize(Roles = ManageSessionRoles)]
    [ProducesResponseType(typeof(ApiResponse<TrainingSessionDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse<object?>), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(ApiResponse<object?>), StatusCodes.Status422UnprocessableEntity)]
    public async Task<IActionResult> SetStatus(Guid id, [FromBody] SetSessionStatusRequest request, CancellationToken ct)
    {
        var session = await _service.SetStatusAsync(id, request.Status, ct);
        return Ok(ApiResponse<TrainingSessionDto>.Ok(session));
    }

    [HttpPatch("{id:guid}/start")]
    [Authorize(Roles = ManageSessionRoles)]
    [ProducesResponseType(typeof(ApiResponse<TrainingSessionDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse<object?>), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(ApiResponse<object?>), StatusCodes.Status422UnprocessableEntity)]
    public async Task<IActionResult> Start(Guid id, CancellationToken ct)
    {
        var session = await _service.StartAsync(id, ct);
        return Ok(ApiResponse<TrainingSessionDto>.Ok(session));
    }

    [HttpPatch("{id:guid}/finish")]
    [Authorize(Roles = ManageSessionRoles)]
    [ProducesResponseType(typeof(ApiResponse<TrainingSessionDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse<object?>), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(ApiResponse<object?>), StatusCodes.Status422UnprocessableEntity)]
    public async Task<IActionResult> Finish(Guid id, CancellationToken ct)
    {
        var session = await _service.FinishAsync(id, ct);
        return Ok(ApiResponse<TrainingSessionDto>.Ok(session));
    }
}

public record SetSessionStatusRequest(string Status);
