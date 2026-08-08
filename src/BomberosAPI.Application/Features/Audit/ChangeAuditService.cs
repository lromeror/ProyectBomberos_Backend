using BomberosAPI.Domain.Repositories;
using ChangeAuditEntity = BomberosAPI.Domain.Entities.ChangeAudit;

namespace BomberosAPI.Application.Features.Audit;

public class ChangeAuditService
{
    private const int MaxRecentEntries = 200;

    private readonly IChangeAuditRepository _repo;

    public ChangeAuditService(IChangeAuditRepository repo) => _repo = repo;

    /// <summary>
    /// Registra un cambio de datos. `previousValues`/`newValues` deben ser diccionarios
    /// pequeños y explícitos (solo los campos que cambiaron) — nunca la entidad
    /// completa, para no terminar guardando datos sensibles (ej. PasswordHash) en el
    /// log de auditoría.
    /// </summary>
    public async Task LogAsync(
        Guid actorUserId,
        string entity,
        Guid entityId,
        string operation,
        IReadOnlyDictionary<string, object?>? previousValues,
        IReadOnlyDictionary<string, object?>? newValues,
        CancellationToken ct = default)
    {
        var entry = new ChangeAuditEntity
        {
            ChangeAuditId = Guid.NewGuid(),
            ActorUserId = actorUserId,
            Entity = entity,
            EntityId = entityId,
            Operation = operation,
            PreviousValuesJson = previousValues is null ? null : System.Text.Json.JsonSerializer.Serialize(previousValues),
            NewValuesJson = newValues is null ? null : System.Text.Json.JsonSerializer.Serialize(newValues),
            OccurredAt = DateTime.UtcNow
        };

        await _repo.AddAsync(entry, ct);
    }

    public async Task<IReadOnlyList<ChangeAuditDto>> GetRecentAsync(string? entity, CancellationToken ct = default)
    {
        var items = await _repo.GetRecentAsync(entity, MaxRecentEntries, ct);
        return items.Select(ToDto).ToList();
    }

    private static ChangeAuditDto ToDto(ChangeAuditEntity a) => new(
        a.ChangeAuditId, a.ActorUserId, a.Entity, a.EntityId, a.Operation,
        a.PreviousValuesJson, a.NewValuesJson, a.OccurredAt);
}
