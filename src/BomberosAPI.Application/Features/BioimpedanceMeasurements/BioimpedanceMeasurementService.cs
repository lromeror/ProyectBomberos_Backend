using BomberosAPI.Application.Common.Exceptions;
using BomberosAPI.Domain.Repositories;
using FluentValidation;
using BioimpedanceMeasurementEntity = BomberosAPI.Domain.Entities.BioimpedanceMeasurement;
using AppValidationException = BomberosAPI.Application.Common.Exceptions.ValidationException;

namespace BomberosAPI.Application.Features.BioimpedanceMeasurements;

public class BioimpedanceMeasurementService
{
    private readonly IBioimpedanceMeasurementRepository _repo;
    private readonly ISessionParticipantRepository _participantRepo;
    private readonly IHealthPersonnelRepository _hpRepo;
    private readonly IValidator<CreateBioimpedanceMeasurementRequest> _createValidator;

    public BioimpedanceMeasurementService(
        IBioimpedanceMeasurementRepository repo,
        ISessionParticipantRepository participantRepo,
        IHealthPersonnelRepository hpRepo,
        IValidator<CreateBioimpedanceMeasurementRequest> createValidator)
    {
        _repo = repo;
        _participantRepo = participantRepo;
        _hpRepo = hpRepo;
        _createValidator = createValidator;
    }

    public async Task<IReadOnlyList<BioimpedanceMeasurementDto>> GetAllAsync(CancellationToken ct = default)
    {
        var items = await _repo.GetAllAsync(ct);
        return items.Select(ToDto).ToList();
    }

    public async Task<BioimpedanceMeasurementDto> GetByIdAsync(Guid id, CancellationToken ct = default)
    {
        var item = await _repo.GetByIdAsync(id, ct)
            ?? throw new NotFoundException("BioimpedanceMeasurement", id);
        return ToDto(item);
    }

    public async Task<IReadOnlyList<BioimpedanceMeasurementDto>> GetByParticipantAsync(Guid participantId, CancellationToken ct = default)
    {
        var items = await _repo.GetByParticipantAsync(participantId, ct);
        return items.Select(ToDto).ToList();
    }

    public async Task<IReadOnlyList<BioimpedanceMeasurementDto>> GetByTraineeAsync(Guid traineeFirefighterId, CancellationToken ct = default)
    {
        var items = await _repo.GetByTraineeAsync(traineeFirefighterId, ct);
        return items.Select(ToDto).ToList();
    }

    public async Task<BioimpedanceMeasurementDto> CreateAsync(CreateBioimpedanceMeasurementRequest request, CancellationToken ct = default)
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

        if (await _hpRepo.GetByIdAsync(request.RegisteredByHealthPersonnelId, ct) is null)
            throw new NotFoundException("HealthPersonnel", request.RegisteredByHealthPersonnelId);

        var measurement = new BioimpedanceMeasurementEntity
        {
            BioimpedanceMeasurementId = Guid.NewGuid(),
            SessionParticipantId = request.SessionParticipantId,
            RegisteredByHealthPersonnelId = request.RegisteredByHealthPersonnelId,
            WeightKg = request.WeightKg,
            FatPercentage = request.FatPercentage,
            MuscleMassKg = request.MuscleMassKg,
            BodyWaterPct = request.BodyWaterPct,
            BasalMetabolicRate = request.BasalMetabolicRate,
            MetabolicAgeYears = request.MetabolicAgeYears,
            LactatePreMmol = request.LactatePreMmol,
            LactatePostMmol = request.LactatePostMmol,
            StroopTimeSeconds = request.StroopTimeSeconds,
            StroopErrors = request.StroopErrors,
            TakenAt = DateTime.UtcNow
        };

        await _repo.AddAsync(measurement, ct);
        return ToDto(measurement);
    }

    private static BioimpedanceMeasurementDto ToDto(BioimpedanceMeasurementEntity b) => new(
        b.BioimpedanceMeasurementId,
        b.SessionParticipantId,
        b.RegisteredByHealthPersonnelId,
        b.WeightKg,
        b.FatPercentage,
        b.MuscleMassKg,
        b.BodyWaterPct,
        b.BasalMetabolicRate,
        b.MetabolicAgeYears,
        b.LactatePreMmol,
        b.LactatePostMmol,
        b.StroopTimeSeconds,
        b.StroopErrors,
        b.TakenAt);
}
