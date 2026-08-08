namespace BomberosAPI.Application.Features.SessionResults;

public record SessionResultDto(
    Guid SessionResultId,
    Guid SessionParticipantId,
    Guid ValidatedByUserId,
    decimal? PerformanceScore,
    string? RiskClassification,
    bool FitToContinue,
    string? Summary,
    DateTime GeneratedAt
);

public record CreateSessionResultRequest(
    Guid SessionParticipantId,
    decimal? PerformanceScore,
    string? RiskClassification,
    bool FitToContinue,
    string? Summary
);
