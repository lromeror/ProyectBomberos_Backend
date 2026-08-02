using System.Globalization;
using System.Net.Http.Json;
using BomberosAPI.AcceptanceTests.Support;
using BomberosAPI.Application.Features.VitalSigns;
using BomberosAPI.Domain.Entities;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Reqnroll;

namespace BomberosAPI.AcceptanceTests.StepDefinitions;

[Binding]
public class VitalSignsSteps
{
    private readonly TestContext _context;

    public VitalSignsSteps(TestContext context)
    {
        _context = context;
    }

    [Given(@"que existe una sesión de entrenamiento activa con el aspirante ""(.*)"" como participante registrado")]
    public async Task GivenQueExisteUnaSesionDeEntrenamientoActivaConElAspirante(string traineeEmail)
    {
        using var db = _context.GetDbContext();

        var traineeUser = await db.Users.FirstOrDefaultAsync(u => u.Email == traineeEmail);
        traineeUser.Should().NotBeNull($"El usuario aspirante {traineeEmail} debe existir.");

        var trainee = await db.TraineeFirefighters.FirstOrDefaultAsync(t => t.UserId == traineeUser!.UserId);
        trainee.Should().NotBeNull($"El registro de aspirante para {traineeEmail} debe existir.");
        _context.CurrentTraineeId = trainee!.TraineeFirefighterId;

        // Obtain or create training session
        var session = await db.TrainingSessions.FirstOrDefaultAsync(s => s.Status == "InProgress")
            ?? await db.TrainingSessions.FirstOrDefaultAsync(s => s.Status == "Scheduled")
            ?? await db.TrainingSessions.FirstAsync();

        _context.LastCreatedSessionId = session.TrainingSessionId;

        // Ensure session participant exists
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

        // Get medical personnel id
        var hp = await db.HealthPersonnel.FirstOrDefaultAsync();
        _context.CurrentHealthPersonnelId = hp?.HealthPersonnelId;
    }

    [When(@"el médico registra una medición de signos vitales para el aspirante con los siguientes valores:")]
    public async Task WhenElMedicoRegistraUnaMedicionDeSignosVitales(Table table)
    {
        var row = table.Rows[0];
        _context.LastCreatedParticipantId.Should().NotBeNull("Debe existir un participante de sesión.");

        using var db = _context.GetDbContext();
        var hp = await db.HealthPersonnel.FirstOrDefaultAsync()
            ?? throw new InvalidOperationException("No se encontró personal médico en la base de datos.");

        var request = new CreateVitalSignsMeasurementRequest(
            _context.LastCreatedParticipantId!.Value,
            hp.HealthPersonnelId,
            decimal.Parse(row["FrecuenciaCardiaca"], CultureInfo.InvariantCulture),
            decimal.Parse(row["PresionSistolica"], CultureInfo.InvariantCulture),
            decimal.Parse(row["PresionDiastolica"], CultureInfo.InvariantCulture),
            decimal.Parse(row["Temperatura"], CultureInfo.InvariantCulture),
            decimal.Parse(row["SaturacionOxigeno"], CultureInfo.InvariantCulture),
            row["RolPractica"],
            bool.Parse(row["EsFumador"]),
            bool.Parse(row["ExpuestoHumo48h"])
        );

        var response = await _context.Client.PostAsJsonAsync("/api/vital-signs", request);
        _context.LastResponse = response;
        _context.LastResponseBody = await response.Content.ReadAsStringAsync();

        if (response.IsSuccessStatusCode)
        {
            var apiResponse = await _context.ReadResponseAs<VitalSignsMeasurementDto>();
            _context.LastCreatedVitalSignsId = apiResponse?.Data?.VitalSignsMeasurementId;
        }
    }

    [Then(@"la medición de signos vitales queda registrada y vinculada al participante de la sesión")]
    public async Task ThenLaMedicionDeSignosVitalesQuedaRegistradaYVinculada()
    {
        var apiResponse = await _context.ReadResponseAs<VitalSignsMeasurementDto>();
        apiResponse.Should().NotBeNull();
        apiResponse!.Data.Should().NotBeNull();
        apiResponse.Data!.SessionParticipantId.Should().Be(_context.LastCreatedParticipantId!.Value);

        using var db = _context.GetDbContext();
        var measurementInDb = await db.VitalSignsMeasurements
            .FirstOrDefaultAsync(v => v.VitalSignsMeasurementId == apiResponse.Data.VitalSignsMeasurementId);

        measurementInDb.Should().NotBeNull();
        measurementInDb!.SessionParticipantId.Should().Be(_context.LastCreatedParticipantId.Value);
    }

    [When(@"el médico intenta registrar signos vitales con valores fisiológicamente inválidos:")]
    public async Task WhenElMedicoIntentaRegistrarSignosVitalesConValoresInvalidos(Table table)
    {
        var row = table.Rows[0];
        _context.LastCreatedParticipantId.Should().NotBeNull();

        using var db = _context.GetDbContext();
        var hp = await db.HealthPersonnel.FirstOrDefaultAsync()
            ?? throw new InvalidOperationException("No se encontró personal médico en la base de datos.");

        var request = new CreateVitalSignsMeasurementRequest(
            _context.LastCreatedParticipantId!.Value,
            hp.HealthPersonnelId,
            decimal.Parse(row["FrecuenciaCardiaca"], CultureInfo.InvariantCulture),
            decimal.Parse(row["PresionSistolica"], CultureInfo.InvariantCulture),
            decimal.Parse(row["PresionDiastolica"], CultureInfo.InvariantCulture),
            decimal.Parse(row["Temperatura"], CultureInfo.InvariantCulture),
            decimal.Parse(row["SaturacionOxigeno"], CultureInfo.InvariantCulture),
            "Pitonero",
            false,
            false
        );

        var response = await _context.Client.PostAsJsonAsync("/api/vital-signs", request);
        _context.LastResponse = response;
        _context.LastResponseBody = await response.Content.ReadAsStringAsync();
    }

    [Then(@"la respuesta debe indicar errores de validación en los rangos fisiológicos")]
    public void ThenLaRespuestaDebeIndicarErroresDeValidacionEnLosRangosFisiologicos()
    {
        _context.LastResponseBody.Should().NotBeNullOrWhiteSpace();
        _context.LastResponseBody!.ToLowerInvariant().Should().Match(s =>
            s.Contains("heartrate") || s.Contains("systolicpressure") || s.Contains("temperature") || s.Contains("spo2") || s.Contains("errors") || s.Contains("validation"));
    }
}
