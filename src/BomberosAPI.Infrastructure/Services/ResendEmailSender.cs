using System.Net.Http.Json;
using System.Text.Json.Serialization;
using BomberosAPI.Application.Common.Interfaces;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace BomberosAPI.Infrastructure.Services;

/// <summary>
/// Envía correo real vía la API HTTP de Resend (https://resend.com) — no requiere
/// dominio propio: el remitente por defecto de Resend (ej. onboarding@resend.dev)
/// funciona para desarrollo/demo sin verificar nada.
/// </summary>
public class ResendEmailSender : IEmailSender
{
    private readonly HttpClient _http;
    private readonly ResendSettings _settings;
    private readonly ILogger<ResendEmailSender> _logger;

    public ResendEmailSender(HttpClient http, IOptions<ResendSettings> settings, ILogger<ResendEmailSender> logger)
    {
        _http = http;
        _settings = settings.Value;
        _logger = logger;
    }

    public async Task SendAsync(string toEmail, string subject, string htmlBody, CancellationToken ct = default)
    {
        var request = new HttpRequestMessage(HttpMethod.Post, "https://api.resend.com/emails")
        {
            Content = JsonContent.Create(new ResendEmailPayload(
                From: $"{_settings.FromName} <{_settings.FromEmail}>",
                To: [toEmail],
                Subject: subject,
                Html: htmlBody))
        };
        request.Headers.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", _settings.ApiKey);

        var response = await _http.SendAsync(request, ct);
        if (!response.IsSuccessStatusCode)
        {
            var body = await response.Content.ReadAsStringAsync(ct);
            _logger.LogError("Resend rechazó el envío a {Email}: {Status} — {Body}", toEmail, response.StatusCode, body);
            throw new InvalidOperationException($"No se pudo enviar el correo (Resend respondió {(int)response.StatusCode}).");
        }
    }

    private record ResendEmailPayload(
        [property: JsonPropertyName("from")] string From,
        [property: JsonPropertyName("to")] string[] To,
        [property: JsonPropertyName("subject")] string Subject,
        [property: JsonPropertyName("html")] string Html);
}
