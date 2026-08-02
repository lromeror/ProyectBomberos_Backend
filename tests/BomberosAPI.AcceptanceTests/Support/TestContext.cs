using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;
using BomberosAPI.API.Common.Responses;
using BomberosAPI.Application.Features.Auth;
using BomberosAPI.Infrastructure.Persistence;
using Microsoft.Extensions.DependencyInjection;

namespace BomberosAPI.AcceptanceTests.Support;

public class TestContext : IDisposable
{
    public CustomWebApplicationFactory Factory { get; }
    public HttpClient Client { get; }

    public HttpResponseMessage? LastResponse { get; set; }
    public string? LastResponseBody { get; set; }

    public string? CurrentToken { get; set; }
    public string? CurrentUserEmail { get; set; }
    public Guid? CurrentUserId { get; set; }

    public Guid? LastCreatedSessionId { get; set; }
    public Guid? LastCreatedParticipantId { get; set; }
    public Guid? LastCreatedVitalSignsId { get; set; }
    public Guid? CurrentTraineeId { get; set; }
    public Guid? CurrentHealthPersonnelId { get; set; }
    public Guid? OtherTraineeId { get; set; }
    public Guid? OtherParticipantId { get; set; }

    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNameCaseInsensitive = true
    };

    public TestContext()
    {
        Factory = new CustomWebApplicationFactory();
        Client = Factory.CreateClient();
    }

    public void Authenticate(string token)
    {
        CurrentToken = token;
        Client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);
    }

    public void ClearAuthentication()
    {
        CurrentToken = null;
        CurrentUserEmail = null;
        CurrentUserId = null;
        Client.DefaultRequestHeaders.Authorization = null;
    }

    public async Task<ApiResponse<T>?> ReadResponseAs<T>()
    {
        if (string.IsNullOrWhiteSpace(LastResponseBody))
            return null;

        return JsonSerializer.Deserialize<ApiResponse<T>>(LastResponseBody, JsonOptions);
    }

    public AppDbContext GetDbContext()
    {
        var scope = Factory.Services.CreateScope();
        return scope.ServiceProvider.GetRequiredService<AppDbContext>();
    }

    public void Dispose()
    {
        Client.Dispose();
        Factory.Dispose();
    }
}
