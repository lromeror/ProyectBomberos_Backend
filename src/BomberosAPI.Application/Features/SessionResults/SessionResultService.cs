using BomberosAPI.Application.Common.Exceptions;
using BomberosAPI.Domain.Repositories;
using SessionResultEntity = BomberosAPI.Domain.Entities.SessionResult;

namespace BomberosAPI.Application.Features.SessionResults;

public class SessionResultService
{
    private static readonly HashSet<string> ValidRiskClassifications = new(StringComparer.OrdinalIgnoreCase)
    { "Low", "Medium", "High" };

    private readonly ISessionResultRepository _repo;
    private readonly ISessionParticipantRepository _participantRepo;

    public SessionResultService(ISessionResultRepository repo, ISessionParticipantRepository participantRepo)
    {
        _repo = repo;
        _participantRepo = participantRepo;
    }

    public async Task<SessionResultDto> CreateAsync(CreateSessionResultRequest request, Guid validatedByUserId, CancellationToken ct = default)
    {
        if (request.RiskClassification is not null && !ValidRiskClassifications.Contains(request.RiskClassification))
            throw new BusinessRuleException($"'{request.RiskClassification}' is not a valid risk classification.");
        if (await _participantRepo.GetByIdAsync(request.SessionParticipantId, ct) is null)
            throw new NotFoundException("SessionParticipant", request.SessionParticipantId);

        var result = new SessionResultEntity
        {
            SessionResultId = Guid.NewGuid(),
            SessionParticipantId = request.SessionParticipantId,
            ValidatedByUserId = validatedByUserId,
            PerformanceScore = request.PerformanceScore,
            RiskClassification = request.RiskClassification,
            FitToContinue = request.FitToContinue,
            Summary = request.Summary,
            GeneratedAt = DateTime.UtcNow
        };

        await _repo.AddAsync(result, ct);
        return ToDto(result);
    }

    public async Task<IReadOnlyList<SessionResultDto>> GetByParticipantAsync(Guid sessionParticipantId, CancellationToken ct = default)
    {
        var items = await _repo.GetByParticipantAsync(sessionParticipantId, ct);
        return items.Select(ToDto).ToList();
    }

    private static SessionResultDto ToDto(SessionResultEntity r) => new(
        r.SessionResultId, r.SessionParticipantId, r.ValidatedByUserId, r.PerformanceScore,
        r.RiskClassification, r.FitToContinue, r.Summary, r.GeneratedAt);
}
