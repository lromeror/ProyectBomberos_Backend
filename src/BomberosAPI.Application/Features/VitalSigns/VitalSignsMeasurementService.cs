using BomberosAPI.Application.Common.Exceptions;
using BomberosAPI.Domain.Entities;
using BomberosAPI.Domain.Repositories;
using FluentValidation;
using AppValidationException = BomberosAPI.Application.Common.Exceptions.ValidationException;

namespace BomberosAPI.Application.Features.VitalSigns;

public class VitalSignsMeasurementService
{
    private readonly IVitalSignsMeasurementRepository _repo;
    private readonly ISessionParticipantRepository _participantRepo;
    private readonly IHealthPersonnelRepository _hpRepo;
    private readonly ITrainingSessionRepository _sessionRepo;
    private readonly IValidator<CreateVitalSignsMeasurementRequest> _createValidator;

    public VitalSignsMeasurementService(
        IVitalSignsMeasurementRepository repo,
        ISessionParticipantRepository participantRepo,
        IHealthPersonnelRepository hpRepo,
        ITrainingSessionRepository sessionRepo,
        IValidator<CreateVitalSignsMeasurementRequest> createValidator)
    {
        _repo = repo;
        _participantRepo = participantRepo;
        _hpRepo = hpRepo;
        _sessionRepo = sessionRepo;
        _createValidator = createValidator;
    }

    public async Task<IReadOnlyList<VitalSignsMeasurementDto>> GetAllAsync(CancellationToken ct = default)
    {
        var items = await _repo.GetAllAsync(ct);
        return items.Select(ToDto).ToList();
    }

    public async Task<VitalSignsMeasurementDto> GetByIdAsync(Guid id, CancellationToken ct = default)
    {
        var v = await _repo.GetByIdAsync(id, ct)
            ?? throw new NotFoundException("VitalSignsMeasurement", id);
        return ToDto(v);
    }

    public async Task<IReadOnlyList<VitalSignsMeasurementDto>> GetByParticipantAsync(Guid participantId, CancellationToken ct = default)
    {
        var items = await _repo.GetByParticipantAsync(participantId, ct);
        return items.Select(ToDto).ToList();
    }

    public async Task<IReadOnlyList<VitalSignsHistoryDto>> GetByTraineeAsync(Guid traineeFirefighterId, CancellationToken ct = default)
    {
        var participants = (await _participantRepo.GetByTraineeAsync(traineeFirefighterId, ct))
            .ToDictionary(p => p.SessionParticipantId);

        if (participants.Count == 0) return [];

        var vitals = await _repo.GetByTraineeAsync(traineeFirefighterId, ct);

        var uniqueSessionIds = participants.Values
            .Select(p => p.TrainingSessionId)
            .Distinct();

        // Las sesiones se cargan SECUENCIALMENTE, no con Task.WhenAll.
        // El DbContext es scoped y EF Core no admite operaciones concurrentes sobre la
        // misma instancia: en cuanto un aspirante participaba en dos sesiones, WhenAll
        // lanzaba las consultas en paralelo y reventaba con
        // "A second operation was started on this context instance...".
        var sessions = new Dictionary<Guid, TrainingSession>();
        foreach (var sessionId in uniqueSessionIds)
        {
            var session = await _sessionRepo.GetByIdAsync(sessionId, ct);
            if (session is not null) sessions[session.TrainingSessionId] = session;
        }

        return vitals
            .Where(v => participants.ContainsKey(v.SessionParticipantId))
            .Select(v =>
            {
                var participant = participants[v.SessionParticipantId];
                sessions.TryGetValue(participant.TrainingSessionId, out var session);
                return new VitalSignsHistoryDto(
                    v.VitalSignsMeasurementId,
                    v.SessionParticipantId,
                    participant.TrainingSessionId,
                    session?.Title ?? string.Empty,
                    session?.ScheduledStart ?? v.TakenAt,
                    v.HeartRate,
                    v.SystolicPressure,
                    v.DiastolicPressure,
                    v.TemperatureC,
                    v.Spo2,
                    v.PracticeRole,
                    v.IsSmoker,
                    v.ExposedToSmoke48h,
                    v.TakenAt);
            })
            .OrderBy(h => h.SessionDate)
            .ToList();
    }

    public async Task<VitalSignsMeasurementDto> CreateAsync(CreateVitalSignsMeasurementRequest request, CancellationToken ct = default)
    {
        var validation = await _createValidator.ValidateAsync(request, ct);
        if (!validation.IsValid)
        {
            var errors = validation.Errors
                .GroupBy(e => e.PropertyName)
                .ToDictionary(g => g.Key, g => g.Select(e => e.ErrorMessage).ToArray());
            throw new AppValidationException(errors);
        }

        if (await _participantRepo.GetByIdAsync(request.SessionParticipantId, ct) is null)
            throw new NotFoundException("SessionParticipant", request.SessionParticipantId);

        if (await _hpRepo.GetByIdAsync(request.RegisteredByHealthPersonnelId, ct) is null)
            throw new NotFoundException("HealthPersonnel", request.RegisteredByHealthPersonnelId);

        var v = new VitalSignsMeasurement
        {
            VitalSignsMeasurementId = Guid.NewGuid(),
            SessionParticipantId = request.SessionParticipantId,
            RegisteredByHealthPersonnelId = request.RegisteredByHealthPersonnelId,
            HeartRate = request.HeartRate,
            SystolicPressure = request.SystolicPressure,
            DiastolicPressure = request.DiastolicPressure,
            TemperatureC = request.TemperatureC,
            Spo2 = request.Spo2,
            PracticeRole = request.PracticeRole,
            IsSmoker = request.IsSmoker,
            ExposedToSmoke48h = request.ExposedToSmoke48h,
            TakenAt = DateTime.UtcNow
        };

        await _repo.AddAsync(v, ct);
        return ToDto(v);
    }

    private static VitalSignsMeasurementDto ToDto(VitalSignsMeasurement v) => new(
        v.VitalSignsMeasurementId,
        v.SessionParticipantId,
        v.RegisteredByHealthPersonnelId,
        v.HeartRate,
        v.SystolicPressure,
        v.DiastolicPressure,
        v.TemperatureC,
        v.Spo2,
        v.PracticeRole,
        v.IsSmoker,
        v.ExposedToSmoke48h,
        v.TakenAt);
}