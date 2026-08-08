using System.Security.Cryptography;
using System.Text;
using BomberosAPI.Application.Common.Exceptions;
using BomberosAPI.Application.Common.Interfaces;
using BomberosAPI.Domain.Entities;
using FluentValidation;
using AppValidationException = BomberosAPI.Application.Common.Exceptions.ValidationException;

namespace BomberosAPI.Application.Features.Auth;

public class AuthService
{
    // Sin esto, `LockedUntil`/`FailedAttempts` existían en el esquema pero nunca se
    // escribían — el login no tenía ninguna protección real contra fuerza bruta.
    private const int MaxFailedAttempts = 5;
    private static readonly TimeSpan LockoutDuration = TimeSpan.FromMinutes(15);

    private readonly IAuthRepository _repo;
    private readonly IPasswordHasher _passwordHasher;
    private readonly IJwtTokenService _jwtService;
    private readonly IValidator<LoginRequest> _loginValidator;
    private readonly IValidator<ForgotPasswordRequest> _forgotValidator;
    private readonly IValidator<ResetPasswordRequest> _resetValidator;
    private readonly IValidator<ChangePasswordRequest> _changePasswordValidator;
    private readonly AuthSessionService _authSessionService;

    public AuthService(
        IAuthRepository repo,
        IPasswordHasher passwordHasher,
        IJwtTokenService jwtService,
        IValidator<LoginRequest> loginValidator,
        IValidator<ForgotPasswordRequest> forgotValidator,
        IValidator<ResetPasswordRequest> resetValidator,
        IValidator<ChangePasswordRequest> changePasswordValidator,
        AuthSessionService authSessionService)
    {
        _repo = repo;
        _passwordHasher = passwordHasher;
        _jwtService = jwtService;
        _loginValidator = loginValidator;
        _forgotValidator = forgotValidator;
        _resetValidator = resetValidator;
        _changePasswordValidator = changePasswordValidator;
        _authSessionService = authSessionService;
    }

    public async Task<LoginResult> LoginAsync(LoginRequest request, CancellationToken ct = default, string? ip = null, string? userAgent = null)
    {
        var validation = await _loginValidator.ValidateAsync(request, ct);
        if (!validation.IsValid)
        {
            var errors = validation.Errors
                .GroupBy(e => e.PropertyName)
                .ToDictionary(g => g.Key, g => g.Select(e => e.ErrorMessage).ToArray());
            throw new AppValidationException(errors);
        }

        var user = await _repo.FindUserByEmailAsync(request.Email, ct);

        // Mismo mensaje para email inexistente y password incorrecta — no revela cual fallo
        if (user is null)
            throw new UnauthorizedException("Invalid credentials.");

        if (user.AccountStatus != "active")
            throw new BusinessRuleException("Account is inactive. Contact your administrator.");

        var credential = await _repo.FindCredentialByUserIdAsync(user.UserId, ct);

        if (credential is null)
            throw new UnauthorizedException("Invalid credentials.");

        // Se revisa el bloqueo ANTES de verificar la contraseña: si se revisara después
        // (como estaba antes), una contraseña correcta contra una cuenta bloqueada
        // devolvía un mensaje distinto ("cuenta bloqueada") al de una incorrecta
        // ("credenciales inválidas") — eso le confirma a quien está probando
        // contraseñas cuándo acertó, aunque la cuenta siga bloqueada.
        if (credential.LockedUntil.HasValue && credential.LockedUntil > DateTime.UtcNow)
            throw new BusinessRuleException("Account is temporarily locked. Try again later.");

        if (!_passwordHasher.Verify(request.Password, credential.PasswordHash))
        {
            credential.FailedAttempts += 1;
            if (credential.FailedAttempts >= MaxFailedAttempts)
            {
                credential.LockedUntil = DateTime.UtcNow.Add(LockoutDuration);
                credential.FailedAttempts = 0;
            }
            await _repo.UpdateCredentialAsync(credential, ct);
            throw new UnauthorizedException("Invalid credentials.");
        }

        if (credential.FailedAttempts > 0 || credential.LockedUntil.HasValue)
        {
            credential.FailedAttempts = 0;
            credential.LockedUntil = null;
            await _repo.UpdateCredentialAsync(credential, ct);
        }

        var roles = await _repo.GetActiveRoleCodesByUserIdAsync(user.UserId, ct);
        var (token, expiresAt) = _jwtService.GenerateToken(user, roles);

        await _authSessionService.RecordLoginAsync(user.UserId, null, ip, userAgent, expiresAt, ct);

        return new LoginResult(token, expiresAt, user.UserId, user.Email,
            user.FirstName, user.LastName, roles);
    }

    public async Task<ForgotPasswordResult> RequestPasswordResetAsync(ForgotPasswordRequest request, CancellationToken ct = default)
    {
        var validation = await _forgotValidator.ValidateAsync(request, ct);
        if (!validation.IsValid)
        {
            var errors = validation.Errors
                .GroupBy(e => e.PropertyName)
                .ToDictionary(g => g.Key, g => g.Select(e => e.ErrorMessage).ToArray());
            throw new AppValidationException(errors);
        }

        var user = await _repo.FindUserByEmailAsync(request.Email, ct);

        // Respuesta genérica — no revela si el email existe en el sistema
        if (user is null)
            return new ForgotPasswordResult(string.Empty);

        var rawToken = Convert.ToBase64String(RandomNumberGenerator.GetBytes(32));
        var tokenHash = Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(rawToken)));

        var resetToken = new PasswordResetToken
        {
            PasswordResetTokenId = Guid.NewGuid(),
            UserId    = user.UserId,
            TokenHash = tokenHash,
            Status    = "pending",
            ExpiresAt = DateTime.UtcNow.AddHours(1),
            CreatedAt = DateTime.UtcNow,
        };

        await _repo.AddPasswordResetTokenAsync(resetToken, ct);

        // Sin servicio de email — devuelve el token para uso en desarrollo
        return new ForgotPasswordResult(rawToken);
    }

    public async Task ResetPasswordAsync(ResetPasswordRequest request, CancellationToken ct = default)
    {
        var validation = await _resetValidator.ValidateAsync(request, ct);
        if (!validation.IsValid)
        {
            var errors = validation.Errors
                .GroupBy(e => e.PropertyName)
                .ToDictionary(g => g.Key, g => g.Select(e => e.ErrorMessage).ToArray());
            throw new AppValidationException(errors);
        }

        var tokenHash = Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(request.Token)));
        var resetToken = await _repo.FindValidResetTokenByHashAsync(tokenHash, ct);

        if (resetToken is null)
            throw new BusinessRuleException("El token es inválido o ya expiró.");

        var credential = await _repo.FindCredentialByUserIdAsync(resetToken.UserId, ct);
        if (credential is null)
            throw new NotFoundException("Credenciales de usuario no encontradas.");

        credential.PasswordHash = _passwordHasher.Hash(request.NewPassword);
        credential.LastPasswordChangeAt = DateTime.UtcNow;
        await _repo.UpdateCredentialAsync(credential, ct);

        resetToken.Status = "used";
        resetToken.UsedAt = DateTime.UtcNow;
        await _repo.UpdatePasswordResetTokenAsync(resetToken, ct);
    }

    /// <summary>Cambia la contraseña del usuario autenticado, verificando la contraseña actual primero.</summary>
    public async Task ChangePasswordAsync(Guid userId, ChangePasswordRequest request, CancellationToken ct = default)
    {
        var validation = await _changePasswordValidator.ValidateAsync(request, ct);
        if (!validation.IsValid)
        {
            var errors = validation.Errors
                .GroupBy(e => e.PropertyName)
                .ToDictionary(g => g.Key, g => g.Select(e => e.ErrorMessage).ToArray());
            throw new AppValidationException(errors);
        }

        var credential = await _repo.FindCredentialByUserIdAsync(userId, ct)
            ?? throw new NotFoundException("User credentials", userId);

        if (!_passwordHasher.Verify(request.CurrentPassword, credential.PasswordHash))
            throw new UnauthorizedException("La contraseña actual es incorrecta.");

        credential.PasswordHash = _passwordHasher.Hash(request.NewPassword);
        credential.LastPasswordChangeAt = DateTime.UtcNow;
        await _repo.UpdateCredentialAsync(credential, ct);
    }

    public string HashPassword(string plainPassword) =>
        _passwordHasher.Hash(plainPassword);
}
