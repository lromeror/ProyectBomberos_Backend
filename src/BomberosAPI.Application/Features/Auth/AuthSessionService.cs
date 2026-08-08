using BomberosAPI.Domain.Repositories;
using AuthSessionEntity = BomberosAPI.Domain.Entities.AuthSession;

namespace BomberosAPI.Application.Features.Auth;

public class AuthSessionService
{
    private const int MaxRecentEntries = 200;

    private readonly IAuthSessionRepository _repo;

    public AuthSessionService(IAuthSessionRepository repo) => _repo = repo;

    /// <summary>
    /// Registra una sesión de login. No respalda la autenticación en sí (el JWT sigue
    /// siendo stateless y no se valida contra esta tabla) — es puramente un registro de
    /// "quién entró, desde dónde y cuándo" para que un SYSTEM_ADMIN pueda auditarlo.
    /// </summary>
    public async Task RecordLoginAsync(Guid userId, string? device, string? ip, string? userAgent, DateTime expiresAt, CancellationToken ct = default)
    {
        var session = new AuthSessionEntity
        {
            AuthSessionId = Guid.NewGuid(),
            UserId = userId,
            // No hay refresh token real (el JWT es stateless) — se guarda un
            // identificador opaco propio de este registro, nunca entregado al cliente.
            RefreshTokenHash = Guid.NewGuid().ToString("N"),
            Device = device,
            Ip = ip,
            UserAgent = userAgent,
            Status = "Active",
            CreatedAt = DateTime.UtcNow,
            ExpiresAt = expiresAt
        };

        await _repo.AddAsync(session, ct);
    }

    /// <summary>Cierra todas las sesiones activas del usuario (logout).</summary>
    public async Task CloseAllForUserAsync(Guid userId, CancellationToken ct = default)
    {
        var active = await _repo.GetActiveByUserIdAsync(userId, ct);
        var now = DateTime.UtcNow;

        foreach (var session in active)
        {
            session.Status = "Closed";
            session.ClosedAt = now;
            await _repo.UpdateAsync(session, ct);
        }
    }

    public async Task<IReadOnlyList<AuthSessionDto>> GetRecentAsync(Guid? userId, CancellationToken ct = default)
    {
        var items = await _repo.GetRecentAsync(userId, MaxRecentEntries, ct);
        return items.Select(ToDto).ToList();
    }

    private static AuthSessionDto ToDto(AuthSessionEntity s) => new(
        s.AuthSessionId, s.UserId, s.Device, s.Ip, s.UserAgent, s.Status, s.CreatedAt, s.ExpiresAt, s.ClosedAt);
}
