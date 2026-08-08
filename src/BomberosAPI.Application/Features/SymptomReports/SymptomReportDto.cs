namespace BomberosAPI.Application.Features.SymptomReports;

public record SymptomReportDto(
    Guid SymptomReportId,
    Guid SessionParticipantId,
    Guid ReportedByUserId,
    string? Severity,
    string? Symptoms,
    bool RequiresAlert,
    DateTime ReportedAt
);
