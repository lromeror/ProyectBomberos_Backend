using System.Text;
using System.Text.Json;
using BomberosAPI.API.Common.Responses;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.IdentityModel.Tokens;

namespace BomberosAPI.API.Common.Extensions;

public static class AuthServiceExtensions
{
    private static readonly JsonSerializerOptions JsonOptions = new()
    { PropertyNamingPolicy = JsonNamingPolicy.CamelCase };

    public static IServiceCollection AddJwtAuthentication(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        var secretKey = configuration["JwtSettings:SecretKey"]
            ?? throw new InvalidOperationException("JwtSettings:SecretKey is not configured.");

        // Without this, .NET remaps "sub" → ClaimTypes.NameIdentifier, breaking FindFirstValue(JwtRegisteredClaimNames.Sub)
        System.IdentityModel.Tokens.Jwt.JwtSecurityTokenHandler.DefaultInboundClaimTypeMap.Clear();

        services
            .AddAuthentication(o =>
            {
                o.DefaultAuthenticateScheme = JwtBearerDefaults.AuthenticationScheme;
                o.DefaultChallengeScheme    = JwtBearerDefaults.AuthenticationScheme;
            })
            .AddJwtBearer(o =>
            {
                o.TokenValidationParameters = new TokenValidationParameters
                {
                    ValidateIssuerSigningKey = true,
                    IssuerSigningKey         = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(secretKey)),
                    ValidateIssuer           = true,
                    ValidIssuer              = configuration["JwtSettings:Issuer"],
                    ValidateAudience         = true,
                    ValidAudience            = configuration["JwtSettings:Audience"],
                    ValidateLifetime         = true,
                    ClockSkew                = TimeSpan.Zero
                };

                // Devuelve ApiResponse consistente cuando el token falta o es invalido (401)
                o.Events = new JwtBearerEvents
                {
                    OnChallenge = async ctx =>
                    {
                        ctx.HandleResponse();
                        ctx.Response.StatusCode  = 401;
                        ctx.Response.ContentType = "application/json";
                        var body = ApiResponse.Fail(401, "Authentication is required. Provide a valid Bearer token.");
                        await ctx.Response.WriteAsync(JsonSerializer.Serialize(body, JsonOptions));
                    },
                    // Devuelve ApiResponse consistente cuando el rol no es suficiente (403)
                    OnForbidden = async ctx =>
                    {
                        ctx.Response.StatusCode  = 403;
                        ctx.Response.ContentType = "application/json";
                        var body = ApiResponse.Fail(403, "You do not have permission to perform this action.");
                        await ctx.Response.WriteAsync(JsonSerializer.Serialize(body, JsonOptions));
                    }
                };
            });

        // Nota: se probó centralizar los sets de roles en policies con nombre
        // (AddAuthorizationBuilder().AddPolicy(...)), pero ningún controller llegó a
        // usarlas — todos siguen componiendo `Roles = "..."` inline por acción, con
        // combinaciones más finas que las 4 policies genéricas que había acá (ver
        // p.ej. TrainingSessionsController.ManageSessionRoles, que incluye FireChief).
        // Las policies quedaban como código muerto que además había empezado a
        // desincronizarse de los roles realmente exigidos — se retiran en vez de
        // dejarlas como una fuente de verdad falsa.
        services.AddAuthorization();

        return services;
    }
}
