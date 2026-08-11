using System.Security.Cryptography;
using System.Text;
using BomberosAPI.Application.Common.Exceptions;
using BomberosAPI.Application.Common.Interfaces;
using BomberosAPI.Application.Features.Participants;
using BomberosAPI.Domain.Entities;
using BomberosAPI.Domain.Repositories;
using FluentValidation;
using Microsoft.Extensions.Logging;
using AppValidationException = BomberosAPI.Application.Common.Exceptions.ValidationException;

namespace BomberosAPI.Application.Features.Invitations;

public class InvitationService
{
    // Igual que UserService.PrivilegedRoles — solo SYSTEM_ADMIN puede invitar a alguien
    // directo a un rol de este nivel. Evita que ADMIN/CAPACITATOR/FIRE_CHIEF (que también
    // pueden crear invitaciones) se otorguen o le otorguen a otro más privilegio del que
    // deberían poder repartir.
    private static readonly HashSet<string> PrivilegedRoles = new(StringComparer.OrdinalIgnoreCase)
    {
        BomberosAPI.Application.Common.Constants.Roles.SystemAdmin,
        BomberosAPI.Application.Common.Constants.Roles.Admin,
    };

    private readonly IInvitationRepository _repo;
    private readonly ISessionParticipantRepository _participantRepo;
    private readonly ITraineeFirefighterRepository _traineeRepo;
    private readonly IRoleRepository _roleRepo;
    private readonly IEmailSender _emailSender;
    private readonly IAppUrlProvider _appUrl;
    private readonly IValidator<CreateInvitationRequest> _createValidator;
    private readonly ICurrentUserService _currentUser;
    private readonly ILogger<InvitationService> _logger;

    public InvitationService(
        IInvitationRepository repo,
        ISessionParticipantRepository participantRepo,
        ITraineeFirefighterRepository traineeRepo,
        IRoleRepository roleRepo,
        IEmailSender emailSender,
        IAppUrlProvider appUrl,
        IValidator<CreateInvitationRequest> createValidator,
        ICurrentUserService currentUser,
        ILogger<InvitationService> logger)
    {
        _repo = repo;
        _participantRepo = participantRepo;
        _traineeRepo = traineeRepo;
        _roleRepo = roleRepo;
        _emailSender = emailSender;
        _appUrl = appUrl;
        _createValidator = createValidator;
        _currentUser = currentUser;
        _logger = logger;
    }

    public async Task<IReadOnlyList<InvitationDto>> GetAllAsync(CancellationToken ct = default)
    {
        var items = await _repo.GetAllAsync(ct);
        return items.Select(ToDto).ToList();
    }

    public async Task<InvitationDto> GetByIdAsync(Guid id, CancellationToken ct = default)
    {
        var invitation = await _repo.GetByIdAsync(id, ct)
            ?? throw new NotFoundException("Invitation", id);
        return ToDto(invitation);
    }

    public async Task<CreateInvitationResponse> CreateAsync(CreateInvitationRequest request, CancellationToken ct = default)
    {
        var validation = await _createValidator.ValidateAsync(request, ct);
        if (!validation.IsValid)
        {
            var errors = validation.Errors
                .GroupBy(e => e.PropertyName)
                .ToDictionary(g => g.Key, g => g.Select(e => e.ErrorMessage).ToArray());
            throw new AppValidationException(errors);
        }

        Role? targetRole = null;
        if (!string.IsNullOrWhiteSpace(request.TargetRoleCode))
        {
            targetRole = await _roleRepo.GetByCodeAsync(request.TargetRoleCode, ct)
                ?? throw new NotFoundException("Role", request.TargetRoleCode);

            // Solo SYSTEM_ADMIN puede repartir invitaciones a un rol de este nivel — el
            // que crea la invitación decide qué cuenta se crea al aceptarla, así que este
            // es exactamente el mismo punto de escalamiento que UserService.UpdateRolesAsync
            // ya protege para el alta directa.
            if (PrivilegedRoles.Contains(targetRole.Code) && !_currentUser.IsInRole(BomberosAPI.Application.Common.Constants.Roles.SystemAdmin))
                throw new ForbiddenException();
        }

        // Token aleatorio, con hash determinístico (SHA256) para poder buscarlo por su
        // valor plano cuando alguien completa el registro — bcrypt (lo que se usaba
        // antes) genera un salt distinto en cada llamada, así que nunca iba a poder
        // recuperarse por igualdad de hash: GetByTokenHashAsync existía pero jamás
        // encontraba nada.
        var plainToken = Convert.ToBase64String(RandomNumberGenerator.GetBytes(32));
        var tokenHash = Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(plainToken)));

        var invitation = new Invitation
        {
            InvitationId = Guid.NewGuid(),
            SenderUserId = _currentUser.IsAuthenticated
    ? _currentUser.UserId
    : throw new UnauthorizedException("No authenticated user."),
            TargetUserId = request.TargetUserId,
            TrainingSessionId = request.TrainingSessionId,
            TargetRoleId = targetRole?.RoleId,
            TargetEmail = request.TargetEmail,
            InvitationTokenHash = tokenHash,
            Status = "Pending",
            ExpiresAt = request.ExpiresAt,
            CreatedAt = DateTime.UtcNow
        };

        await _repo.AddAsync(invitation, ct);

        // Solo se manda correo cuando la invitación es para alguien SIN cuenta todavía
        // (TargetUserId nulo) — las invitaciones a una sesión para alguien que ya tiene
        // cuenta se ven dentro de la propia app (Cola de Validación / dashboards), no
        // necesitan un enlace externo.
        if (request.TargetUserId is null)
        {
            var link = $"{_appUrl.WebBaseUrl}/completar-registro?type=invite&token={Uri.EscapeDataString(plainToken)}";
            try
            {
                await _emailSender.SendAsync(request.TargetEmail, "Te invitaron a FireHealth App",
                    BuildInvitationEmailHtml(link), ct);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "No se pudo enviar el correo de invitación a {Email}", request.TargetEmail);
            }
        }

        // Return the plain token ONCE — never stored, never retrievable again
        return new CreateInvitationResponse(ToDto(invitation), plainToken);
    }

    private static string BuildInvitationEmailHtml(string link) =>
        "<div style=\"font-family: -apple-system, Segoe UI, Roboto, sans-serif; max-width: 480px; margin: 0 auto;\">"
        + "<h2 style=\"color: #D9531F;\">Te invitaron a FireHealth App</h2>"
        + "<p>Completa tu registro para empezar a usar la app:</p>"
        + $"<p style=\"margin: 24px 0;\"><a href=\"{link}\" style=\"background: #D9531F; color: #fff; padding: 12px 20px; border-radius: 8px; text-decoration: none; font-weight: 700;\">Completar registro</a></p>"
        + "<p style=\"color: #666; font-size: 13px;\">Este enlace vence en 7 días. Si no esperabas este correo, puedes ignorarlo.</p>"
        + "</div>";

    public Task<SessionParticipantDto?> AcceptAsync(Guid id, CancellationToken ct = default) =>
        AcceptCoreAsync(id, enforceRecipient: true, ct);

    // Aprobación por parte del personal de validación (MEDICAL/ADMIN/SYSTEM_ADMIN) en
    // nombre del destinatario real de la invitación — el flujo de la pantalla "Cola de
    // Validaciones" del frontend, que revisa invitaciones de terceros. Deliberadamente
    // NO reutiliza el chequeo de EnsureIsRecipient (que sigue exigiendo, sin cambios,
    // que AcceptAsync/RejectAsync solo los use el propio destinatario): esta es una vía
    // separada y explícita, autorizada solo por rol a nivel de controller
    // ([Authorize(Roles = ...)] en InvitationsController.StaffAccept/StaffReject).
    public Task<SessionParticipantDto?> StaffAcceptAsync(Guid id, CancellationToken ct = default) =>
        AcceptCoreAsync(id, enforceRecipient: false, ct);

    private async Task<SessionParticipantDto?> AcceptCoreAsync(Guid id, bool enforceRecipient, CancellationToken ct)
    {
        var invitation = await _repo.GetByIdAsync(id, ct)
            ?? throw new NotFoundException("Invitation", id);

        if (enforceRecipient) EnsureIsRecipient(invitation);
        else EnsureHasRecipientAccount(invitation);
        EnsurePending(invitation);
        EnsureNotExpired(invitation);

        // Resolve trainee BEFORE committing status change so that if this lookup
        // fails the invitation does not end up stuck in "Accepted" without a participant.
        TraineeFirefighter? trainee = null;
        if (invitation.TrainingSessionId is not null && invitation.TargetUserId is not null)
            trainee = await _traineeRepo.GetByUserIdAsync(invitation.TargetUserId.Value, ct);

        invitation.Status = "Accepted";
        invitation.RespondedAt = DateTime.UtcNow;
        await _repo.UpdateAsync(invitation, ct);

        // Non-trainee users (health personnel, etc.) can accept invitations — no participant record needed.
        if (trainee is null)
            return null;

        var participant = new SessionParticipant
        {
            SessionParticipantId = Guid.NewGuid(),
            TrainingSessionId = invitation.TrainingSessionId!.Value,
            TraineeFirefighterId = trainee.TraineeFirefighterId,
            InvitationId = invitation.InvitationId,
            ParticipationStatus = "Confirmed",
            AttendanceConfirmed = false
        };

        await _participantRepo.AddAsync(participant, ct);

        return new SessionParticipantDto(
            participant.SessionParticipantId,
            participant.TrainingSessionId,
            participant.TraineeFirefighterId,
            participant.InvitationId,
            participant.ParticipationStatus,
            participant.AttendanceConfirmed,
            participant.CheckInAt,
            participant.Observations);
    }

    public Task RejectAsync(Guid id, CancellationToken ct = default) =>
        RejectCoreAsync(id, enforceRecipient: true, ct);

    /// See StaffAcceptAsync — misma vía separada y explícita para el personal de
    /// validación, sin tocar el chequeo EnsureIsRecipient de RejectAsync.
    public Task StaffRejectAsync(Guid id, CancellationToken ct = default) =>
        RejectCoreAsync(id, enforceRecipient: false, ct);

    private async Task RejectCoreAsync(Guid id, bool enforceRecipient, CancellationToken ct)
    {
        var invitation = await _repo.GetByIdAsync(id, ct)
            ?? throw new NotFoundException("Invitation", id);

        if (enforceRecipient) EnsureIsRecipient(invitation);
        else EnsureHasRecipientAccount(invitation);
        EnsurePending(invitation);

        invitation.Status = "Rejected";
        invitation.RespondedAt = DateTime.UtcNow;
        await _repo.UpdateAsync(invitation, ct);
    }

    public async Task RevokeAsync(Guid id, CancellationToken ct = default)
    {
        var invitation = await _repo.GetByIdAsync(id, ct)
            ?? throw new NotFoundException("Invitation", id);

        EnsurePending(invitation);

        invitation.Status = "Revoked";
        invitation.RespondedAt = DateTime.UtcNow;
        await _repo.UpdateAsync(invitation, ct);
    }

    // Sin este chequeo, cualquier cuenta autenticada podía aceptar o rechazar una
    // invitación dirigida a otra persona — no corrompe a quién queda vinculada la
    // participación (eso sigue viniendo de invitation.TargetUserId), pero le permitía a
    // cualquiera decidir por alguien más si asiste o no a una sesión.
    private void EnsureIsRecipient(Invitation invitation)
    {
        if (invitation.TargetUserId is not null && invitation.TargetUserId != _currentUser.UserId)
            throw new ForbiddenException("This invitation is not addressed to you.");
    }

    // El personal de validación solo puede resolver invitaciones de alguien que YA
    // tiene cuenta (invitado a una sesión, por ejemplo). Las invitaciones por correo a
    // gente sin cuenta (TargetUserId nulo) se resuelven solas cuando esa persona
    // completa su registro con el token del correo — si el staff las "aprobara" aquí
    // primero, la invitación pasaría a Accepted sin crear ningún usuario, y el enlace
    // de registro de la persona real quedaría inválido (GetPendingInvitationByTokenAsync
    // exige Status == "Pending").
    private static void EnsureHasRecipientAccount(Invitation invitation)
    {
        if (invitation.TargetUserId is null)
            throw new BusinessRuleException("Esta invitación es para una persona sin cuenta todavía; se resuelve cuando complete su registro, no desde aquí.");
    }

    private static void EnsurePending(Invitation invitation)
    {
        if (invitation.Status != "Pending")
            throw new ConflictException($"Invitation is not pending (current status: {invitation.Status}).");
    }

    private static void EnsureNotExpired(Invitation invitation)
    {
        if (invitation.ExpiresAt < DateTime.UtcNow)
            throw new ConflictException("Invitation has expired.");
    }

    private static InvitationDto ToDto(Invitation i) => new(
        i.InvitationId,
        i.SenderUserId,
        i.TargetUserId,
        i.TrainingSessionId,
        i.TargetRoleId,
        i.TargetEmail,
        i.Status,
        i.ExpiresAt,
        i.RespondedAt,
        i.CreatedAt);
}