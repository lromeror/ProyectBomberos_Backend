using BomberosAPI.Application.Common.Constants;
using BomberosAPI.Application.Common.Exceptions;
using BomberosAPI.Application.Features.Audit;
using BomberosAPI.Domain.Entities;
using BomberosAPI.Domain.Repositories;
using FluentValidation;
using AppValidationException = BomberosAPI.Application.Common.Exceptions.ValidationException;

namespace BomberosAPI.Application.Features.Users;

public class UserService
{
    // El enum BomberosAPI.Domain.Enums.AccountStatus quedó sin usar — User.AccountStatus
    // es un string plano en minúscula (ver AuthService.LoginAsync) — así que se valida
    // aquí contra la lista real en vez de aceptar cualquier texto.
    private static readonly HashSet<string> ValidStatuses = new(StringComparer.OrdinalIgnoreCase)
    {
        "active", "suspended", "inactive", "pending_activation",
    };

    private readonly IUserRepository _repo;
    private readonly IRoleRepository _roleRepo;
    private readonly IUserRoleRepository _userRoleRepo;
    private readonly IValidator<CreateUserRequest> _createValidator;
    private readonly ChangeAuditService _changeAudit;

    public UserService(
        IUserRepository repo,
        IRoleRepository roleRepo,
        IUserRoleRepository userRoleRepo,
        IValidator<CreateUserRequest> createValidator,
        ChangeAuditService changeAudit)
    {
        _repo = repo;
        _roleRepo = roleRepo;
        _userRoleRepo = userRoleRepo;
        _createValidator = createValidator;
        _changeAudit = changeAudit;
    }

    public async Task<IReadOnlyList<UserDto>> GetAllAsync(CancellationToken ct = default)
    {
        var users = await _repo.GetAllAsync(ct);
        return users.Select(ToDto).ToList();
    }

    public async Task<IReadOnlyList<UserDto>> GetByRoleAsync(string roleCode, CancellationToken ct = default)
    {
        var users = await _repo.GetByRoleAsync(roleCode, ct);
        return users.Select(ToDto).ToList();
    }

    public async Task<UserDto> GetByIdAsync(Guid id, CancellationToken ct = default)
    {
        var user = await _repo.GetByIdAsync(id, ct)
            ?? throw new NotFoundException("User", id);
        return ToDto(user);
    }

    public async Task<UserDto> CreateAsync(CreateUserRequest request, CancellationToken ct = default)
    {
        var validation = await _createValidator.ValidateAsync(request, ct);
        if (!validation.IsValid)
        {
            var errors = validation.Errors
                .GroupBy(e => e.PropertyName)
                .ToDictionary(g => g.Key, g => g.Select(e => e.ErrorMessage).ToArray());
            throw new AppValidationException(errors);
        }

        if (await _repo.ExistsByEmailAsync(request.Email, ct))
            throw new ConflictException("Email already in use.");

        var user = new User
        {
            UserId = Guid.NewGuid(),
            InstitutionId = request.InstitutionId,
            Email = request.Email,
            FirstName = request.FirstName,
            LastName = request.LastName,
            Phone = request.Phone,
            AccountStatus = "active",
            CreatedAt = DateTime.UtcNow
        };

        await _repo.AddAsync(user, ct);
        return ToDto(user);
    }

    public async Task<UserDto> UpdateAsync(Guid id, UpdateUserRequest request, CancellationToken ct = default)
    {
        var user = await _repo.GetByIdAsync(id, ct)
            ?? throw new NotFoundException("User", id);

        if (!string.Equals(user.Email, request.Email, StringComparison.OrdinalIgnoreCase)
            && await _repo.ExistsByEmailAsync(request.Email, ct))
            throw new ConflictException("Email already in use.");

        user.FirstName = request.FirstName;
        user.LastName = request.LastName;
        user.Phone = request.Phone;
        user.Email = request.Email;

        await _repo.UpdateAsync(user, ct);
        return ToDto(user);
    }

    public async Task SetStatusAsync(Guid id, string status, Guid actingUserId, CancellationToken ct = default)
    {
        if (!ValidStatuses.Contains(status))
            throw new BusinessRuleException($"'{status}' is not a valid account status.");

        var user = await _repo.GetByIdAsync(id, ct)
            ?? throw new NotFoundException("User", id);

        var previousStatus = user.AccountStatus;
        user.AccountStatus = status.ToLowerInvariant();
        await _repo.UpdateAsync(user, ct);

        await _changeAudit.LogAsync(actingUserId, "User", id, "StatusChange",
            new Dictionary<string, object?> { ["status"] = previousStatus },
            new Dictionary<string, object?> { ["status"] = user.AccountStatus },
            ct);
    }

    /// <summary>Códigos de rol actualmente activos para el usuario (para precargar un formulario de edición de permisos).</summary>
    public async Task<IReadOnlyList<string>> GetRoleCodesAsync(Guid userId, CancellationToken ct = default)
    {
        if (await _repo.GetByIdAsync(userId, ct) is null)
            throw new NotFoundException("User", userId);

        return await _userRoleRepo.GetActiveRoleCodesByUserIdAsync(userId, ct);
    }

    /// <summary>
    /// Reemplaza los roles activos del usuario por `roleCodes`. Es borrado lógico: las
    /// asignaciones que ya no aplican se desactivan (IsActive=false, EndDate=now) en vez
    /// de eliminarse, preservando el historial de quién tuvo qué rol y cuándo.
    /// </summary>
    // Roles que solo SYSTEM_ADMIN puede otorgar — evita que ADMIN/FIRE_CHIEF (que
    // también llaman a este método para completar el alta de personal nuevo) se
    // auto-escalen o escalen a otra cuenta a un nivel de privilegio mayor al suyo.
    private static readonly HashSet<string> PrivilegedRoles = new(StringComparer.OrdinalIgnoreCase)
    {
        BomberosAPI.Application.Common.Constants.Roles.SystemAdmin,
        BomberosAPI.Application.Common.Constants.Roles.Admin,
    };

    public async Task UpdateRolesAsync(Guid userId, IReadOnlyList<string> roleCodes, Guid? actingUserId, bool actingIsSystemAdmin, CancellationToken ct = default)
    {
        if (await _repo.GetByIdAsync(userId, ct) is null)
            throw new NotFoundException("User", userId);

        var distinctCodes = (roleCodes ?? Array.Empty<string>()).Distinct().ToList();
        if (distinctCodes.Count == 0)
            throw new BusinessRuleException("A user must have at least one role.");

        // ADMIN y FIRE_CHIEF pueden llegar aquí al completar el alta de personal nuevo
        // (personal médico / bombero aspirante), pero no pueden otorgar SYSTEM_ADMIN ni
        // ADMIN — solo SYSTEM_ADMIN puede asignar roles de ese nivel.
        if (!actingIsSystemAdmin && distinctCodes.Any(c => PrivilegedRoles.Contains(c)))
            throw new ForbiddenException();

        var resolvedRoleIds = new List<Guid>();
        foreach (var code in distinctCodes)
        {
            var role = await _roleRepo.GetByCodeAsync(code, ct)
                ?? throw new NotFoundException("Role", code);
            resolvedRoleIds.Add(role.RoleId);
        }

        var current = await _userRoleRepo.GetByUserIdAsync(userId, ct);
        var previousRoleIds = current.Where(ur => ur.IsActive).Select(ur => ur.RoleId).ToList();
        var now = DateTime.UtcNow;

        foreach (var ur in current.Where(ur => ur.IsActive && !resolvedRoleIds.Contains(ur.RoleId)))
        {
            ur.IsActive = false;
            ur.EndDate = now;
            await _userRoleRepo.UpdateAsync(ur, ct);
        }

        foreach (var roleId in resolvedRoleIds)
        {
            var existing = current.FirstOrDefault(ur => ur.RoleId == roleId);
            if (existing is null)
            {
                await _userRoleRepo.AddAsync(new UserRole
                {
                    UserRoleId = Guid.NewGuid(),
                    UserId = userId,
                    RoleId = roleId,
                    AssignedByUserId = actingUserId,
                    StartDate = now,
                    IsActive = true
                }, ct);
            }
            else if (!existing.IsActive)
            {
                existing.IsActive = true;
                existing.StartDate = now;
                existing.EndDate = null;
                existing.AssignedByUserId = actingUserId;
                await _userRoleRepo.UpdateAsync(existing, ct);
            }
        }

        await _changeAudit.LogAsync(actingUserId ?? Guid.Empty, "User", userId, "RolesChange",
            new Dictionary<string, object?> { ["roleIds"] = previousRoleIds },
            new Dictionary<string, object?> { ["roleIds"] = resolvedRoleIds },
            ct);
    }

    private static UserDto ToDto(User u) => new(
        u.UserId, u.InstitutionId, u.Email,
        u.FirstName, u.LastName, u.Phone,
        u.AccountStatus, u.CreatedAt);
}
