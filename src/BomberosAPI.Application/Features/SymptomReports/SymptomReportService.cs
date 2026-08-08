using BomberosAPI.Application.Common.Exceptions;
using BomberosAPI.Domain.Repositories;
using FluentValidation;
using SymptomReportEntity = BomberosAPI.Domain.Entities.SymptomReport;
using AppValidationException = BomberosAPI.Application.Common.Exceptions.ValidationException;

namespace BomberosAPI.Application.Features.SymptomReports;

public class SymptomReportService
{
    private readonly ISymptomReportRepository _repo;
    private readonly ISessionParticipantRepository _participantRepo;
    private readonly IUserRepository _userRepo;
    private readonly IValidator<CreateSymptomReportRequest> _createValidator;

    public SymptomReportService(
        ISymptomReportRepository repo,
        ISessionParticipantRepository participantRepo,
        IUserRepository userRepo,
        IValidator<CreateSymptomReportRequest> createValidator)
    {
        _repo = repo;
        _participantRepo = participantRepo;
        _userRepo = userRepo;
        _createValidator = createValidator;
    }

    public async Task<IReadOnlyList<SymptomReportDto>> GetAllAsync(CancellationToken ct = default)
    {
        var items = await _repo.GetAllAsync(ct);
        return items.Select(ToDto).ToList();
    }

    public async Task<SymptomReportDto> GetByIdAsync(Guid id, CancellationToken ct = default)
    {
        var item = await _repo.GetByIdAsync(id, ct)
            ?? throw new NotFoundException("SymptomReport", id);
        return ToDto(item);
    }

    public async Task<IReadOnlyList<SymptomReportDto>> GetByParticipantAsync(Guid participantId, CancellationToken ct = default)
    {
        var items = await _repo.GetByParticipantAsync(participantId, ct);
        return items.Select(ToDto).ToList();
    }

    public async Task<IReadOnlyList<SymptomReportDto>> GetByTraineeAsync(Guid traineeFirefighterId, CancellationToken ct = default)
    {
        var items = await _repo.GetByTraineeAsync(traineeFirefighterId, ct);
        return items.Select(ToDto).ToList();
    }

    public async Task<SymptomReportDto> CreateAsync(CreateSymptomReportRequest request, CancellationToken ct = default)
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

        if (await _userRepo.GetByIdAsync(request.ReportedByUserId, ct) is null)
            throw new NotFoundException("User", request.ReportedByUserId);

        var report = new SymptomReportEntity
        {
            SymptomReportId = Guid.NewGuid(),
            SessionParticipantId = request.SessionParticipantId,
            ReportedByUserId = request.ReportedByUserId,
            Severity = request.Severity,
            Symptoms = request.Symptoms,
            RequiresAlert = request.RequiresAlert,
            ReportedAt = DateTime.UtcNow
        };

        await _repo.AddAsync(report, ct);
        return ToDto(report);
    }

    private static SymptomReportDto ToDto(SymptomReportEntity s) => new(
        s.SymptomReportId, s.SessionParticipantId, s.ReportedByUserId,
        s.Severity, s.Symptoms, s.RequiresAlert, s.ReportedAt);
}
