using BomberosAPI.API.Common.Responses;
using BomberosAPI.Application.Common.Constants;
using BomberosAPI.Application.Features.BioimpedanceMeasurements;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace BomberosAPI.API.Controllers;

[ApiController]
[Route("api/bioimpedance-measurements")]
[Authorize]
public class BioimpedanceMeasurementsController : ControllerBase
{
    private readonly BioimpedanceMeasurementService _service;

    public BioimpedanceMeasurementsController(BioimpedanceMeasurementService service)
    {
        _service = service;
    }

    [HttpGet]
    [ProducesResponseType(typeof(ApiResponse<IReadOnlyList<BioimpedanceMeasurementDto>>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetAll(CancellationToken ct)
    {
        var items = await _service.GetAllAsync(ct);
        return Ok(ApiResponse<IReadOnlyList<BioimpedanceMeasurementDto>>.Ok(items));
    }

    [HttpGet("{id:guid}")]
    [ProducesResponseType(typeof(ApiResponse<BioimpedanceMeasurementDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse<object?>), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetById(Guid id, CancellationToken ct)
    {
        var item = await _service.GetByIdAsync(id, ct);
        return Ok(ApiResponse<BioimpedanceMeasurementDto>.Ok(item));
    }

    [HttpGet("by-participant/{participantId:guid}")]
    [ProducesResponseType(typeof(ApiResponse<IReadOnlyList<BioimpedanceMeasurementDto>>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetByParticipant(Guid participantId, CancellationToken ct)
    {
        var items = await _service.GetByParticipantAsync(participantId, ct);
        return Ok(ApiResponse<IReadOnlyList<BioimpedanceMeasurementDto>>.Ok(items));
    }

    [HttpGet("by-trainee/{traineeFirefighterId:guid}")]
    [ProducesResponseType(typeof(ApiResponse<IReadOnlyList<BioimpedanceMeasurementDto>>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetByTrainee(Guid traineeFirefighterId, CancellationToken ct)
    {
        var items = await _service.GetByTraineeAsync(traineeFirefighterId, ct);
        return Ok(ApiResponse<IReadOnlyList<BioimpedanceMeasurementDto>>.Ok(items));
    }

    [HttpPost]
    [Authorize(Roles = Roles.Medical + "," + Roles.Admin + "," + Roles.SystemAdmin)]
    [ProducesResponseType(typeof(ApiResponse<BioimpedanceMeasurementDto>), StatusCodes.Status201Created)]
    [ProducesResponseType(typeof(ApiResponse<object?>), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ApiResponse<object?>), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Create([FromBody] CreateBioimpedanceMeasurementRequest request, CancellationToken ct)
    {
        var item = await _service.CreateAsync(request, ct);
        return CreatedAtAction(nameof(GetById), new { id = item.BioimpedanceMeasurementId },
            ApiResponse<BioimpedanceMeasurementDto>.Created(item));
    }
}
