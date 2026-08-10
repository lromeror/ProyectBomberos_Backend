using BomberosAPI.Application.Common.Exceptions;
using BomberosAPI.Application.Features.Audit;
using BomberosAPI.Domain.Entities;
using BomberosAPI.Domain.Repositories;
using FluentValidation;
using AppValidationException = BomberosAPI.Application.Common.Exceptions.ValidationException;

namespace BomberosAPI.Application.Features.TraineeFirefighters;

public class TraineeFirefighterService
{
    // Domain.Enums.TrainingStatus define estos valores, pero TraineeFirefighter.TrainingStatus
    // es un string plano — sin esta lista, el endpoint aceptaba cualquier texto como
    // estado válido.
    private static readonly HashSet<string> ValidTrainingStatuses = new(StringComparer.OrdinalIgnoreCase)
    {
        "Active", "Suspended", "Graduated", "Withdrawn",
    };

    private readonly ITraineeFirefighterRepository _repo;
    private readonly IValidator<CreateTraineeFirefighterRequest> _createValidator;
    private readonly ChangeAuditService _changeAudit;

    public TraineeFirefighterService(
        ITraineeFirefighterRepository repo,
        IValidator<CreateTraineeFirefighterRequest> createValidator,
        ChangeAuditService changeAudit)
    {
        _repo = repo;
        _createValidator = createValidator;
        _changeAudit = changeAudit;
    }

    public async Task<IReadOnlyList<TraineeFirefighterDto>> GetAllAsync(CancellationToken ct = default)
    {
        var trainees = await _repo.GetAllAsync(ct);
        return trainees.Select(ToDto).ToList();
    }

    public async Task<TraineeFirefighterDto> GetByIdAsync(Guid id, CancellationToken ct = default)
    {
        var trainee = await _repo.GetByIdAsync(id, ct)
            ?? throw new NotFoundException("TraineeFirefighter", id);
        return ToDto(trainee);
    }

    public async Task<TraineeFirefighterDto> CreateAsync(CreateTraineeFirefighterRequest request, CancellationToken ct = default)
    {
        // No tiene sentido pedirle a quien da de alta a un aspirante (o al propio
        // aspirante, completando su registro por invitación) que se invente un
        // identificador interno — se genera solo si no llega uno.
        if (string.IsNullOrWhiteSpace(request.ApplicantCode))
            request = request with { ApplicantCode = await GenerateUniqueApplicantCodeAsync(ct) };

        var validation = await _createValidator.ValidateAsync(request, ct);
        if (await _repo.ExistsByApplicantCodeAsync(request.ApplicantCode, ct))
            throw new ConflictException("Applicant code already in use.");
        if (!validation.IsValid)
        {
            var errors = validation.Errors
                .GroupBy(e => e.PropertyName)
                .ToDictionary(g => g.Key, g => g.Select(e => e.ErrorMessage).ToArray());
            throw new AppValidationException(errors);
        }

        var trainee = new TraineeFirefighter
        {
            TraineeFirefighterId = Guid.NewGuid(),
            UserId = request.UserId,
            ApplicantCode = request.ApplicantCode,
            BirthDate = request.BirthDate,
            Sex = request.Sex,
            BloodType = request.BloodType,
            EmergencyContactName = request.EmergencyContactName,
            EmergencyContactPhone = request.EmergencyContactPhone,
            TrainingStatus = "Active"
        };

        await _repo.AddAsync(trainee, ct);
        return ToDto(trainee);
    }

    public async Task<TraineeFirefighterDto> UpdateAsync(Guid id, UpdateTraineeFirefighterRequest request, CancellationToken ct = default)
    {
        var trainee = await _repo.GetByIdAsync(id, ct)
            ?? throw new NotFoundException("TraineeFirefighter", id);

        trainee.BloodType = request.BloodType;
        trainee.EmergencyContactName = request.EmergencyContactName;
        trainee.EmergencyContactPhone = request.EmergencyContactPhone;

        await _repo.UpdateAsync(trainee, ct);
        return ToDto(trainee);
    }

    public async Task SetTrainingStatusAsync(Guid id, string status, Guid actingUserId, CancellationToken ct = default)
    {
        if (!ValidTrainingStatuses.Contains(status))
            throw new BusinessRuleException($"'{status}' is not a valid training status.");

        var trainee = await _repo.GetByIdAsync(id, ct)
            ?? throw new NotFoundException("TraineeFirefighter", id);

        var previousStatus = trainee.TrainingStatus;
        trainee.TrainingStatus = status;
        await _repo.UpdateAsync(trainee, ct);

        await _changeAudit.LogAsync(actingUserId, "TraineeFirefighter", id, "TrainingStatusChange",
            new Dictionary<string, object?> { ["trainingStatus"] = previousStatus },
            new Dictionary<string, object?> { ["trainingStatus"] = trainee.TrainingStatus },
            ct);
    }

    private async Task<string> GenerateUniqueApplicantCodeAsync(CancellationToken ct)
    {
        for (var attempt = 0; attempt < 10; attempt++)
        {
            var candidate = $"BOM-{Random.Shared.Next(100_000, 999_999)}";
            if (!await _repo.ExistsByApplicantCodeAsync(candidate, ct))
                return candidate;
        }
        throw new BusinessRuleException("No se pudo generar un código de aspirante único. Intenta de nuevo.");
    }

    private static TraineeFirefighterDto ToDto(TraineeFirefighter t) => new(
        t.TraineeFirefighterId, t.UserId,
        t.User?.FirstName ?? "", t.User?.LastName ?? "", t.User?.Email ?? "", t.User?.Phone,
        t.ApplicantCode, t.BirthDate, t.Sex, t.BloodType,
        t.EmergencyContactName, t.EmergencyContactPhone,
        t.TrainingStatus);
}