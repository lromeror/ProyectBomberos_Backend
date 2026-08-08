namespace BomberosAPI.Application.Features.VitalSigns;

public record VitalSignsMeasurementDto(
    Guid VitalSignsMeasurementId,
    Guid SessionParticipantId,
    Guid RegisteredByHealthPersonnelId,
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