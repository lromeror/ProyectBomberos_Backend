using BomberosAPI.API.Common.Responses;
using BomberosAPI.Application.Common.Constants;
using BomberosAPI.Application.Common.Interfaces;
using BomberosAPI.Application.Features.MedicalHistory;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace BomberosAPI.API.Controllers;

[ApiController]
[Route("api/medical-history")]
[Authorize]
public class MedicalHistoryController : ControllerBase
{
    private readonly MedicalHistoryService _service;
    private readonly ICurrentUserService _currentUser;

    public MedicalHistoryController(MedicalHistoryService service, ICurrentUserService currentUser)
    {
        _service = service;
        _currentUser = currentUser;
    }

    // Lectura restringida al mismo personal que puede escribir (Medical/Admin/SystemAdmin) —
    // antes cualquier cuenta autenticada, incluido un aspirante, podía leer el historial
    // médico completo de cualquier otro aspirante (alergias, medicación, condiciones
    // preexistentes). Ningún flujo del frontend actual necesita un rol distinto para
    // estas rutas (MedicalHistoryScreen ya está gateado por el mismo permiso).
    [HttpGet]
    [Authorize(Roles = Roles.Medical + "," + Roles.Admin + "," + Roles.SystemAdmin)]
    [ProducesResponseType(typeof(ApiResponse<IReadOnlyList<MedicalHistoryDto>>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetAll(CancellationToken ct)
    {
        var items = await _service.GetAllAsync(ct);
        return Ok(ApiResponse<IReadOnlyList<MedicalHistoryDto>>.Ok(items));
    }

    [HttpGet("{id:guid}")]
    [Authorize(Roles = Roles.Medical + "," + Roles.Admin + "," + Roles.SystemAdmin)]
    [ProducesResponseType(typeof(ApiResponse<MedicalHistoryDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse<object?>), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetById(Guid id, CancellationToken ct)
    {
        var mh = await _service.GetByIdAsync(id, ct);
        return Ok(ApiResponse<MedicalHistoryDto>.Ok(mh));
    }

    [HttpGet("by-trainee/{traineeId:guid}")]
    [Authorize(Roles = Roles.Medical + "," + Roles.Admin + "," + Roles.SystemAdmin)]
    [ProducesResponseType(typeof(ApiResponse<MedicalHistoryDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse<object?>), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetByTrainee(Guid traineeId, CancellationToken ct)
    {
        var mh = await _service.GetByTraineeAsync(traineeId, ct);
        return Ok(ApiResponse<MedicalHistoryDto>.Ok(mh));
    }

    [HttpPost]
    [Authorize(Roles = Roles.Medical)]
    [ProducesResponseType(typeof(ApiResponse<MedicalHistoryDto>), StatusCodes.Status201Created)]
    [ProducesResponseType(typeof(ApiResponse<object?>), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ApiResponse<object?>), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(ApiResponse<object?>), StatusCodes.Status409Conflict)]
    public async Task<IActionResult> Create([FromBody] CreateMedicalHistoryRequest request, CancellationToken ct)
    {
        var mh = await _service.CreateAsync(request, _currentUser.UserId, ct);
        return CreatedAtAction(nameof(GetById), new { id = mh.MedicalHistoryId },
            ApiResponse<MedicalHistoryDto>.Created(mh));
    }

    [HttpPut("{id:guid}")]
    [Authorize(Roles = Roles.Medical)]
    [ProducesResponseType(typeof(ApiResponse<MedicalHistoryDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse<object?>), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Update(Guid id, [FromBody] UpdateMedicalHistoryRequest request, CancellationToken ct)
    {
        var mh = await _service.UpdateAsync(id, request, _currentUser.UserId, ct);
        return Ok(ApiResponse<MedicalHistoryDto>.Ok(mh));
    }
}