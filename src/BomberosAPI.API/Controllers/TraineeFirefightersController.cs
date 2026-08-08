using BomberosAPI.API.Common.Responses;
using BomberosAPI.Application.Common.Constants;
using BomberosAPI.Application.Common.Interfaces;
using BomberosAPI.Application.Features.TraineeFirefighters;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace BomberosAPI.API.Controllers;

[ApiController]
[Route("api/trainee-firefighters")]
[Authorize]
public class TraineeFirefightersController : ControllerBase
{
    private readonly TraineeFirefighterService _service;
    private readonly ICurrentUserService _currentUser;

    public TraineeFirefightersController(TraineeFirefighterService service, ICurrentUserService currentUser)
    {
        _service = service;
        _currentUser = currentUser;
    }

    [HttpGet]
    [ProducesResponseType(typeof(ApiResponse<IReadOnlyList<TraineeFirefighterDto>>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetAll(CancellationToken ct)
    {
        var trainees = await _service.GetAllAsync(ct);
        return Ok(ApiResponse<IReadOnlyList<TraineeFirefighterDto>>.Ok(trainees));
    }

    [HttpGet("{id:guid}")]
    [ProducesResponseType(typeof(ApiResponse<TraineeFirefighterDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse<object?>), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetById(Guid id, CancellationToken ct)
    {
        var trainee = await _service.GetByIdAsync(id, ct);
        return Ok(ApiResponse<TraineeFirefighterDto>.Ok(trainee));
    }

    [HttpPost]
    [Authorize(Roles = Roles.FireChief + "," + Roles.Admin + "," + Roles.SystemAdmin)]
    [ProducesResponseType(typeof(ApiResponse<TraineeFirefighterDto>), StatusCodes.Status201Created)]
    [ProducesResponseType(typeof(ApiResponse<object?>), StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> Create([FromBody] CreateTraineeFirefighterRequest request, CancellationToken ct)
    {
        var trainee = await _service.CreateAsync(request, ct);
        return CreatedAtAction(nameof(GetById), new { id = trainee.TraineeFirefighterId },
            ApiResponse<TraineeFirefighterDto>.Created(trainee));
    }

    [HttpPut("{id:guid}")]
    [Authorize(Roles = Roles.FireChief + "," + Roles.Admin + "," + Roles.SystemAdmin)]
    [ProducesResponseType(typeof(ApiResponse<TraineeFirefighterDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse<object?>), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Update(Guid id, [FromBody] UpdateTraineeFirefighterRequest request, CancellationToken ct)
    {
        var trainee = await _service.UpdateAsync(id, request, ct);
        return Ok(ApiResponse<TraineeFirefighterDto>.Ok(trainee));
    }

    [HttpPatch("{id:guid}/training-status")]
    [Authorize(Roles = Roles.FireChief + "," + Roles.Admin + "," + Roles.SystemAdmin)]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(typeof(ApiResponse<object?>), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> SetTrainingStatus(Guid id, [FromBody] SetTrainingStatusRequest request, CancellationToken ct)
    {
        await _service.SetTrainingStatusAsync(id, request.Status, _currentUser.UserId, ct);
        return NoContent();
    }
}

public record SetTrainingStatusRequest(string Status);