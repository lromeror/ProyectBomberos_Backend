using BomberosAPI.API.Common.Responses;
using BomberosAPI.Application.Features.Reports;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace BomberosAPI.API.Controllers;

[ApiController]
[Route("api/reports")]
[Authorize]
public class ReportsController : ControllerBase
{
    private readonly ReportService _service;

    public ReportsController(ReportService service) => _service = service;

    /// <summary>Estadísticas agregadas (conteos y promedios de signos vitales), opcionalmente acotadas por fecha.</summary>
    [HttpGet("summary")]
    [Authorize(Roles = "RESEARCHER,SYSTEM_ADMIN")]
    [ProducesResponseType(typeof(ApiResponse<ReportSummaryDto>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetSummary([FromQuery] DateTime? from, [FromQuery] DateTime? to, CancellationToken ct)
    {
        var summary = await _service.GetSummaryAsync(from, to, ct);
        return Ok(ApiResponse<ReportSummaryDto>.Ok(summary));
    }

    /// <summary>Exportación anonimizada de signos vitales (identificados solo por código de aspirante, sin PII).</summary>
    [HttpGet("export")]
    [Authorize(Roles = "RESEARCHER,SYSTEM_ADMIN")]
    [ProducesResponseType(typeof(ApiResponse<IReadOnlyList<AnonymizedVitalsRowDto>>), StatusCodes.Status200OK)]
    public async Task<IActionResult> Export([FromQuery] DateTime? from, [FromQuery] DateTime? to, CancellationToken ct)
    {
        var rows = await _service.GetAnonymizedVitalsExportAsync(from, to, ct);
        return Ok(ApiResponse<IReadOnlyList<AnonymizedVitalsRowDto>>.Ok(rows));
    }
}
