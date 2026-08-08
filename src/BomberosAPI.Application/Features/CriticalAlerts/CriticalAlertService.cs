using BomberosAPI.Application.Common.Exceptions;
using BomberosAPI.Domain.Repositories;
using CriticalAlertEntity = BomberosAPI.Domain.Entities.CriticalAlert;

namespace BomberosAPI.Application.Features.CriticalAlerts;

public class CriticalAlertService
{
    private static readonly HashSet<string> ValidSeverities = new(StringComparer.OrdinalIgnoreCase)
    { "Low", "Medium", "High", "Critical" };

    private readonly ICriticalAlertRepository _repo;
    private readonly ISessionParticipantRepository _participantRepo;

    public CriticalAlertService(ICriticalAlertRepository repo, ISessionParticipantRepository participantRepo)
    {
        _repo = repo;
        _participantRepo = participantRepo;
    }

    public async Task<CriticalAlertDto> CreateAsync(CreateCriticalAlertRequest request, CancellationToken ct = default)
    {
        if (string.IsNullOrWhiteSpace(request.AlertType))
            throw new BusinessRuleException("AlertType is required.");
        if (!ValidSeverities.Contains(request.Severity))
            throw new BusinessRuleException($"'{request.Severity}' is not a valid severity.");
        if (await _participantRepo.GetByIdAsync(request.SessionParticipantId, ct) is null)
            throw new NotFoundException("SessionParticipant", request.SessionParticipantId);

        var alert = new CriticalAlertEntity
        {
            CriticalAlertId = Guid.NewGuid(),
            SessionParticipantId = request.SessionParticipantId,
            VitalSignsMeasurementId = request.VitalSignsMeasurementId,
            SymptomReportId = request.SymptomReportId,
            EnvironmentalDataId = request.EnvironmentalDataId,
            AlertType = request.AlertType,
            Severity = request.Severity,
            Status = "Open",
            Description = request.Description,
            GeneratedAt = DateTime.UtcNow
        };

        await _repo.AddAsync(alert, ct);
        return ToDto(alert);
    }

    public async Task<IReadOnlyList<CriticalAlertDto>> GetAllAsync(string? status, CancellationToken ct = default)
    {
        var items = await _repo.GetAllAsync(status, ct);
        return items.Select(ToDto).ToList();
    }

    public async Task<CriticalAlertDto> AttendAsync(Guid id, Guid attendedByUserId, CancellationToken ct = default)
    {
        var alert = await _repo.GetByIdAsync(id, ct)
            ?? throw new NotFoundException("CriticalAlert", id);

        if (alert.Status == "Attended")
            throw new ConflictException("This alert has already been attended.");

        alert.Status = "Attended";
        alert.AttendedByUserId = attendedByUserId;
        alert.AttendedAt = DateTime.UtcNow;

        await _repo.UpdateAsync(alert, ct);
        return ToDto(alert);
    }

    private static CriticalAlertDto ToDto(CriticalAlertEntity a) => new(
        a.CriticalAlertId, a.SessionParticipantId, a.VitalSignsMeasurementId, a.SymptomReportId,
        a.EnvironmentalDataId, a.AttendedByUserId, a.AlertType, a.Severity, a.Status, a.Description,
        a.GeneratedAt, a.AttendedAt);
}
