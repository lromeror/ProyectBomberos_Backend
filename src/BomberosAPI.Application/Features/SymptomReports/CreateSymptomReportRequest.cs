namespace BomberosAPI.Application.Features.SymptomReports;

public record CreateSymptomReportRequest(
    Guid SessionParticipantId,
    Guid ReportedByUserId,
    string? Severity,
    string? Symptoms,
    bool RequiresAlert
);
