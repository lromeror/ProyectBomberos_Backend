using BomberosAPI.Application.Common.Exceptions;
using BomberosAPI.Domain.Entities;
using BomberosAPI.Domain.Repositories;
using FluentValidation;
using AppValidationException = BomberosAPI.Application.Common.Exceptions.ValidationException;

namespace BomberosAPI.Application.Features.TrainingLocations;

public class TrainingLocationService
{
    private readonly ITrainingLocationRepository _repo;
    private readonly ITrainingInstitutionRepository _institutionRepo;
    private readonly IValidator<CreateTrainingLocationRequest> _createValidator;

    public TrainingLocationService(
        ITrainingLocationRepository repo,
        ITrainingInstitutionRepository institutionRepo,
        IValidator<CreateTrainingLocationRequest> createValidator)
    {
        _repo = repo;
        _institutionRepo = institutionRepo;
        _createValidator = createValidator;
    }

    public async Task<IReadOnlyList<TrainingLocationDto>> GetAllAsync(CancellationToken ct = default)
    {
        var locations = await _repo.GetAllAsync(ct);
        return locations.Select(ToDto).ToList();
    }

    public async Task<TrainingLocationDto> GetByIdAsync(Guid id, CancellationToken ct = default)
    {
        var loc = await _repo.GetByIdAsync(id, ct)
            ?? throw new NotFoundException("TrainingLocation", id);
        return ToDto(loc);
    }

    public async Task<TrainingLocationDto> CreateAsync(CreateTrainingLocationRequest request, CancellationToken ct = default)
    {
        var validation = await _createValidator.ValidateAsync(request, ct);
        if (!validation.IsValid)
        {
            var errors = validation.Errors
                .GroupBy(e => e.PropertyName)
                .ToDictionary(g => g.Key, g => g.Select(e => e.ErrorMessage).ToArray());
            throw new AppValidationException(errors);
        }

        _ = await _institutionRepo.GetByIdAsync(request.InstitutionId, ct)
            ?? throw new NotFoundException("Institution", request.InstitutionId);

        var location = new TrainingLocation
        {
            TrainingLocationId = Guid.NewGuid(),
            InstitutionId = request.InstitutionId,
            Name = request.Name,
            LocationType = request.LocationType,
            Address = request.Address,
            MaxCapacity = request.MaxCapacity
        };

        await _repo.AddAsync(location, ct);
        return ToDto(location);
    }

    public async Task<TrainingLocationDto> UpdateAsync(Guid id, UpdateTrainingLocationRequest request, CancellationToken ct = default)
    {
        var location = await _repo.GetByIdAsync(id, ct)
            ?? throw new NotFoundException("TrainingLocation", id);

        location.Name = request.Name;
        location.LocationType = request.LocationType;
        location.Address = request.Address;
        location.MaxCapacity = request.MaxCapacity;

        await _repo.UpdateAsync(location, ct);
        return ToDto(location);
    }

    private static TrainingLocationDto ToDto(TrainingLocation l) => new(
        l.TrainingLocationId, l.InstitutionId, l.Name, l.LocationType, l.Address, l.MaxCapacity);
}
