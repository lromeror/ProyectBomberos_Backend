using BomberosAPI.Domain.Entities;
using BomberosAPI.Domain.Repositories;

namespace BomberosAPI.Application.Features.Reports;

public class ReportService
{
    private readonly IVitalSignsMeasurementRepository _vitalsRepo;
    private readonly ISessionParticipantRepository _participantRepo;
    private readonly ITraineeFirefighterRepository _traineeRepo;
    private readonly ITrainingSessionRepository _sessionRepo;

    public ReportService(
        IVitalSignsMeasurementRepository vitalsRepo,
        ISessionParticipantRepository participantRepo,
        ITraineeFirefighterRepository traineeRepo,
        ITrainingSessionRepository sessionRepo)
    {
        _vitalsRepo = vitalsRepo;
        _participantRepo = participantRepo;
        _traineeRepo = traineeRepo;
        _sessionRepo = sessionRepo;
    }

    public async Task<ReportSummaryDto> GetSummaryAsync(DateTime? from, DateTime? to, CancellationToken ct = default)
    {
        var vitals = (await _vitalsRepo.GetAllAsync(ct))
            .Where(v => (!from.HasValue || v.TakenAt >= from.Value) && (!to.HasValue || v.TakenAt <= to.Value))
            .ToList();

        var sessions = (await _sessionRepo.GetAllAsync(ct)).ToList();
        var participants = (await _participantRepo.GetAllAsync(ct)).ToList();
        var trainees = (await _traineeRepo.GetAllAsync(ct)).ToList();

        return new ReportSummaryDto(
            sessions.Count,
            participants.Count,
            trainees.Count,
            vitals.Count,
            Avg(vitals.Select(v => v.HeartRate)),
            Avg(vitals.Select(v => v.SystolicPressure)),
            Avg(vitals.Select(v => v.DiastolicPressure)),
            Avg(vitals.Select(v => v.TemperatureC)),
            Avg(vitals.Select(v => v.Spo2)),
            from, to, DateTime.UtcNow);
    }

    public async Task<IReadOnlyList<AnonymizedVitalsRowDto>> GetAnonymizedVitalsExportAsync(DateTime? from, DateTime? to, CancellationToken ct = default)
    {
        var vitals = (await _vitalsRepo.GetAllAsync(ct))
            .Where(v => (!from.HasValue || v.TakenAt >= from.Value) && (!to.HasValue || v.TakenAt <= to.Value))
            .ToList();
        if (vitals.Count == 0) return [];

        var participantsById = (await _participantRepo.GetAllAsync(ct)).ToDictionary(p => p.SessionParticipantId);
        var traineesById = (await _traineeRepo.GetAllAsync(ct)).ToDictionary(t => t.TraineeFirefighterId);

        var sessionIds = participantsById.Values.Select(p => p.TrainingSessionId).Distinct();
        var sessionsById = new Dictionary<Guid, TrainingSession>();
        foreach (var sessionId in sessionIds)
        {
            var session = await _sessionRepo.GetByIdAsync(sessionId, ct);
            if (session is not null) sessionsById[session.TrainingSessionId] = session;
        }

        var rows = new List<AnonymizedVitalsRowDto>();
        foreach (var v in vitals)
        {
            if (!participantsById.TryGetValue(v.SessionParticipantId, out var participant)) continue;
            if (!traineesById.TryGetValue(participant.TraineeFirefighterId, out var trainee)) continue;
            sessionsById.TryGetValue(participant.TrainingSessionId, out var session);

            rows.Add(new AnonymizedVitalsRowDto(
                trainee.ApplicantCode ?? string.Empty,
                participant.TrainingSessionId,
                session?.Title ?? string.Empty,
                session?.ScheduledStart ?? v.TakenAt,
                v.HeartRate, v.SystolicPressure, v.DiastolicPressure, v.TemperatureC, v.Spo2,
                v.TakenAt));
        }

        return rows.OrderBy(r => r.TakenAt).ToList();
    }

    private static decimal? Avg(IEnumerable<decimal?> values)
    {
        var present = values.Where(v => v.HasValue).Select(v => v!.Value).ToList();
        return present.Count == 0 ? null : Math.Round(present.Average(), 2);
    }
}
