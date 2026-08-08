namespace BomberosAPI.Application.Features.TrainingLocations;

public record UpdateTrainingLocationRequest(
    string Name,
    string? LocationType,
    string? Address,
    int? MaxCapacity
);
