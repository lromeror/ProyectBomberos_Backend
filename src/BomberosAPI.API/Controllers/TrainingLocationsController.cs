using BomberosAPI.API.Common.Responses;
using BomberosAPI.Application.Common.Constants;
using BomberosAPI.Application.Features.TrainingLocations;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace BomberosAPI.API.Controllers;

[ApiController]
[Route("api/training-locations")]
[Authorize]
public class TrainingLocationsController : ControllerBase
{
    private readonly TrainingLocationService _service;

    public TrainingLocationsController(TrainingLocationService service) => _service = service;

    [HttpGet]
    [ProducesResponseType(typeof(ApiResponse<IReadOnlyList<TrainingLocationDto>>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetAll(CancellationToken ct)
    {
        var locations = await _service.GetAllAsync(ct);
        return Ok(ApiResponse<IReadOnlyList<TrainingLocationDto>>.Ok(locations));
    }

    [HttpGet("{id:guid}")]
    [ProducesResponseType(typeof(ApiResponse<TrainingLocationDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse<object?>), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetById(Guid id, CancellationToken ct)
    {
        var loc = await _service.GetByIdAsync(id, ct);
        return Ok(ApiResponse<TrainingLocationDto>.Ok(loc));
    }

    [HttpPost]
    [Authorize(Roles = Roles.Admin + "," + Roles.SystemAdmin)]
    [ProducesResponseType(typeof(ApiResponse<TrainingLocationDto>), StatusCodes.Status201Created)]
    [ProducesResponseType(typeof(ApiResponse<object?>), StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> Create([FromBody] CreateTrainingLocationRequest request, CancellationToken ct)
    {
        var location = await _service.CreateAsync(request, ct);
        return CreatedAtAction(nameof(GetById), new { id = location.TrainingLocationId },
            ApiResponse<TrainingLocationDto>.Created(location));
    }

    [HttpPut("{id:guid}")]
    [Authorize(Roles = Roles.Admin + "," + Roles.SystemAdmin)]
    [ProducesResponseType(typeof(ApiResponse<TrainingLocationDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse<object?>), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Update(Guid id, [FromBody] UpdateTrainingLocationRequest request, CancellationToken ct)
    {
        var location = await _service.UpdateAsync(id, request, ct);
        return Ok(ApiResponse<TrainingLocationDto>.Ok(location));
    }
}
