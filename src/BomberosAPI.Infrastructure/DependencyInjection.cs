using BomberosAPI.Application.Common.Interfaces;
using BomberosAPI.Application.Features.Audit;
using BomberosAPI.Application.Features.Auth;
using BomberosAPI.Application.Features.BioimpedanceMeasurements;
using BomberosAPI.Application.Features.Consents;
using BomberosAPI.Application.Features.CriticalAlerts;
using BomberosAPI.Application.Features.DSARRequests;
using BomberosAPI.Application.Features.EnvironmentalData;
using BomberosAPI.Application.Features.HealthPersonnel;
using BomberosAPI.Application.Features.Institutions;
using BomberosAPI.Application.Features.Invitations;
using BomberosAPI.Application.Features.MedicalHistory;
using BomberosAPI.Application.Features.Participants;
using BomberosAPI.Application.Features.Reports;
using BomberosAPI.Application.Features.Roles;
using BomberosAPI.Application.Features.SessionResults;
using BomberosAPI.Application.Features.SymptomReports;
using BomberosAPI.Application.Features.TraineeFirefighters;
using BomberosAPI.Application.Features.TrainingLocations;
using BomberosAPI.Application.Features.TrainingSessions;
using BomberosAPI.Application.Features.Users;
using BomberosAPI.Application.Features.VitalSigns;
using BomberosAPI.Domain.Repositories;
using BomberosAPI.Infrastructure.Repositories;
using BomberosAPI.Infrastructure.Services;
using FluentValidation;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace BomberosAPI.Infrastructure;

public static class DependencyInjection
{
    public static IServiceCollection AddInfrastructure(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        services.Configure<JwtSettings>(configuration.GetSection(JwtSettings.SectionName));
        services.Configure<ResendSettings>(configuration.GetSection(ResendSettings.SectionName));
        services.Configure<AppUrlSettings>(configuration.GetSection(AppUrlSettings.SectionName));
        services.AddSingleton<IAppUrlProvider, AppUrlProvider>();
        services.AddHttpClient<IEmailSender, ResendEmailSender>();

        services.AddScoped<IAccessAuditRepository, AccessAuditRepository>();
        services.AddScoped<AccessAuditService>();
        services.AddScoped<IChangeAuditRepository, ChangeAuditRepository>();
        services.AddScoped<ChangeAuditService>();

        services.AddScoped<IPasswordHasher, PasswordHasher>();
        services.AddScoped<IJwtTokenService, JwtTokenService>();
        services.AddScoped<IAuthRepository, AuthRepository>();
        services.AddScoped<IAuthSessionRepository, AuthSessionRepository>();
        services.AddScoped<AuthSessionService>();
        services.AddScoped<IValidator<Application.Features.Auth.ChangePasswordRequest>, Application.Features.Auth.ChangePasswordRequestValidator>();

        services.AddScoped<IUserRepository, UserRepository>();
        services.AddScoped<IUserRoleRepository, UserRoleRepository>();
        services.AddScoped<UserService>();
        services.AddScoped<IValidator<CreateUserRequest>, CreateUserValidator>();

        services.AddScoped<IRoleRepository, RoleRepository>();
        services.AddScoped<RoleService>();

        services.AddScoped<ITrainingInstitutionRepository, TrainingInstitutionRepository>();
        services.AddScoped<InstitutionService>();
        services.AddScoped<IValidator<CreateInstitutionRequest>, CreateInstitutionValidator>();

        services.AddScoped<ITrainingLocationRepository, TrainingLocationRepository>();
        services.AddScoped<TrainingLocationService>();

        services.AddScoped<ITraineeFirefighterRepository, TraineeFirefighterRepository>();
        services.AddScoped<TraineeFirefighterService>();
        services.AddScoped<IValidator<CreateTraineeFirefighterRequest>, CreateTraineeFirefighterValidator>();

        services.AddScoped<ITrainingSessionRepository, TrainingSessionRepository>();
        services.AddScoped<TrainingSessionService>();
        services.AddScoped<IValidator<CreateTrainingSessionRequest>, CreateTrainingSessionValidator>();

        services.AddScoped<ISessionParticipantRepository, SessionParticipantRepository>();
        services.AddScoped<SessionParticipantService>();
        services.AddScoped<IValidator<CreateSessionParticipantRequest>, CreateSessionParticipantValidator>();

        services.AddScoped<IInvitationRepository, InvitationRepository>();
        services.AddScoped<InvitationService>();
        services.AddScoped<IValidator<CreateInvitationRequest>, CreateInvitationValidator>();

        services.AddScoped<IHealthPersonnelRepository, HealthPersonnelRepository>();
        services.AddScoped<HealthPersonnelService>();
        services.AddScoped<IValidator<CreateHealthPersonnelRequest>, CreateHealthPersonnelValidator>();

        services.AddScoped<IMedicalHistoryRepository, MedicalHistoryRepository>();
        services.AddScoped<MedicalHistoryService>();
        services.AddScoped<IValidator<CreateMedicalHistoryRequest>, CreateMedicalHistoryValidator>();

        services.AddScoped<IVitalSignsMeasurementRepository, VitalSignsMeasurementRepository>();
        services.AddScoped<VitalSignsMeasurementService>();
        services.AddScoped<IValidator<CreateVitalSignsMeasurementRequest>, CreateVitalSignsMeasurementValidator>();

        services.AddScoped<IEnvironmentalDataRepository, EnvironmentalDataRepository>();
        services.AddScoped<EnvironmentalDataService>();
        services.AddScoped<IValidator<CreateEnvironmentalDataRequest>, CreateEnvironmentalDataValidator>();

        services.AddScoped<ISymptomReportRepository, SymptomReportRepository>();
        services.AddScoped<SymptomReportService>();
        services.AddScoped<IValidator<CreateSymptomReportRequest>, CreateSymptomReportValidator>();

        services.AddScoped<IBioimpedanceMeasurementRepository, BioimpedanceMeasurementRepository>();
        services.AddScoped<BioimpedanceMeasurementService>();
        services.AddScoped<IValidator<CreateBioimpedanceMeasurementRequest>, CreateBioimpedanceMeasurementValidator>();

        services.AddScoped<IConsentDocumentRepository, ConsentDocumentRepository>();
        services.AddScoped<IUserConsentRepository, UserConsentRepository>();
        services.AddScoped<ConsentService>();

        services.AddScoped<IDSARRequestRepository, DSARRequestRepository>();
        services.AddScoped<DSARRequestService>();

        services.AddScoped<ICriticalAlertRepository, CriticalAlertRepository>();
        services.AddScoped<CriticalAlertService>();

        services.AddScoped<ISessionResultRepository, SessionResultRepository>();
        services.AddScoped<SessionResultService>();

        services.AddScoped<ReportService>();

        return services;
    }
}
