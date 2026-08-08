using BomberosAPI.API.Common.Extensions;
using BomberosAPI.Application.Features.Auth;
using BomberosAPI.Application.Features.HealthPersonnel;
using BomberosAPI.Application.Features.TraineeFirefighters;
using BomberosAPI.Application.Features.TrainingSessions;
using BomberosAPI.Infrastructure;
using BomberosAPI.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Scalar.AspNetCore;

var builder = WebApplication.CreateBuilder(args);

// Permite instalar y correr esta API como Windows Service (ver deploy/install-service.ps1).
// No-op cuando se ejecuta de cualquier otra forma (dotnet run, IIS, consola) — es seguro
// dejarlo siempre activo, incluido en desarrollo local.
builder.Host.UseWindowsService();

// Database
builder.Services.AddDbContext<AppDbContext>(options =>
    options.UseSqlServer(builder.Configuration.GetConnectionString("DefaultConnection")));

// Infrastructure (BCrypt, JWT, repositories)
builder.Services.AddInfrastructure(builder.Configuration);

// Application services
builder.Services.AddScoped<AuthService>();
builder.Services.AddScoped<TrainingSessionService>();
builder.Services.AddScoped<TraineeFirefighterService>();
builder.Services.AddScoped<HealthPersonnelService>();

// Controllers + FluentValidation
builder.Services.AddControllers();
builder.Services.AddValidation();

// JWT Authentication + Authorization policies
builder.Services.AddJwtAuthentication(builder.Configuration);

// ICurrentUserService — lee claims del JWT en cada request
builder.Services.AddCurrentUser();

// CORS — orígenes permitidos. Se leen de "Cors:AllowedOrigins" en el appsettings del
// entorno activo (ver appsettings.Production.json) para no tener que recompilar cuando
// cambie el dominio/IP del servidor; si esa sección no existe (como en Local/Development
// hoy) se usa la lista de desarrollo de siempre.
var configuredOrigins = builder.Configuration.GetSection("Cors:AllowedOrigins").Get<string[]>();
var corsOrigins = configuredOrigins is { Length: > 0 }
    ? configuredOrigins
    : [
        "http://localhost:8081",
        "http://localhost:19006",
        "http://localhost:3000",
        "http://100.89.25.34:8081",
      ];

builder.Services.AddCors(options =>
    options.AddDefaultPolicy(policy =>
        policy.WithOrigins(corsOrigins)
              .AllowAnyHeader()
              .AllowAnyMethod()));

// OpenAPI / Scalar / Swagger
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddOpenApi();
builder.Services.AddSwaggerGen();

var app = builder.Build();

if (app.Environment.IsDevelopment())
    await DbSeeder.SeedAsync(app.Services);

app.UseGlobalExceptionMiddleware();

// Swagger/OpenAPI/Scalar solo fuera de Production: no tiene sentido exponer el
// esquema completo de la API (y una UI para probarla) en un servidor real.
if (!app.Environment.IsProduction())
{
    app.UseSwagger();
    app.UseSwaggerUI(c => c.SwaggerEndpoint("/swagger/v1/swagger.json", "BomberosAPI v1"));
    app.MapOpenApi();
    app.MapScalarApiReference();
}

app.UseHttpsRedirection();
app.UseCors();
app.UseAuthentication();
app.UseAuthorization();
app.MapControllers();

app.Run();
