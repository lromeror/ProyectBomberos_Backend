namespace BomberosAPI.Application.Features.VitalSigns;

public record VitalSignsHistoryDto(
    Guid VitalSignsMeasurementId,
    Guid SessionParticipantId,
    Guid TrainingSessionId,
    string SessionTitle,
    DateTime SessionDate,
    decimal? HeartRate,
    decimal? SystolicPressure,
    decimal? DiastolicPressure,
    decimal? TemperatureC,
    decimal? Spo2,
    string? PracticeRole,
    bool? IsSmoker,
    bool? ExposedToSmoke48h,
    DateTime TakenAt
);
