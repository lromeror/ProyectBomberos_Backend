using BomberosAPI.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace BomberosAPI.Infrastructure.Persistence;

public class AppDbContext : DbContext
{
    public AppDbContext(DbContextOptions<AppDbContext> options) : base(options) { }

    public DbSet<TrainingInstitution> TrainingInstitutions => Set<TrainingInstitution>();
    public DbSet<User> Users => Set<User>();
    public DbSet<Role> Roles => Set<Role>();
    public DbSet<UserRole> UserRoles => Set<UserRole>();
    public DbSet<TraineeFirefighter> TraineeFirefighters => Set<TraineeFirefighter>();
    public DbSet<HealthPersonnel> HealthPersonnel => Set<HealthPersonnel>();
    public DbSet<TrainingLocation> TrainingLocations => Set<TrainingLocation>();
    public DbSet<TrainingSession> TrainingSessions => Set<TrainingSession>();
    public DbSet<SessionParticipant> SessionParticipants => Set<SessionParticipant>();
    public DbSet<MedicalHistory> MedicalHistories => Set<MedicalHistory>();
    public DbSet<VitalSignsMeasurement> VitalSignsMeasurements => Set<VitalSignsMeasurement>();
    public DbSet<SymptomReport> SymptomReports => Set<SymptomReport>();
    public DbSet<BioimpedanceMeasurement> BioimpedanceMeasurements => Set<BioimpedanceMeasurement>();
    public DbSet<EnvironmentalData> EnvironmentalData => Set<EnvironmentalData>();
    public DbSet<SessionResult> SessionResults => Set<SessionResult>();
    public DbSet<Invitation> Invitations => Set<Invitation>();
    public DbSet<CriticalAlert> CriticalAlerts => Set<CriticalAlert>();
    public DbSet<UserCredential> UserCredentials => Set<UserCredential>();
    public DbSet<AuthSession> AuthSessions => Set<AuthSession>();
    public DbSet<PasswordResetToken> PasswordResetTokens => Set<PasswordResetToken>();
    public DbSet<AccountActivationToken> AccountActivationTokens => Set<AccountActivationToken>();
    public DbSet<ConsentDocument> ConsentDocuments => Set<ConsentDocument>();
    public DbSet<UserConsent> UserConsents => Set<UserConsent>();
    public DbSet<DSARRequest> DSARRequests => Set<DSARRequest>();
    public DbSet<AccessAudit> AccessAudits => Set<AccessAudit>();
    public DbSet<ChangeAudit> ChangeAudits => Set<ChangeAudit>();
    public DbSet<OfflineSyncQueue> OfflineSyncQueues => Set<OfflineSyncQueue>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.ApplyConfigurationsFromAssembly(typeof(AppDbContext).Assembly);
    }
}
