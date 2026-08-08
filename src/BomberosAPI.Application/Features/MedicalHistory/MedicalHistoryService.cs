using BomberosAPI.Application.Common.Exceptions;
using BomberosAPI.Application.Features.Audit;
using BomberosAPI.Domain.Repositories;
using FluentValidation;
using MedicalHistoryEntity = BomberosAPI.Domain.Entities.MedicalHistory;
using AppValidationException = BomberosAPI.Application.Common.Exceptions.ValidationException;

namespace BomberosAPI.Application.Features.MedicalHistory;

public class MedicalHistoryService
{
    private readonly IMedicalHistoryRepository _repo;
    private readonly ITraineeFirefighterRepository _traineeRepo;
    private readonly IHealthPersonnelRepository _hpRepo;
    private readonly IValidator<CreateMedicalHistoryRequest> _createValidator;
    private readonly ChangeAuditService _changeAudit;

    public MedicalHistoryService(
        IMedicalHistoryRepository repo,
        ITraineeFirefighterRepository traineeRepo,
        IHealthPersonnelRepository hpRepo,
        IValidator<CreateMedicalHistoryRequest> createValidator,
        ChangeAuditService changeAudit)
    {
        _repo = repo;
        _traineeRepo = traineeRepo;
        _hpRepo = hpRepo;
        _createValidator = createValidator;
        _changeAudit = changeAudit;
    }

    public async Task<IReadOnlyList<MedicalHistoryDto>> GetAllAsync(CancellationToken ct = default)
    {
        var items = await _repo.GetAllAsync(ct);
        return items.Select(ToDto).ToList();
    }

    public async Task<MedicalHistoryDto> GetByIdAsync(Guid id, CancellationToken ct = default)
    {
        var mh = await _repo.GetByIdAsync(id, ct)
            ?? throw new NotFoundException("MedicalHistory", id);
        return ToDto(mh);
    }

    public async Task<MedicalHistoryDto> GetByTraineeAsync(Guid traineeId, CancellationToken ct = default)
    {
        var mh = await _repo.GetByTraineeAsync(traineeId, ct)
            ?? throw new NotFoundException("MedicalHistory for Trainee", traineeId);
        return ToDto(mh);
    }

    public async Task<MedicalHistoryDto> CreateAsync(CreateMedicalHistoryRequest request, Guid actingUserId, CancellationToken ct = default)
    {
        var validation = await _createValidator.ValidateAsync(request, ct);
        if (!validation.IsValid)
        {
            var errors = validation.Errors
                .GroupBy(e => e.PropertyName)
                .ToDictionary(g => g.Key, g => g.Select(e => e.ErrorMessage).ToArray());
            throw new AppValidationException(errors);
        }

        if (await _traineeRepo.GetByIdAsync(request.TraineeFirefighterId, ct) is null)
            throw new NotFoundException("TraineeFirefighter", request.TraineeFirefighterId);

        if (await _hpRepo.GetByIdAsync(request.CreatedByHealthPersonnelId, ct) is null)
            throw new NotFoundException("HealthPersonnel", request.CreatedByHealthPersonnelId);

        if (await _repo.ExistsByTraineeAsync(request.TraineeFirefighterId, ct))
            throw new ConflictException("Medical history already exists for this trainee.");

        var mh = new MedicalHistoryEntity
        {
            MedicalHistoryId = Guid.NewGuid(),
            TraineeFirefighterId = request.TraineeFirefighterId,
            CreatedByHealthPersonnelId = request.CreatedByHealthPersonnelId,
            Allergies = request.Allergies,
            PreexistingConditions = request.PreexistingConditions,
            CurrentMedication = request.CurrentMedication,
            GeneralObservations = request.GeneralObservations,
            UpdatedAt = DateTime.UtcNow
        };

        await _repo.AddAsync(mh, ct);

        await _changeAudit.LogAsync(actingUserId, "MedicalHistory", mh.MedicalHistoryId, "Create", null,
            SnapshotOf(mh), ct);

        return ToDto(mh);
    }

    public async Task<MedicalHistoryDto> UpdateAsync(Guid id, UpdateMedicalHistoryRequest request, Guid actingUserId, CancellationToken ct = default)
    {
        var mh = await _repo.GetByIdAsync(id, ct)
            ?? throw new NotFoundException("MedicalHistory", id);

        var previous = SnapshotOf(mh);

        mh.Allergies = request.Allergies;
        mh.PreexistingConditions = request.PreexistingConditions;
        mh.CurrentMedication = request.CurrentMedication;
        mh.GeneralObservations = request.GeneralObservations;
        mh.UpdatedAt = DateTime.UtcNow;

        await _repo.UpdateAsync(mh, ct);

        await _changeAudit.LogAsync(actingUserId, "MedicalHistory", id, "Update", previous, SnapshotOf(mh), ct);

        return ToDto(mh);
    }

    private static Dictionary<string, object?> SnapshotOf(MedicalHistoryEntity mh) => new()
    {
        ["allergies"] = mh.Allergies,
        ["preexistingConditions"] = mh.PreexistingConditions,
        ["currentMedication"] = mh.CurrentMedication,
        ["generalObservations"] = mh.GeneralObservations,
    };

    private static MedicalHistoryDto ToDto(MedicalHistoryEntity m) => new(
        m.MedicalHistoryId,
        m.TraineeFirefighterId,
        m.CreatedByHealthPersonnelId,
        m.Allergies,
        m.PreexistingConditions,
        m.CurrentMedication,
        m.GeneralObservations,
        m.UpdatedAt);
}