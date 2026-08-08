namespace BomberosAPI.Application.Features.TrainingLocations;

public record CreateTrainingLocationRequest(
    Guid InstitutionId,
    string Name,
    string? LocationType,
    string? Address,
    int? MaxCapacity
);
