using BomberosAPI.Application.Common.Exceptions;
using BomberosAPI.Domain.Repositories;
using DSARRequestEntity = BomberosAPI.Domain.Entities.DSARRequest;

namespace BomberosAPI.Application.Features.DSARRequests;

public class DSARRequestService
{
    // Derechos habeas-data / GDPR-style que el sistema reconoce — sin esta lista, el
    // endpoint aceptaría cualquier texto como "tipo de derecho".
    private static readonly HashSet<string> ValidRightTypes = new(StringComparer.OrdinalIgnoreCase)
    { "Access", "Rectification", "Erasure", "Portability", "Restriction", "Objection" };

    private static readonly HashSet<string> ValidStatuses = new(StringComparer.OrdinalIgnoreCase)
    { "Pending", "InProgress", "Completed", "Rejected" };

    // Plazo típico de respuesta para este tipo de solicitud (habeas data / GDPR-style).
    private const int LegalDeadlineDays = 30;

    private readonly IDSARRequestRepository _repo;

    public DSARRequestService(IDSARRequestRepository repo) => _repo = repo;

    public async Task<DSARRequestDto> CreateAsync(Guid userId, CreateDSARRequestRequest request, CancellationToken ct = default)
    {
        if (!ValidRightTypes.Contains(request.RightType))
            throw new BusinessRuleException($"'{request.RightType}' is not a valid right type.");

        var entity = new DSARRequestEntity
        {
            DSARRequestId = Guid.NewGuid(),
            UserId = userId,
            RightType = request.RightType,
            Status = "Pending",
            Description = request.Description,
            RequestedAt = DateTime.UtcNow,
            LegalDeadlineAt = DateTime.UtcNow.AddDays(LegalDeadlineDays)
        };

        await _repo.AddAsync(entity, ct);
        return ToDto(entity);
    }

    public async Task<IReadOnlyList<DSARRequestDto>> GetMineAsync(Guid userId, CancellationToken ct = default)
    {
        var items = await _repo.GetByUserIdAsync(userId, ct);
        return items.Select(ToDto).ToList();
    }

    public async Task<IReadOnlyList<DSARRequestDto>> GetAllAsync(CancellationToken ct = default)
    {
        var items = await _repo.GetAllAsync(ct);
        return items.Select(ToDto).ToList();
    }

    public async Task<DSARRequestDto> RespondAsync(Guid id, Guid managedByUserId, RespondDSARRequestRequest request, CancellationToken ct = default)
    {
        if (!ValidStatuses.Contains(request.Status))
            throw new BusinessRuleException($"'{request.Status}' is not a valid status.");

        var entity = await _repo.GetByIdAsync(id, ct)
            ?? throw new NotFoundException("DSARRequest", id);

        entity.ManagedByUserId = managedByUserId;
        entity.Status = request.Status;
        entity.Response = request.Response;
        entity.RespondedAt = DateTime.UtcNow;

        await _repo.UpdateAsync(entity, ct);
        return ToDto(entity);
    }

    private static DSARRequestDto ToDto(DSARRequestEntity r) => new(
        r.DSARRequestId, r.UserId, r.ManagedByUserId, r.RightType, r.Status,
        r.Description, r.Response, r.RequestedAt, r.RespondedAt, r.LegalDeadlineAt);
}
