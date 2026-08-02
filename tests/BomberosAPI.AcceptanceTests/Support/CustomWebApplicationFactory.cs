using BomberosAPI.Application.Common.Constants;
using BomberosAPI.Application.Common.Interfaces;
using BomberosAPI.Domain.Entities;
using BomberosAPI.Infrastructure.Persistence;
using BomberosAPI.Infrastructure.Services;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Storage;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;

namespace BomberosAPI.AcceptanceTests.Support;

public class FakeEmailSender : IEmailSender
{
    public List<(string ToEmail, string Subject, string Body)> SentEmails { get; } = [];

    public Task SendAsync(string toEmail, string subject, string htmlBody, CancellationToken ct = default)
    {
        SentEmails.Add((toEmail, subject, htmlBody));
        return Task.CompletedTask;
    }
}

public class CustomWebApplicationFactory : WebApplicationFactory<Program>
{
    private readonly string _databaseName = "AcceptanceTestsDb_" + Guid.NewGuid().ToString("N");
    private readonly InMemoryDatabaseRoot _databaseRoot = new();

    static CustomWebApplicationFactory()
    {
        Environment.SetEnvironmentVariable("JwtSettings__SecretKey", "smab_acceptance_test_secret_key_super_secure_2026_at_least_32_bytes_long!");
        Environment.SetEnvironmentVariable("JwtSettings__Issuer", "BomberosAPI.AcceptanceTests");
        Environment.SetEnvironmentVariable("JwtSettings__Audience", "BomberosAPI.AcceptanceTests");
        Environment.SetEnvironmentVariable("JwtSettings__ExpirationMinutes", "120");
        Environment.SetEnvironmentVariable("ConnectionStrings__DefaultConnection", "Server=localhost;Database=BomberosDB_Test;Trusted_Connection=True;");
        Environment.SetEnvironmentVariable("App__WebBaseUrl", "http://localhost:3000");
    }

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.UseEnvironment("Development");

        builder.ConfigureServices(services =>
        {
            // Configure strongly typed settings
            services.Configure<JwtSettings>(options =>
            {
                options.SecretKey = "smab_acceptance_test_secret_key_super_secure_2026_at_least_32_bytes_long!";
                options.Issuer = "BomberosAPI.AcceptanceTests";
                options.Audience = "BomberosAPI.AcceptanceTests";
                options.ExpirationMinutes = 120;
            });

            services.Configure<AppUrlSettings>(options =>
            {
                options.WebBaseUrl = "http://localhost:3000";
            });

            // Replace email sender with in-memory fake
            services.RemoveAll<IEmailSender>();
            services.AddSingleton<IEmailSender, FakeEmailSender>();

            // Remove all EF Core related services and options to prevent multiple provider registrations
            var descriptorsToRemove = services.Where(d =>
                d.ServiceType == typeof(AppDbContext) ||
                d.ServiceType == typeof(DbContextOptions) ||
                d.ServiceType == typeof(DbContextOptions<AppDbContext>) ||
                (d.ServiceType.Namespace != null && d.ServiceType.Namespace.StartsWith("Microsoft.EntityFrameworkCore")) ||
                (d.ImplementationType != null && d.ImplementationType.Namespace != null && d.ImplementationType.Namespace.StartsWith("Microsoft.EntityFrameworkCore"))
            ).ToList();

            foreach (var descriptor in descriptorsToRemove)
            {
                services.Remove(descriptor);
            }

            // Register InMemory Database for isolated acceptance tests
            services.AddDbContext<AppDbContext>(options =>
            {
                options.UseInMemoryDatabase(_databaseName, _databaseRoot);
            });

            // Ensure database is created and seeded
            var sp = services.BuildServiceProvider();
            using var scope = sp.CreateScope();
            var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();

            db.Database.EnsureCreated();

            // Run standard seed
            DbSeeder.SeedAsync(scope.ServiceProvider).GetAwaiter().GetResult();

            // Additional seed for Burn House location if not present
            var institution = db.TrainingInstitutions.FirstOrDefault();
            if (institution != null)
            {
                if (!db.TrainingLocations.Any(l => l.Name.Contains("Burn House") || l.Name.Contains("Casa de Fuego")))
                {
                    db.TrainingLocations.Add(new TrainingLocation
                    {
                        TrainingLocationId = Guid.NewGuid(),
                        InstitutionId = institution.InstitutionId,
                        Name = "Casa de Fuego (Burn House)",
                        LocationType = "Live Fire Training",
                        Address = "Av. Bomberos 5000, Campo de Pruebas",
                        MaxCapacity = 20
                    });
                    db.SaveChanges();
                }
            }
        });
    }
}
