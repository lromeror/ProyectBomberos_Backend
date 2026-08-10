using BomberosAPI.API.Common.Responses;
using BomberosAPI.Application.Common.Constants;
using BomberosAPI.Application.Common.Interfaces;
using BomberosAPI.Application.Features.Users;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace BomberosAPI.API.Controllers;

[ApiController]
[Route("api/users")]
[Authorize]
public class UsersController : ControllerBase
{
    private readonly UserService _userService;
    private readonly ICurrentUserService _currentUser;

    public UsersController(UserService userService, ICurrentUserService currentUser)
    {
        _userService = userService;
        _currentUser = currentUser;
    }

    [HttpGet]
    [ProducesResponseType(typeof(ApiResponse<IReadOnlyList<UserDto>>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetAll([FromQuery] string? role, CancellationToken ct)
    {
        var users = role is not null
            ? await _userService.GetByRoleAsync(role, ct)
            : await _userService.GetAllAsync(ct);
        return Ok(ApiResponse<IReadOnlyList<UserDto>>.Ok(users));
    }

    [HttpGet("{id:guid}")]
    [ProducesResponseType(typeof(ApiResponse<UserDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse<object?>), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetById(Guid id, CancellationToken ct)
    {
        var user = await _userService.GetByIdAsync(id, ct);
        return Ok(ApiResponse<UserDto>.Ok(user));
    }

    // Quien da de alta bomberos aspirantes (Jefe) o personal médico (Admin) necesita
    // poder crear la cuenta de usuario que respalda esa ficha — ver PersonasScreen.js
    // del frontend, que llama a este endpoint antes de crear el TraineeFirefighter o
    // HealthPersonnel correspondiente.
    [HttpPost]
    [Authorize(Roles = Roles.FireChief + "," + Roles.Admin + "," + Roles.SystemAdmin)]
    [ProducesResponseType(typeof(ApiResponse<UserDto>), StatusCodes.Status201Created)]
    [ProducesResponseType(typeof(ApiResponse<object?>), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ApiResponse<object?>), StatusCodes.Status409Conflict)]
    public async Task<IActionResult> Create([FromBody] CreateUserRequest request, CancellationToken ct)
    {
        var user = await _userService.CreateAsync(request, ct);
        return CreatedAtAction(nameof(GetById), new { id = user.UserId },
            ApiResponse<UserDto>.Created(user));
    }

    // Cualquier usuario autenticado puede editar SU PROPIO perfil (ver SettingsScreen.js);
    // editar el de alguien más requiere SYSTEM_ADMIN. Sin este chequeo, cualquier cuenta
    // podía modificar el perfil de cualquier otra — incluida la de un administrador.
    [HttpPut("{id:guid}")]
    [ProducesResponseType(typeof(ApiResponse<UserDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse<object?>), StatusCodes.Status403Forbidden)]
    [ProducesResponseType(typeof(ApiResponse<object?>), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Update(Guid id, [FromBody] UpdateUserRequest request, CancellationToken ct)
    {
        if (id != _currentUser.UserId && !_currentUser.IsInRole(Roles.SystemAdmin))
            return Forbid();

        var user = await _userService.UpdateAsync(id, request, ct);
        return Ok(ApiResponse<UserDto>.Ok(user));
    }

    [HttpPatch("{id:guid}/status")]
    [Authorize(Roles = Roles.SystemAdmin)]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(typeof(ApiResponse<object?>), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> SetStatus(Guid id, [FromBody] SetStatusRequest request, CancellationToken ct)
    {
        await _userService.SetStatusAsync(id, request.Status, _currentUser.UserId, ct);
        return NoContent();
    }

    [HttpGet("{id:guid}/roles")]
    [ProducesResponseType(typeof(ApiResponse<IReadOnlyList<string>>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse<object?>), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetRoles(Guid id, CancellationToken ct)
    {
        var roleCodes = await _userService.GetRoleCodesAsync(id, ct);
        return Ok(ApiResponse<IReadOnlyList<string>>.Ok(roleCodes));
    }

    // El hallazgo más grave de la auditoría: este endpoint no tenía NINGUNA restricción
    // de rol — cualquier cuenta autenticada (incluido un aspirante) podía otorgarse
    // SYSTEM_ADMIN a sí misma vía PATCH /users/{miPropioId}/roles. Confirmado en vivo
    // contra la BD local antes de corregirlo.
    //
    // ADMIN y FIRE_CHIEF también necesitan llamar a este endpoint para completar el
    // alta de personal nuevo (PersonasScreen.js llama aquí después de crear el
    // HealthPersonnel/TraineeFirefighter), así que se les da acceso — pero
    // UserService.UpdateRolesAsync bloquea que otorguen SYSTEM_ADMIN o ADMIN, para no
    // reabrir el hueco de escalamiento de privilegios que motivó la restricción original.
    [HttpPatch("{id:guid}/roles")]
    [Authorize(Roles = Roles.SystemAdmin + "," + Roles.Admin + "," + Roles.FireChief)]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(typeof(ApiResponse<object?>), StatusCodes.Status403Forbidden)]
    [ProducesResponseType(typeof(ApiResponse<object?>), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(ApiResponse<object?>), StatusCodes.Status422UnprocessableEntity)]
    public async Task<IActionResult> UpdateRoles(Guid id, [FromBody] UpdateUserRolesRequest request, CancellationToken ct)
    {
        await _userService.UpdateRolesAsync(id, request.RoleCodes, _currentUser.UserId,
            _currentUser.IsInRole(Roles.SystemAdmin), ct);
        return NoContent();
    }
}

public record SetStatusRequest(string Status);
public record UpdateUserRolesRequest(IReadOnlyList<string> RoleCodes);
