using System.Net.Http.Json;
using BomberosAPI.AcceptanceTests.Support;
using BomberosAPI.Application.Features.Auth;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Reqnroll;

namespace BomberosAPI.AcceptanceTests.StepDefinitions;

[Binding]
public class AuthSteps
{
    private readonly TestContext _context;

    public AuthSteps(TestContext context)
    {
        _context = context;
    }

    [Given(@"(?:que )?existe un usuario registrado con correo ""(.*)"" y contraseña ""(.*)"" con rol ""(.*)""")]
    public async Task GivenQueExisteUnUsuarioRegistrado(string email, string password, string roleCode)
    {
        using var db = _context.GetDbContext();
        var user = await db.Users.FirstOrDefaultAsync(u => u.Email == email);
        user.Should().NotBeNull($"El usuario con correo {email} debe existir en la base de datos.");

        var roles = await (from ur in db.UserRoles
                           join r in db.Roles on ur.RoleId equals r.RoleId
                           where ur.UserId == user!.UserId && ur.IsActive
                           select r.Code).ToListAsync();

        roles.Should().Contain(roleCode, $"El usuario {email} debe tener asignado el rol {roleCode}.");
    }

    [When(@"el usuario envía una solicitud de inicio de sesión con correo ""(.*)"" y contraseña ""(.*)""")]
    public async Task WhenElUsuarioEnviaUnaSolicitudDeInicioDeSesion(string email, string password)
    {
        var loginRequest = new LoginRequest(email, password);
        var response = await _context.Client.PostAsJsonAsync("/api/auth/login", loginRequest);

        _context.LastResponse = response;
        _context.LastResponseBody = await response.Content.ReadAsStringAsync();

        if (response.IsSuccessStatusCode)
        {
            var apiResponse = await _context.ReadResponseAs<LoginResult>();
            if (apiResponse?.Data?.Token is not null)
            {
                _context.Authenticate(apiResponse.Data.Token);
                _context.CurrentUserEmail = apiResponse.Data.Email;
                _context.CurrentUserId = apiResponse.Data.UserId;
            }
        }
    }

    [Given(@"(?:que )?el usuario ""(.*)"" con correo ""(.*)"" ha iniciado sesión como ""(.*)""")]
    [When(@"(?:que )?el usuario ""(.*)"" con correo ""(.*)"" ha iniciado sesión como ""(.*)""")]
    public async Task GivenQueElUsuarioHaIniciadoSesionComo(string userTag, string email, string roleCode)
    {
        // En los datos de prueba la contraseña estándar es "Smab2026!"
        var loginRequest = new LoginRequest(email, "Smab2026!");
        var response = await _context.Client.PostAsJsonAsync("/api/auth/login", loginRequest);

        _context.LastResponse = response;
        _context.LastResponseBody = await response.Content.ReadAsStringAsync();

        response.StatusCode.Should().Be(System.Net.HttpStatusCode.OK,
            $"El login para {email} debe ser exitoso con código 200, recibido: {(int)response.StatusCode}. Cuerpo: {_context.LastResponseBody}");

        var apiResponse = await _context.ReadResponseAs<LoginResult>();
        apiResponse.Should().NotBeNull();
        apiResponse!.Data.Should().NotBeNull();
        apiResponse.Data!.Token.Should().NotBeNullOrWhiteSpace();

        _context.Authenticate(apiResponse.Data.Token);
        _context.CurrentUserEmail = apiResponse.Data.Email;
        _context.CurrentUserId = apiResponse.Data.UserId;
    }

    [Then(@"la respuesta debe contener un token JWT válido")]
    public async Task ThenLaRespuestaDebeContenerUnTokenJwtValido()
    {
        var apiResponse = await _context.ReadResponseAs<LoginResult>();
        apiResponse.Should().NotBeNull();
        apiResponse!.Data.Should().NotBeNull();
        apiResponse.Data!.Token.Should().NotBeNullOrWhiteSpace();
        apiResponse.Data.ExpiresAt.Should().BeAfter(DateTime.UtcNow);
    }

    [Then(@"el perfil del usuario autenticado debe incluir el rol ""(.*)""")]
    public async Task ThenElPerfilDelUsuarioAutenticadoDebeIncluirElRol(string roleCode)
    {
        var apiResponse = await _context.ReadResponseAs<LoginResult>();
        apiResponse.Should().NotBeNull();
        apiResponse!.Data.Should().NotBeNull();
        apiResponse.Data!.Roles.Should().Contain(roleCode);
    }
}
