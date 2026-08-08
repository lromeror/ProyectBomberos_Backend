using BomberosAPI.API.Common.Responses;
using BomberosAPI.Application.Common.Constants;
using BomberosAPI.Application.Common.Interfaces;
using BomberosAPI.Application.Features.Consents;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace BomberosAPI.API.Controllers;

[ApiController]
[Route("api/consents")]
[Authorize]
public class ConsentsController : ControllerBase
{
    private readonly ConsentService _service;
    private readonly ICurrentUserService _currentUser;

    public ConsentsController(ConsentService service, ICurrentUserService currentUser)
    {
        _service = service;
        _currentUser = currentUser;
    }

    /// <summary>Documentos de consentimiento vigentes (texto legal a mostrar/aceptar).</summary>
    [HttpGet("documents")]
    [ProducesResponseType(typeof(ApiResponse<IReadOnlyList<ConsentDocumentDto>>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetActiveDocuments(CancellationToken ct)
    {
        var items = await _service.GetActiveDocumentsAsync(ct);
        return Ok(ApiResponse<IReadOnlyList<ConsentDocumentDto>>.Ok(items));
    }

    /// <summary>Publica una nueva versión de un documento de consentimiento. Solo SYSTEM_ADMIN.</summary>
    [HttpPost("documents")]
    [Authorize(Roles = Roles.SystemAdmin)]
    [ProducesResponseType(typeof(ApiResponse<ConsentDocumentDto>), StatusCodes.Status201Created)]
    public async Task<IActionResult> CreateDocument([FromBody] CreateConsentDocumentRequest request, CancellationToken ct)
    {
        var document = await _service.CreateDocumentAsync(request, ct);
        return Ok(ApiResponse<ConsentDocumentDto>.Created(document));
    }

    /// <summary>Consentimientos otorgados/revocados por el usuario autenticado.</summary>
    [HttpGet("me")]
    [ProducesResponseType(typeof(ApiResponse<IReadOnlyList<UserConsentDto>>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetMine(CancellationToken ct)
    {
        var items = await _service.GetMineAsync(_currentUser.UserId, ct);
        return Ok(ApiResponse<IReadOnlyList<UserConsentDto>>.Ok(items));
    }

    [HttpPost("{documentId:guid}/grant")]
    [ProducesResponseType(typeof(ApiResponse<UserConsentDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse<object?>), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Grant(Guid documentId, CancellationToken ct)
    {
        var consent = await _service.GrantAsync(_currentUser.UserId, documentId, ct);
        return Ok(ApiResponse<UserConsentDto>.Ok(consent));
    }

    [HttpPost("{documentId:guid}/revoke")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(typeof(ApiResponse<object?>), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Revoke(Guid documentId, CancellationToken ct)
    {
        await _service.RevokeAsync(_currentUser.UserId, documentId, ct);
        return NoContent();
    }
}
