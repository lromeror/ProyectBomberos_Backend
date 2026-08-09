
namespace BomberosAPI.Domain.Enums;

/// <summary>
/// Estado de atención de una alerta crítica.
///
/// `CriticalAlert.Status` se persiste como string sin conversión a este enum (ver
/// CriticalAlertConfiguration) — este tipo documenta el vocabulario real que
/// CriticalAlertService.cs escribe/lee (Open al crear, Attended al atender), no un
/// flujo de estados más rico que nunca se implementó. Antes declaraba
/// Open/Acknowledged/InProgress/Resolved/Dismissed, un vocabulario que no coincidía
/// con "Attended", el valor que el código realmente usa.
/// </summary>
public enum AlertStatus
{
    Open,
    Attended
}
