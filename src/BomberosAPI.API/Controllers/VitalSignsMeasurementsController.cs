using BomberosAPI.API.Common.Responses;
using BomberosAPI.Application.Common.Constants;
using BomberosAPI.Application.Features.VitalSigns;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace BomberosAPI.API.Controllers;

[ApiController]
[Route("api/vital-signs")]
[Authorize]
public class VitalSignsMeasurementsController : ControllerBase
{
    private readonly VitalSignsMeasurementService _service;

    public VitalSignsMeasurementsController(VitalSignsMeasurementService service)
    {
        _service = service;
    }

    [HttpGet]
    [ProducesResponseType(typeof(ApiResponse<IReadOnlyList<VitalSignsMeasurementDto>>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetAll(CancellationToken ct)
    {
        var items = await _service.GetAllAsync(ct);
        return Ok(ApiResponse<IReadOnlyList<VitalSignsMeasurementDto>>.Ok(items));
    }

    [HttpGet("{id:guid}")]
    [ProducesResponseType(typeof(ApiResponse<VitalSignsMeasurementDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse<object?>), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetById(Guid id, CancellationToken ct)
    {
        var v = await _service.GetByIdAsync(id, ct);
        return Ok(ApiResponse<VitalSignsMeasurementDto>.Ok(v));
    }

    [HttpGet("by-participant/{participantId:guid}")]
    [ProducesResponseType(typeof(ApiResponse<IReadOnlyList<VitalSignsMeasurementDto>>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetByParticipant(Guid participantId, CancellationToken ct)
    {
        var items = await _service.GetByParticipantAsync(participantId, ct);
        return Ok(ApiResponse<IReadOnlyList<VitalSignsMeasurementDto>>.Ok(items));
    }

    [HttpGet("by-trainee/{traineeFirefighterId:guid}")]
    [ProducesResponseType(typeof(ApiResponse<IReadOnlyList<VitalSignsHistoryDto>>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetByTrainee(Guid traineeFirefighterId, CancellationToken ct)
    {
        var items = await _service.GetByTraineeAsync(traineeFirefighterId, ct);
        return Ok(ApiResponse<IReadOnlyList<VitalSignsHistoryDto>>.Ok(items));
    }

    [HttpPost]
    [Authorize(Roles = Roles.Medical + "," + Roles.Admin + "," + Roles.SystemAdmin)]
    [ProducesResponseType(typeof(ApiResponse<VitalSignsMeasurementDto>), StatusCodes.Status201Created)]
    [ProducesResponseType(typeof(ApiResponse<object?>), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ApiResponse<object?>), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Create([FromBody] CreateVitalSignsMeasurementRequest request, CancellationToken ct)
    {
        var v = await _service.CreateAsync(request, ct);
        return CreatedAtAction(nameof(GetById), new { id = v.VitalSignsMeasurementId },
            ApiResponse<VitalSignsMeasurementDto>.Created(v));
    }
}