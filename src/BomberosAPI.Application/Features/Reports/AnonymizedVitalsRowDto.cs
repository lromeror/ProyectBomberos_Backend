namespace BomberosAPI.Application.Features.Reports;

/// <summary>
/// One row of the anonymized export: identifies the trainee only by their
/// pre-existing pseudonymous `ApplicantCode`, never by name/email/phone.
/// </summary>
public record AnonymizedVitalsRowDto(
    string ApplicantCode,
    Guid TrainingSessionId,
    string SessionTitle,
    DateTime SessionDate,
    decimal? HeartRate,
    decimal? SystolicPressure,
    decimal? DiastolicPressure,
    decimal? TemperatureC,
    decimal? Spo2,
    DateTime TakenAt
);
