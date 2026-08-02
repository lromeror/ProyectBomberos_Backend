using System.Net.Http.Json;
using BomberosAPI.AcceptanceTests.Support;
using BomberosAPI.Application.Common.Constants;
using BomberosAPI.Application.Features.VitalSigns;
using BomberosAPI.Domain.Entities;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Reqnroll;

namespace BomberosAPI.AcceptanceTests.StepDefinitions;

[Binding]
public class ResultsAndHistorySteps
{
    private readonly TestContext _context;

    public ResultsAndHistorySteps(TestContext context)
    {
        _context = context;
    }

    [Given(@"que existen mediciones de signos vitales registradas para el aspirante ""(.*)""")]
    public async Task GivenQueExistenMedicionesParaElAspirante(string email)
    {
        using var db = _context.GetDbContext();

        var user = await db.Users.FirstOrDefaultAsync(u => u.Email == email);
        user.Should().NotBeNull();

        var trainee = await db.TraineeFirefighters.FirstOrDefaultAsync(t => t.UserId == user!.UserId);
        trainee.Should().NotBeNull();
        _context.CurrentTraineeId = trainee!.TraineeFirefighterId;

        var session = await db.TrainingSessions.FirstAsync();
        var hp = await db.HealthPersonnel.FirstAsync();

        var participant = await db.SessionParticipants
            .FirstOrDefaultAsync(p => p.TrainingSessionId == session.TrainingSessionId && p.TraineeFirefighterId == trainee.TraineeFirefighterId);

        if (participant == null)
        {
            participant = new SessionParticipant
            {
                SessionParticipantId = Guid.NewGuid(),
                TrainingSessionId = session.TrainingSessionId,
                TraineeFirefighterId = trainee.TraineeFirefighterId,
                CheckInAt = DateTime.UtcNow,
                AttendanceConfirmed = true,
                ParticipationStatus = "Attended"
            };
            db.SessionParticipants.Add(participant);
            await db.SaveChangesAsync();
        }

        _context.LastCreatedParticipantId = participant.SessionParticipantId;

        // Ensure at least one vital signs measurement
        var hasVitals = await db.VitalSignsMeasurements.AnyAsync(v => v.SessionParticipantId == participant.SessionParticipantId);
        if (!hasVitals)
        {
            db.VitalSignsMeasurements.Add(new VitalSignsMeasurement
            {
                VitalSignsMeasurementId = Guid.NewGuid(),
                SessionParticipantId = participant.SessionParticipantId,
                RegisteredByHealthPersonnelId = hp.HealthPersonnelId,
                HeartRate = 72,
                SystolicPressure = 118,
                DiastolicPressure = 78,
                TemperatureC = 36.5m,
                Spo2 = 99,
                PracticeRole = "Aspirante Principal",
                TakenAt = DateTime.UtcNow
            });
            await db.SaveChangesAsync();
        }
    }

    [Given(@"existen mediciones de signos vitales registradas para otro aspirante ""(.*)""")]
    public async Task GivenExistenMedicionesParaOtroAspirante(string email)
    {
        using var db = _context.GetDbContext();

        var user = await db.Users.FirstOrDefaultAsync(u => u.Email == email);
        user.Should().NotBeNull();

        var trainee = await db.TraineeFirefighters.FirstOrDefaultAsync(t => t.UserId == user!.UserId);
        trainee.Should().NotBeNull();
        _context.OtherTraineeId = trainee!.TraineeFirefighterId;

        var session = await db.TrainingSessions.FirstAsync();
        var hp = await db.HealthPersonnel.FirstAsync();

        var participant = await db.SessionParticipants
            .FirstOrDefaultAsync(p => p.TrainingSessionId == session.TrainingSessionId && p.TraineeFirefighterId == trainee.TraineeFirefighterId);

        if (participant == null)
        {
            participant = new SessionParticipant
            {
                SessionParticipantId = Guid.NewGuid(),
                TrainingSessionId = session.TrainingSessionId,
                TraineeFirefighterId = trainee.TraineeFirefighterId,
                CheckInAt = DateTime.UtcNow,
                AttendanceConfirmed = true,
                ParticipationStatus = "Attended"
            };
            db.SessionParticipants.Add(participant);
            await db.SaveChangesAsync();
        }

        _context.OtherParticipantId = participant.SessionParticipantId;

        var hasVitals = await db.VitalSignsMeasurements.AnyAsync(v => v.SessionParticipantId == participant.SessionParticipantId);
        if (!hasVitals)
        {
            db.VitalSignsMeasurements.Add(new VitalSignsMeasurement
            {
                VitalSignsMeasurementId = Guid.NewGuid(),
                SessionParticipantId = participant.SessionParticipantId,
                RegisteredByHealthPersonnelId = hp.HealthPersonnelId,
                HeartRate = 85,
                SystolicPressure = 125,
                DiastolicPressure = 82,
                TemperatureC = 36.8m,
                Spo2 = 97,
                PracticeRole = "Aspirante Secundario",
                TakenAt = DateTime.UtcNow
            });
            await db.SaveChangesAsync();
        }
    }

    [When(@"el aspirante consulta su propio historial de signos vitales")]
    public async Task WhenElAspiranteConsultaSuPropioHistorialDeSignosVitales()
    {
        _context.CurrentTraineeId.Should().NotBeNull();

        var response = await _context.Client.GetAsync($"/api/vital-signs/by-trainee/{_context.CurrentTraineeId}");
        _context.LastResponse = response;
        _context.LastResponseBody = await response.Content.ReadAsStringAsync();
    }

    [Then(@"todas las lecturas de signos vitales devueltas deben corresponder únicamente al aspirante ""(.*)""")]
    public async Task ThenTodasLasLecturasDebenCorresponderAlAspirante(string email)
    {
        var apiResponse = await _context.ReadResponseAs<IReadOnlyList<VitalSignsHistoryDto>>();
        apiResponse.Should().NotBeNull();
        apiResponse!.Data.Should().NotBeNull();
        apiResponse.Data.Should().NotBeEmpty();

        using var db = _context.GetDbContext();
        var ownParticipantIds = await db.SessionParticipants
            .Where(p => p.TraineeFirefighterId == _context.CurrentTraineeId)
            .Select(p => p.SessionParticipantId)
            .ToListAsync();

        apiResponse.Data.Should().OnlyContain(h => ownParticipantIds.Contains(h.SessionParticipantId));
    }

    [Then(@"ninguna lectura debe pertenecer al aspirante ""(.*)""")]
    public async Task ThenNingunaLecturaDebePertenecerAlAspirante(string otherEmail)
    {
        var apiResponse = await _context.ReadResponseAs<IReadOnlyList<VitalSignsHistoryDto>>();
        apiResponse.Should().NotBeNull();
        apiResponse!.Data.Should().NotBeNull();

        if (_context.OtherParticipantId.HasValue)
        {
            apiResponse.Data.Should().NotContain(h => h.SessionParticipantId == _context.OtherParticipantId.Value);
        }
    }

    [When(@"el aspirante intenta consultar el listado global no filtrado de signos vitales")]
    public async Task WhenElAspiranteIntentaConsultarElListadoGlobal()
    {
        var response = await _context.Client.GetAsync("/api/vital-signs");
        _context.LastResponse = response;
        _context.LastResponseBody = await response.Content.ReadAsStringAsync();
    }
}
