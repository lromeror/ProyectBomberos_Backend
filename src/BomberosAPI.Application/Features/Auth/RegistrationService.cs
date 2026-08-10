using System.Net;
using System.Security.Cryptography;
using System.Text;
using BomberosAPI.Application.Common.Exceptions;
using BomberosAPI.Application.Common.Interfaces;
using BomberosAPI.Application.Features.HealthPersonnel;
using BomberosAPI.Application.Features.TraineeFirefighters;
using BomberosAPI.Domain.Entities;
using BomberosAPI.Domain.Repositories;

namespace BomberosAPI.Application.Features.Auth;

/// <summary>
/// Orquesta la creación de cuentas: activar una cuenta creada directamente (ej.
/// "Agregar Personal", que antes no dejaba ninguna forma de conseguir contraseña) y
/// completar el registro de una cuenta nueva a partir de una invitación por correo
/// (cualquier rol). Ambos flujos terminan en el mismo lugar: una `UserCredential` real
/// y `User.EmailVerified = true` — llegar hasta aquí ya prueba que el correo es válido.
/// </summary>
public class RegistrationService : IRegistrationService
{
    private const int MinPasswordLength = 8;

    private readonly IAuthRepository _authRepo;
    private readonly IUserRepository _userRepo;
    private readonly IInvitationRepository _invitationRepo;
    private readonly IRoleRepository _roleRepo;
    private readonly IUserRoleRepository _userRoleRepo;
    private readonly TraineeFirefighterService _traineeService;
    private readonly HealthPersonnelService _healthPersonnelService;
    private readonly IPasswordHasher _passwordHasher;
    private readonly IEmailSender _emailSender;
    private readonly IAppUrlProvider _appUrl;

    public RegistrationService(
        IAuthRepository authRepo,
        IUserRepository userRepo,
        IInvitationRepository invitationRepo,
        IRoleRepository roleRepo,
        IUserRoleRepository userRoleRepo,
        TraineeFirefighterService traineeService,
        HealthPersonnelService healthPersonnelService,
        IPasswordHasher passwordHasher,
        IEmailSender emailSender,
        IAppUrlProvider appUrl)
    {
        _authRepo = authRepo;
        _userRepo = userRepo;
        _invitationRepo = invitationRepo;
        _roleRepo = roleRepo;
        _userRoleRepo = userRoleRepo;
        _traineeService = traineeService;
        _healthPersonnelService = healthPersonnelService;
        _passwordHasher = passwordHasher;
        _emailSender = emailSender;
        _appUrl = appUrl;
    }

    /// <summary>Genera un token de activación y envía el correo — se llama justo después de crear una cuenta directamente (UserService.CreateAsync).</summary>
    public async Task SendActivationEmailAsync(Guid userId, string email, string firstName, CancellationToken ct = default)
    {
        var rawToken = GenerateRawToken();
        await _authRepo.AddActivationTokenAsync(new AccountActivationToken
        {
            AccountActivationTokenId = Guid.NewGuid(),
            UserId = userId,
            TokenHash = HashToken(rawToken),
            Status = "pending",
            ExpiresAt = DateTime.UtcNow.AddDays(7),
            CreatedAt = DateTime.UtcNow,
        }, ct);

        var link = $"{_appUrl.WebBaseUrl}/completar-registro?type=activate&token={Uri.EscapeDataString(rawToken)}";
        await _emailSender.SendAsync(email, "Activa tu cuenta — FireHealth App",
            BuildActivationEmailHtml(firstName, link), ct);
    }

    public async Task ActivateAccountAsync(ActivateAccountRequest request, CancellationToken ct = default)
    {
        EnsureValidPassword(request.Password);

        var token = await _authRepo.FindValidActivationTokenByHashAsync(HashToken(request.Token), ct)
            ?? throw new BusinessRuleException("El enlace de activación es inválido o ya expiró.");

        var user = await _authRepo.FindUserByIdAsync(token.UserId, ct)
            ?? throw new NotFoundException("User", token.UserId);

        await UpsertCredentialAsync(user.UserId, request.Password, ct);

        user.EmailVerified = true;
        await _authRepo.UpdateUserAsync(user, ct);

        token.Status = "used";
        token.UsedAt = DateTime.UtcNow;
        await _authRepo.UpdateActivationTokenAsync(token, ct);
    }

    public async Task<InvitationPreviewResult> PreviewInvitationAsync(string token, CancellationToken ct = default)
    {
        var invitation = await GetPendingInvitationByTokenAsync(token, ct);
        string? roleCode = null;
        if (invitation.TargetRoleId is not null)
        {
            var role = await _roleRepo.GetByIdAsync(invitation.TargetRoleId.Value, ct);
            roleCode = role?.Code;
        }
        return new InvitationPreviewResult(invitation.TargetEmail, roleCode);
    }

    public async Task CompleteRegistrationAsync(CompleteRegistrationRequest request, CancellationToken ct = default)
    {
        EnsureValidPassword(request.Password);
        if (string.IsNullOrWhiteSpace(request.FirstName) || string.IsNullOrWhiteSpace(request.LastName))
            throw new BusinessRuleException("Nombre y apellido son obligatorios.");

        var invitation = await GetPendingInvitationByTokenAsync(request.Token, ct);

        if (invitation.TargetUserId is not null)
            throw new BusinessRuleException("Esta invitación ya está asociada a una cuenta existente.");
        if (await _userRepo.ExistsByEmailAsync(invitation.TargetEmail, ct))
            throw new ConflictException("Ya existe una cuenta con este correo.");

        var sender = await _authRepo.FindUserByIdAsync(invitation.SenderUserId, ct)
            ?? throw new NotFoundException("User", invitation.SenderUserId);

        Role? role = invitation.TargetRoleId is not null
            ? await _roleRepo.GetByIdAsync(invitation.TargetRoleId.Value, ct)
            : null;

        var newUser = new User
        {
            UserId = Guid.NewGuid(),
            InstitutionId = sender.InstitutionId,
            Email = invitation.TargetEmail,
            FirstName = request.FirstName.Trim(),
            LastName = request.LastName.Trim(),
            Phone = string.IsNullOrWhiteSpace(request.Phone) ? null : request.Phone.Trim(),
            AccountStatus = "active",
            EmailVerified = true,
            CreatedAt = DateTime.UtcNow,
        };
        await _userRepo.AddAsync(newUser, ct);
        await UpsertCredentialAsync(newUser.UserId, request.Password, ct);

        if (role?.Code == BomberosAPI.Application.Common.Constants.Roles.FirefighterTrainee)
        {
            if (request.BirthDate is null || string.IsNullOrWhiteSpace(request.Sex))
                throw new BusinessRuleException("Fecha de nacimiento y sexo son obligatorios.");

            await _traineeService.CreateAsync(new CreateTraineeFirefighterRequest(
                newUser.UserId,
                null, // se genera solo — ver TraineeFirefighterService.CreateAsync
                request.BirthDate.Value,
                request.Sex,
                string.IsNullOrWhiteSpace(request.BloodType) ? null : request.BloodType.Trim(),
                string.IsNullOrWhiteSpace(request.EmergencyContactName) ? null : request.EmergencyContactName.Trim(),
                string.IsNullOrWhiteSpace(request.EmergencyContactPhone) ? null : request.EmergencyContactPhone.Trim()
            ), ct);
        }
        else if (role?.Code == BomberosAPI.Application.Common.Constants.Roles.Medical)
        {
            if (string.IsNullOrWhiteSpace(request.Profession))
                throw new BusinessRuleException("Selecciona una profesión.");

            await _healthPersonnelService.CreateAsync(new CreateHealthPersonnelRequest(
                newUser.UserId,
                request.Profession.Trim(),
                string.IsNullOrWhiteSpace(request.Specialty) ? null : request.Specialty.Trim(),
                string.IsNullOrWhiteSpace(request.LicenseNumber) ? null : request.LicenseNumber.Trim(),
                false
            ), ct);
        }

        if (role is not null)
        {
            await _userRoleRepo.AddAsync(new UserRole
            {
                UserRoleId = Guid.NewGuid(),
                UserId = newUser.UserId,
                RoleId = role.RoleId,
                AssignedByUserId = invitation.SenderUserId,
                StartDate = DateTime.UtcNow,
                IsActive = true,
            }, ct);
        }

        invitation.Status = "Accepted";
        invitation.TargetUserId = newUser.UserId;
        invitation.RespondedAt = DateTime.UtcNow;
        await _invitationRepo.UpdateAsync(invitation, ct);
    }

    private async Task<Invitation> GetPendingInvitationByTokenAsync(string token, CancellationToken ct)
    {
        var invitation = await _invitationRepo.GetByTokenHashAsync(HashToken(token), ct)
            ?? throw new BusinessRuleException("El enlace de invitación es inválido o ya expiró.");
        if (invitation.Status != "Pending")
            throw new BusinessRuleException("Esta invitación ya no está disponible.");
        if (invitation.ExpiresAt < DateTime.UtcNow)
            throw new BusinessRuleException("Esta invitación expiró.");
        return invitation;
    }

    private async Task UpsertCredentialAsync(Guid userId, string password, CancellationToken ct)
    {
        var existing = await _authRepo.FindCredentialByUserIdAsync(userId, ct);
        if (existing is not null)
        {
            existing.PasswordHash = _passwordHasher.Hash(password);
            existing.LastPasswordChangeAt = DateTime.UtcNow;
            await _authRepo.UpdateCredentialAsync(existing, ct);
            return;
        }

        await _authRepo.AddCredentialAsync(new UserCredential
        {
            UserCredentialId = Guid.NewGuid(),
            UserId = userId,
            PasswordHash = _passwordHasher.Hash(password),
            HashAlgorithm = "bcrypt",
            MfaEnabled = false,
            FailedAttempts = 0,
            LastPasswordChangeAt = DateTime.UtcNow,
        }, ct);
    }

    private static void EnsureValidPassword(string password)
    {
        if (string.IsNullOrWhiteSpace(password) || password.Length < MinPasswordLength)
            throw new BusinessRuleException($"La contraseña debe tener al menos {MinPasswordLength} caracteres.");
    }

    private static string GenerateRawToken() =>
        Convert.ToBase64String(RandomNumberGenerator.GetBytes(32));

    private static string HashToken(string rawToken) =>
        Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(rawToken)));

    private static string BuildActivationEmailHtml(string firstName, string link)
    {
        var safeName = WebUtility.HtmlEncode(firstName);
        return "<div style=\"font-family: -apple-system, Segoe UI, Roboto, sans-serif; max-width: 480px; margin: 0 auto;\">"
             + "<h2 style=\"color: #D9531F;\">Activa tu cuenta</h2>"
             + $"<p>Hola {safeName},</p>"
             + "<p>Se creó una cuenta para ti en <strong>FireHealth App</strong>. Elige tu contraseña para empezar a usarla:</p>"
             + $"<p style=\"margin: 24px 0;\"><a href=\"{link}\" style=\"background: #D9531F; color: #fff; padding: 12px 20px; border-radius: 8px; text-decoration: none; font-weight: 700;\">Activar mi cuenta</a></p>"
             + "<p style=\"color: #666; font-size: 13px;\">Este enlace vence en 7 días. Si no esperabas este correo, puedes ignorarlo.</p>"
             + "</div>";
    }
}
