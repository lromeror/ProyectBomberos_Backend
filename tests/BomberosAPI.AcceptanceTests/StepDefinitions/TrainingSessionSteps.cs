using System.Net.Http.Json;
using BomberosAPI.AcceptanceTests.Support;
using BomberosAPI.Application.Common.Constants;
using BomberosAPI.Application.Features.Invitations;
using BomberosAPI.Application.Features.TrainingSessions;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Reqnroll;

namespace BomberosAPI.AcceptanceTests.StepDefinitions;

[Binding]
public class TrainingSessionSteps
{
    private readonly TestContext _context;

    public TrainingSessionSteps(TestContext context)
    {
        _context = context;
    }

    [When(@"el Jefe de Bomberos crea una sesión de entrenamiento con los siguientes datos:")]
    public async Task WhenElJefeDeBomberosCreaUnaSesionDeEntrenamiento(Table table)
    {
        var row = table.Rows[0];
        var title = row["Titulo"];
        var locationName = row["Ubicacion"];
        var capacity = int.Parse(row["CapacidadPlanificada"], System.Globalization.CultureInfo.InvariantCulture);
        var daysInFuture = int.Parse(row["DiasEnElFuturo"], System.Globalization.CultureInfo.InvariantCulture);

        using var db = _context.GetDbContext();
        var institution = await db.TrainingInstitutions.FirstAsync();
        var location = await db.TrainingLocations.FirstOrDefaultAsync(l => l.Name.Contains(locationName) || locationName.Contains(l.Name))
            ?? await db.TrainingLocations.FirstAsync();

        var startTime = DateTime.UtcNow.AddDays(daysInFuture);
        var endTime = startTime.AddHours(4);

        var request = new CreateTrainingSessionRequest(
            institution.InstitutionId,
            location.TrainingLocationId,
            "SES-" + Guid.NewGuid().ToString("N")[..4].ToUpperInvariant(),
            title,
            "Entrenamiento práctico en casa de fuego",
            startTime,
            endTime,
            capacity
        );

        var response = await _context.Client.PostAsJsonAsync("/api/training-sessions", request);

        _context.LastResponse = response;
        _context.LastResponseBody = await response.Content.ReadAsStringAsync();

        if (response.IsSuccessStatusCode)
        {
            var apiResponse = await _context.ReadResponseAs<TrainingSessionDto>();
            _context.LastCreatedSessionId = apiResponse?.Data?.TrainingSessionId;
        }
    }

    [Then(@"la sesión de entrenamiento creada debe tener estado ""(.*)""")]
    public async Task ThenLaSesionDeEntrenamientoCreadaDebeTenerEstado(string expectedStatus)
    {
        var apiResponse = await _context.ReadResponseAs<TrainingSessionDto>();
        apiResponse.Should().NotBeNull();
        apiResponse!.Data.Should().NotBeNull();
        apiResponse.Data!.Status.Should().Be(expectedStatus);
    }

    [When(@"el Jefe de Bomberos asigna al personal médico ""(.*)"" a la sesión")]
    public async Task WhenElJefeDeBomberosAsignaAlPersonalMedicoALaSesion(string medicalEmail)
    {
        _context.LastCreatedSessionId.Should().NotBeNull("Debe haber una sesión creada previamente.");

        using var db = _context.GetDbContext();
        var user = await db.Users.FirstOrDefaultAsync(u => u.Email == medicalEmail);
        user.Should().NotBeNull($"El usuario médico {medicalEmail} debe existir.");

        var request = new CreateInvitationRequest(
            user!.UserId,
            _context.LastCreatedSessionId,
            Roles.Medical,
            medicalEmail,
            DateTime.UtcNow.AddDays(7)
        );

        var response = await _context.Client.PostAsJsonAsync("/api/invitations", request);
        _context.LastResponse = response;
        _context.LastResponseBody = await response.Content.ReadAsStringAsync();

        response.StatusCode.Should().Be(System.Net.HttpStatusCode.Created);
    }

    [When(@"el Jefe de Bomberos envía una invitación por correo al bombero aspirante ""(.*)"" para la sesión")]
    public async Task WhenElJefeDeBomberosEnviaUnaInvitacionPorCorreoAlBomberoAspirante(string traineeEmail)
    {
        _context.LastCreatedSessionId.Should().NotBeNull("Debe haber una sesión creada previamente.");

        using var db = _context.GetDbContext();
        var user = await db.Users.FirstOrDefaultAsync(u => u.Email == traineeEmail);

        var request = new CreateInvitationRequest(
            user?.UserId,
            _context.LastCreatedSessionId,
            Roles.FirefighterTrainee,
            traineeEmail,
            DateTime.UtcNow.AddDays(7)
        );

        var response = await _context.Client.PostAsJsonAsync("/api/invitations", request);
        _context.LastResponse = response;
        _context.LastResponseBody = await response.Content.ReadAsStringAsync();

        response.StatusCode.Should().Be(System.Net.HttpStatusCode.Created);
    }

    [Then(@"las invitaciones para la sesión son creadas exitosamente con estado ""(.*)""")]
    public async Task ThenLasInvitacionesParaLaSesionSonCreadasExitosamenteConEstado(string expectedStatus)
    {
        _context.LastCreatedSessionId.Should().NotBeNull();

        using var db = _context.GetDbContext();
        var invitations = await db.Invitations
            .Where(i => i.TrainingSessionId == _context.LastCreatedSessionId)
            .ToListAsync();

        invitations.Should().NotBeEmpty("Deben existir invitaciones creadas para la sesión.");
        invitations.Should().OnlyContain(i => i.Status == expectedStatus);
    }
}
