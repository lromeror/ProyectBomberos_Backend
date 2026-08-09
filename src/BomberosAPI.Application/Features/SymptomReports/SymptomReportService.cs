using BomberosAPI.Application.Common.Exceptions;
using BomberosAPI.Domain.Repositories;
using FluentValidation;
using SymptomReportEntity = BomberosAPI.Domain.Entities.SymptomReport;
using CriticalAlertEntity = BomberosAPI.Domain.Entities.CriticalAlert;
using AppValidationException = BomberosAPI.Application.Common.Exceptions.ValidationException;

namespace BomberosAPI.Application.Features.SymptomReports;

public class SymptomReportService
{
    private readonly ISymptomReportRepository _repo;
    private readonly ISessionParticipantRepository _participantRepo;
    private readonly IUserRepository _userRepo;
    private readonly ICriticalAlertRepository _criticalAlertRepo;
    private readonly IValidator<CreateSymptomReportRequest> _createValidator;

    public SymptomReportService(
        ISymptomReportRepository repo,
        ISessionParticipantRepository participantRepo,
        IUserRepository userRepo,
        ICriticalAlertRepository criticalAlertRepo,
        IValidator<CreateSymptomReportRequest> createValidator)
    {
        _repo = repo;
        _participantRepo = participantRepo;
        _userRepo = userRepo;
        _criticalAlertRepo = criticalAlertRepo;
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

        // El pipeline de alertas críticas existía en el backend (CriticalAlertsController)
        // pero nada lo disparaba nunca: `RequiresAlert` llegaba siempre en `false` desde el
        // frontend y ningún flujo generaba una fila en CriticalAlert. Un síntoma reportado
        // como "requiere alerta" ahora sí queda registrado como alerta abierta, visible vía
        // GET /critical-alerts para el personal médico/capacitador/jefe de bomberos.
        if (report.RequiresAlert)
        {
            var alert = new CriticalAlertEntity
            {
                CriticalAlertId = Guid.NewGuid(),
                SessionParticipantId = report.SessionParticipantId,
                SymptomReportId = report.SymptomReportId,
                AlertType = "SymptomReport",
                // CriticalAlert usa su propio vocabulario de severidad (Low/Medium/High/
                // Critical, ver CriticalAlertService.ValidSeverities) distinto al de
                // SymptomSeverity (Mild/Moderate/Severe) — se traduce en vez de copiar el
                // string tal cual, que hubiera dejado un valor ("Severe") que ningún otro
                // registro de CriticalAlert usa.
                Severity = report.Severity == "Severe" ? "Critical" : "High",
                Status = "Open",
                Description = report.Symptoms,
                GeneratedAt = report.ReportedAt
            };
            await _criticalAlertRepo.AddAsync(alert, ct);
        }

        return ToDto(report);
    }

    private static SymptomReportDto ToDto(SymptomReportEntity s) => new(
        s.SymptomReportId, s.SessionParticipantId, s.ReportedByUserId,
        s.Severity, s.Symptoms, s.RequiresAlert, s.ReportedAt);
}
