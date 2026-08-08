using BomberosAPI.Domain.Repositories;
using AccessAuditEntity = BomberosAPI.Domain.Entities.AccessAudit;

namespace BomberosAPI.Application.Features.Audit;

public class AccessAuditService
{
    private const int MaxRecentEntries = 200;

    private readonly IAccessAuditRepository _repo;

    public AccessAuditService(IAccessAuditRepository repo) => _repo = repo;

    public async Task LogAsync(Guid actingUserId, LogAccessAuditRequest request, CancellationToken ct = default)
    {
        var entry = new AccessAuditEntity
        {
            AccessAuditId = Guid.NewGuid(),
            UserId = actingUserId,
            ResourceType = request.ResourceType,
            ResourceId = request.ResourceId,
            Event = string.IsNullOrWhiteSpace(request.Action) ? "ACCESS" : request.Action,
            Success = true,
            OccurredAt = DateTime.UtcNow
        };

        await _repo.AddAsync(entry, ct);
    }

    public async Task<IReadOnlyList<AccessAuditDto>> GetRecentAsync(string? resourceType, CancellationToken ct = default)
    {
        var items = await _repo.GetRecentAsync(resourceType, MaxRecentEntries, ct);
        return items.Select(ToDto).ToList();
    }

    private static AccessAuditDto ToDto(AccessAuditEntity a) => new(
        a.AccessAuditId, a.UserId, a.ResourceType, a.ResourceId, a.Event, a.Success, a.OccurredAt);
}
