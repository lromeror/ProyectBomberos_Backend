namespace BomberosAPI.Application.Features.Reports;

public record ReportSummaryDto(
    int TotalSessions,
    int TotalParticipants,
    int TotalTrainees,
    int TotalVitalSignsMeasurements,
    decimal? AvgHeartRate,
    decimal? AvgSystolicPressure,
    decimal? AvgDiastolicPressure,
    decimal? AvgTemperatureC,
    decimal? AvgSpo2,
    DateTime? From,
    DateTime? To,
    DateTime GeneratedAt
);
