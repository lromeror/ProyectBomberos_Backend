namespace BomberosAPI.Application.Features.TraineeFirefighters;

public record CreateTraineeFirefighterRequest(
    Guid UserId,
    // Opcional: si no llega, TraineeFirefighterService.CreateAsync genera uno único —
    // no tiene sentido pedirle a un admin o al propio aspirante que se invente un
    // código, es un identificador interno del sistema.
    string? ApplicantCode,
    DateOnly BirthDate,
    string Sex,
    string? BloodType,
    string? EmergencyContactName,
    string? EmergencyContactPhone
);