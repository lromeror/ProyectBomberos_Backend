using BomberosAPI.API.Common.Responses;
using BomberosAPI.Application.Common.Constants;
using BomberosAPI.Application.Features.Invitations;
using BomberosAPI.Application.Features.Participants;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace BomberosAPI.API.Controllers;

[ApiController]
[Route("api/invitations")]
[Authorize]
public class InvitationsController : ControllerBase
{
    // Igual que "manageInvitations" en el guards.js del frontend.
    private const string ManageInvitationRoles = Roles.Admin + "," + Roles.SystemAdmin + "," + Roles.Capacitator + "," + Roles.FireChief;

    // Igual que "validateInvitations" en el guards.js del frontend — la Cola de
    // Validaciones (ValidationQueueScreen) revisa invitaciones de terceros, así que
    // necesita poder aceptar/rechazar en nombre del destinatario real.
    private const string ValidateInvitationRoles = Roles.Medical + "," + Roles.Admin + "," + Roles.SystemAdmin;

    private readonly InvitationService _service;

    public InvitationsController(InvitationService service)
    {
        _service = service;
    }

    [HttpGet]
    [ProducesResponseType(typeof(ApiResponse<IReadOnlyList<InvitationDto>>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetAll(CancellationToken ct)
    {
        var items = await _service.GetAllAsync(ct);
        return Ok(ApiResponse<IReadOnlyList<InvitationDto>>.Ok(items));
    }

    [HttpGet("{id:guid}")]
    [ProducesResponseType(typeof(ApiResponse<InvitationDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse<object?>), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetById(Guid id, CancellationToken ct)
    {
        var invitation = await _service.GetByIdAsync(id, ct);
        return Ok(ApiResponse<InvitationDto>.Ok(invitation));
    }

    [HttpPost]
    [Authorize(Roles = ManageInvitationRoles)]
    [ProducesResponseType(typeof(ApiResponse<CreateInvitationResponse>), StatusCodes.Status201Created)]
    [ProducesResponseType(typeof(ApiResponse<object?>), StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> Create([FromBody] CreateInvitationRequest request, CancellationToken ct)
    {
        var response = await _service.CreateAsync(request, ct);
        return CreatedAtAction(nameof(GetById), new { id = response.Invitation.InvitationId },
            ApiResponse<CreateInvitationResponse>.Created(response));
    }

    [HttpPost("{id:guid}/accept")]
    [ProducesResponseType(typeof(ApiResponse<SessionParticipantDto?>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse<object?>), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(ApiResponse<object?>), StatusCodes.Status409Conflict)]
    public async Task<IActionResult> Accept(Guid id, CancellationToken ct)
    {
        var participant = await _service.AcceptAsync(id, ct);
        return Ok(ApiResponse<SessionParticipantDto?>.Ok(participant));
    }

    [HttpPost("{id:guid}/reject")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(typeof(ApiResponse<object?>), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(ApiResponse<object?>), StatusCodes.Status409Conflict)]
    public async Task<IActionResult> Reject(Guid id, CancellationToken ct)
    {
        await _service.RejectAsync(id, ct);
        return NoContent();
    }

    /// <summary>
    /// Aprueba una invitación en nombre de su destinatario. Uso exclusivo del personal
    /// de validación (Cola de Validaciones) — el propio destinatario sigue usando
    /// POST /accept.
    /// </summary>
    [HttpPost("{id:guid}/staff-accept")]
    [Authorize(Roles = ValidateInvitationRoles)]
    [ProducesResponseType(typeof(ApiResponse<SessionParticipantDto?>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse<object?>), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(ApiResponse<object?>), StatusCodes.Status409Conflict)]
    public async Task<IActionResult> StaffAccept(Guid id, CancellationToken ct)
    {
        var participant = await _service.StaffAcceptAsync(id, ct);
        return Ok(ApiResponse<SessionParticipantDto?>.Ok(participant));
    }

    /// <summary>
    /// Rechaza una invitación en nombre de su destinatario. Uso exclusivo del personal
    /// de validación (Cola de Validaciones) — el propio destinatario sigue usando
    /// POST /reject.
    /// </summary>
    [HttpPost("{id:guid}/staff-reject")]
    [Authorize(Roles = ValidateInvitationRoles)]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(typeof(ApiResponse<object?>), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(ApiResponse<object?>), StatusCodes.Status409Conflict)]
    public async Task<IActionResult> StaffReject(Guid id, CancellationToken ct)
    {
        await _service.StaffRejectAsync(id, ct);
        return NoContent();
    }

    [HttpPost("{id:guid}/revoke")]
    [Authorize(Roles = ManageInvitationRoles)]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(typeof(ApiResponse<object?>), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(ApiResponse<object?>), StatusCodes.Status409Conflict)]
    public async Task<IActionResult> Revoke(Guid id, CancellationToken ct)
    {
        await _service.RevokeAsync(id, ct);
        return NoContent();
    }
}