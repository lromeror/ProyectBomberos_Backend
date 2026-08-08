using BomberosAPI.API.Common.Responses;
using BomberosAPI.Application.Common.Constants;
using BomberosAPI.Application.Features.SymptomReports;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace BomberosAPI.API.Controllers;

[ApiController]
[Route("api/symptom-reports")]
[Authorize]
public class SymptomReportsController : ControllerBase
{
    private readonly SymptomReportService _service;

    public SymptomReportsController(SymptomReportService service)
    {
        _service = service;
    }

    [HttpGet]
    [ProducesResponseType(typeof(ApiResponse<IReadOnlyList<SymptomReportDto>>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetAll(CancellationToken ct)
    {
        var items = await _service.GetAllAsync(ct);
        return Ok(ApiResponse<IReadOnlyList<SymptomReportDto>>.Ok(items));
    }

    [HttpGet("{id:guid}")]
    [ProducesResponseType(typeof(ApiResponse<SymptomReportDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse<object?>), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetById(Guid id, CancellationToken ct)
    {
        var item = await _service.GetByIdAsync(id, ct);
        return Ok(ApiResponse<SymptomReportDto>.Ok(item));
    }

    [HttpGet("by-participant/{participantId:guid}")]
    [ProducesResponseType(typeof(ApiResponse<IReadOnlyList<SymptomReportDto>>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetByParticipant(Guid participantId, CancellationToken ct)
    {
        var items = await _service.GetByParticipantAsync(participantId, ct);
        return Ok(ApiResponse<IReadOnlyList<SymptomReportDto>>.Ok(items));
    }

    [HttpGet("by-trainee/{traineeFirefighterId:guid}")]
    [ProducesResponseType(typeof(ApiResponse<IReadOnlyList<SymptomReportDto>>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetByTrainee(Guid traineeFirefighterId, CancellationToken ct)
    {
        var items = await _service.GetByTraineeAsync(traineeFirefighterId, ct);
        return Ok(ApiResponse<IReadOnlyList<SymptomReportDto>>.Ok(items));
    }

    // Más amplio que signos vitales/bioimpedancia (que exigen una ficha de personal de
    // salud): un síntoma lo puede levantar cualquiera de los roles presentes durante
    // la sesión, no solo personal médico. Sigue excluyendo a aspirantes e
    // investigadores — ninguna pantalla de hoy permite el auto-reporte del aspirante.
    [HttpPost]
    [Authorize(Roles = Roles.Medical + "," + Roles.Admin + "," + Roles.SystemAdmin + "," + Roles.Capacitator + "," + Roles.FireChief)]
    [ProducesResponseType(typeof(ApiResponse<SymptomReportDto>), StatusCodes.Status201Created)]
    [ProducesResponseType(typeof(ApiResponse<object?>), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ApiResponse<object?>), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Create([FromBody] CreateSymptomReportRequest request, CancellationToken ct)
    {
        var item = await _service.CreateAsync(request, ct);
        return CreatedAtAction(nameof(GetById), new { id = item.SymptomReportId },
            ApiResponse<SymptomReportDto>.Created(item));
    }
}
