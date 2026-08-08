using BomberosAPI.API.Common.Responses;
using BomberosAPI.Application.Common.Constants;
using BomberosAPI.Application.Features.Institutions;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace BomberosAPI.API.Controllers;

[ApiController]
[Route("api/institutions")]
[Authorize]
public class InstitutionsController : ControllerBase
{
    private readonly InstitutionService _institutionService;

    public InstitutionsController(InstitutionService institutionService) =>
        _institutionService = institutionService;

    [HttpGet]
    [ProducesResponseType(typeof(ApiResponse<IReadOnlyList<InstitutionDto>>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetAll(CancellationToken ct)
    {
        var institutions = await _institutionService.GetAllAsync(ct);
        return Ok(ApiResponse<IReadOnlyList<InstitutionDto>>.Ok(institutions));
    }

    [HttpGet("{id:guid}")]
    [ProducesResponseType(typeof(ApiResponse<InstitutionDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse<object?>), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetById(Guid id, CancellationToken ct)
    {
        var institution = await _institutionService.GetByIdAsync(id, ct);
        return Ok(ApiResponse<InstitutionDto>.Ok(institution));
    }

    [HttpPost]
    [Authorize(Roles = Roles.Admin + "," + Roles.SystemAdmin)]
    [ProducesResponseType(typeof(ApiResponse<InstitutionDto>), StatusCodes.Status201Created)]
    [ProducesResponseType(typeof(ApiResponse<object?>), StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> Create([FromBody] CreateInstitutionRequest request, CancellationToken ct)
    {
        var institution = await _institutionService.CreateAsync(request, ct);
        return CreatedAtAction(nameof(GetById), new { id = institution.InstitutionId },
            ApiResponse<InstitutionDto>.Created(institution));
    }

    [HttpPut("{id:guid}")]
    [Authorize(Roles = Roles.Admin + "," + Roles.SystemAdmin)]
    [ProducesResponseType(typeof(ApiResponse<InstitutionDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse<object?>), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Update(Guid id, [FromBody] UpdateInstitutionRequest request, CancellationToken ct)
    {
        var institution = await _institutionService.UpdateAsync(id, request, ct);
        return Ok(ApiResponse<InstitutionDto>.Ok(institution));
    }
}
