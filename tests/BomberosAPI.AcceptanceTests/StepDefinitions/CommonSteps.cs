using System.Net;
using BomberosAPI.AcceptanceTests.Support;
using FluentAssertions;
using Reqnroll;

namespace BomberosAPI.AcceptanceTests.StepDefinitions;

[Binding]
public class CommonSteps
{
    private readonly TestContext _context;

    public CommonSteps(TestContext context)
    {
        _context = context;
    }

    [Then(@"la respuesta debe tener código de estado (.*)")]
    [Then(@"la respuesta de autenticación debe tener código de estado (.*)")]
    public void ThenLaRespuestaDebeTenerCodigoDeEstado(int statusCode)
    {
        _context.LastResponse.Should().NotBeNull("Se esperaba una respuesta HTTP previa.");
        ((int)_context.LastResponse!.StatusCode).Should().Be(statusCode,
            $"Respuesta esperada: {statusCode} ({((HttpStatusCode)statusCode)}), recibida: {(int)_context.LastResponse.StatusCode} con cuerpo: {_context.LastResponseBody}");
    }
}
