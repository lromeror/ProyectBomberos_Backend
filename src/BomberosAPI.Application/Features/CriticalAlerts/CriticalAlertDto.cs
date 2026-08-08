namespace BomberosAPI.Application.Features.CriticalAlerts;

public record CriticalAlertDto(
    Guid CriticalAlertId,
    Guid SessionParticipantId,
    Guid? VitalSignsMeasurementId,
    Guid? SymptomReportId,
    Guid? EnvironmentalDataId,
    Guid? AttendedByUserId,
    string AlertType,
    string Severity,
    string Status,
    string? Description,
    DateTime GeneratedAt,
    DateTime? AttendedAt
);

/// <summary>
/// Creación manual: quien está revisando signos vitales/síntomas/datos ambientales
/// marca algo como alerta crítica. No hay un motor de umbrales automático — definir
/// qué valores médicos disparan una alerta requiere criterio clínico que este sistema
/// no fabrica por su cuenta.
/// </summary>
public record CreateCriticalAlertRequest(
    Guid SessionParticipantId,
    Guid? VitalSignsMeasurementId,
    Guid? SymptomReportId,
    Guid? EnvironmentalDataId,
    string AlertType,
    string Severity,
    string? Description
);
