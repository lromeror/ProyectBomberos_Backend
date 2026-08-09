using System.Linq;
using BomberosAPI.Domain.Enums;
using FluentAssertions;

namespace BomberosAPI.Domain.UnitTests;

// Estos enums se serializan como el nombre del miembro (string) y el frontend los
// compara contra literales propios en JS, sin ningún chequeo de tipos compartido entre
// los dos repos. Un ejemplo real: src/services/participantService.js comparaba contra
// 'Absent', un valor que ParticipationStatus nunca tuvo — la rama nunca se alcanzaba
// (ver AUDIT_REPORT.md). Estas pruebas fijan el vocabulario exacto de cada enum para
// que un cambio de nombre aquí (que rompería silenciosamente al frontend) falle acá
// primero, en vez de en producción.
public class EnumContractTests
{
    [Fact]
    public void ParticipationStatus_HasExpectedMembers()
    {
        System.Enum.GetNames<ParticipationStatus>().Should().BeEquivalentTo(
            "Invited", "Confirmed", "CheckedIn", "Completed", "NoShow", "Withdrawn");
    }

    [Fact]
    public void ParticipationStatus_DoesNotContainAbsent()
    {
        // Regression guard específico: 'Absent' es el valor que el frontend comparaba
        // por error antes del fix en participantService.js.
        System.Enum.GetNames<ParticipationStatus>().Should().NotContain("Absent");
    }

    [Fact]
    public void SessionStatus_HasExpectedMembers()
    {
        // Debe coincidir con STATUS_MAP en src/services/sessionService.js.
        System.Enum.GetNames<SessionStatus>().Should().BeEquivalentTo(
            "Scheduled", "InProgress", "Finished", "Cancelled");
    }

    [Fact]
    public void SymptomSeverity_HasExpectedMembers()
    {
        // Debe coincidir con SEVERITY_TO_API en src/services/symptomReportService.js.
        System.Enum.GetNames<SymptomSeverity>().Should().BeEquivalentTo("Mild", "Moderate", "Severe");
    }

    [Fact]
    public void TrainingStatus_HasExpectedMembers()
    {
        System.Enum.GetNames<TrainingStatus>().Should().BeEquivalentTo(
            "Active", "Suspended", "Graduated", "Withdrawn");
    }

    [Fact]
    public void AlertStatus_MatchesWhatCriticalAlertServiceActuallyWrites()
    {
        // CriticalAlertService.cs solo escribe "Open" (al crear) y "Attended" (al
        // atender) — el enum antes declaraba un flujo de 5 estados que no
        // correspondía a ningún código real (ver AUDIT_REPORT.md).
        System.Enum.GetNames<AlertStatus>().Should().BeEquivalentTo("Open", "Attended");
    }
}
