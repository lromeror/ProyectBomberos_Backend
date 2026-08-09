-- ============================================================
-- BomberosAPI - Script completo de base de datos
-- Motor: SQL Server / Azure SQL
-- Generado desde: EF Core 10 + entidades del dominio
-- ============================================================

-- ============================================================
-- LIMPIEZA PREVIA (orden inverso de dependencias FK)
-- Permite ejecutar el script múltiples veces sin error
-- ============================================================

IF OBJECT_ID(N'[CriticalAlert]',           'U') IS NOT NULL DROP TABLE [CriticalAlert];
IF OBJECT_ID(N'[DSARRequest]',             'U') IS NOT NULL DROP TABLE [DSARRequest];
IF OBJECT_ID(N'[UserConsent]',             'U') IS NOT NULL DROP TABLE [UserConsent];
IF OBJECT_ID(N'[SessionResult]',           'U') IS NOT NULL DROP TABLE [SessionResult];
IF OBJECT_ID(N'[SymptomReport]',           'U') IS NOT NULL DROP TABLE [SymptomReport];
IF OBJECT_ID(N'[EnvironmentalData]',       'U') IS NOT NULL DROP TABLE [EnvironmentalData];
IF OBJECT_ID(N'[BioimpedanceMeasurement]', 'U') IS NOT NULL DROP TABLE [BioimpedanceMeasurement];
IF OBJECT_ID(N'[VitalSignsMeasurement]',   'U') IS NOT NULL DROP TABLE [VitalSignsMeasurement];
IF OBJECT_ID(N'[MedicalHistory]',          'U') IS NOT NULL DROP TABLE [MedicalHistory];
IF OBJECT_ID(N'[SessionParticipant]',      'U') IS NOT NULL DROP TABLE [SessionParticipant];
IF OBJECT_ID(N'[Invitation]',              'U') IS NOT NULL DROP TABLE [Invitation];
IF OBJECT_ID(N'[TrainingSession]',         'U') IS NOT NULL DROP TABLE [TrainingSession];
IF OBJECT_ID(N'[TrainingLocation]',        'U') IS NOT NULL DROP TABLE [TrainingLocation];
IF OBJECT_ID(N'[OfflineSyncQueue]',        'U') IS NOT NULL DROP TABLE [OfflineSyncQueue];
IF OBJECT_ID(N'[ChangeAudit]',             'U') IS NOT NULL DROP TABLE [ChangeAudit];
IF OBJECT_ID(N'[AccessAudit]',             'U') IS NOT NULL DROP TABLE [AccessAudit];
IF OBJECT_ID(N'[PasswordResetToken]',      'U') IS NOT NULL DROP TABLE [PasswordResetToken];
IF OBJECT_ID(N'[AuthSession]',             'U') IS NOT NULL DROP TABLE [AuthSession];
IF OBJECT_ID(N'[UserRole]',                'U') IS NOT NULL DROP TABLE [UserRole];
IF OBJECT_ID(N'[UserCredential]',          'U') IS NOT NULL DROP TABLE [UserCredential];
IF OBJECT_ID(N'[HealthPersonnel]',         'U') IS NOT NULL DROP TABLE [HealthPersonnel];
IF OBJECT_ID(N'[TraineeFirefighter]',      'U') IS NOT NULL DROP TABLE [TraineeFirefighter];
IF OBJECT_ID(N'[User]',                    'U') IS NOT NULL DROP TABLE [User];
IF OBJECT_ID(N'[Role]',                    'U') IS NOT NULL DROP TABLE [Role];
IF OBJECT_ID(N'[TrainingInstitution]',     'U') IS NOT NULL DROP TABLE [TrainingInstitution];
IF OBJECT_ID(N'[ConsentDocument]',         'U') IS NOT NULL DROP TABLE [ConsentDocument];
IF OBJECT_ID(N'[__EFMigrationsHistory]',   'U') IS NOT NULL DROP TABLE [__EFMigrationsHistory];
GO

-- Tabla de control de migraciones EF Core
IF OBJECT_ID(N'[__EFMigrationsHistory]') IS NULL
BEGIN
    CREATE TABLE [__EFMigrationsHistory] (
        [MigrationId] nvarchar(150) NOT NULL,
        [ProductVersion] nvarchar(32) NOT NULL,
        CONSTRAINT [PK___EFMigrationsHistory] PRIMARY KEY ([MigrationId])
    );
END;
GO

BEGIN TRANSACTION;

-- ============================================================
-- TABLAS INDEPENDIENTES (sin FK salientes)
-- ============================================================

CREATE TABLE [Role] (
    [role_id]     uniqueidentifier NOT NULL,
    [code]        nvarchar(50)     NOT NULL,
    [name]        nvarchar(100)    NOT NULL,
    [description] nvarchar(500)    NULL,
    CONSTRAINT [PK_Role] PRIMARY KEY ([role_id])
);

CREATE TABLE [TrainingInstitution] (
    [institution_id] uniqueidentifier NOT NULL,
    [name]           nvarchar(200)    NOT NULL,
    [acronym]        nvarchar(20)     NULL,
    [country]        nvarchar(100)    NULL,
    [city]           nvarchar(100)    NULL,
    [is_active]      bit              NOT NULL DEFAULT 1,
    CONSTRAINT [PK_TrainingInstitution] PRIMARY KEY ([institution_id])
);

CREATE TABLE [ConsentDocument] (
    [consent_document_id] uniqueidentifier NOT NULL,
    [consent_type]        nvarchar(100)    NOT NULL,
    [version]             nvarchar(20)     NOT NULL,
    [text_content]        nvarchar(max)    NULL,
    [valid_from]          datetime2        NOT NULL,
    [valid_until]         datetime2        NULL,
    CONSTRAINT [PK_ConsentDocument] PRIMARY KEY ([consent_document_id])
);

CREATE TABLE [OfflineSyncQueue] (
    [offline_sync_queue_id] uniqueidentifier NOT NULL,
    [source_table]          nvarchar(100)    NOT NULL,
    [record_id]             uniqueidentifier NOT NULL,
    [payload_json]          nvarchar(max)    NOT NULL,
    [attempts]              int              NOT NULL DEFAULT 0,
    [max_attempts]          int              NOT NULL DEFAULT 3,
    [synced]                bit              NOT NULL DEFAULT 0,
    [created_at]            datetime2        NOT NULL,
    [synced_at]             datetime2        NULL,
    CONSTRAINT [PK_OfflineSyncQueue] PRIMARY KEY ([offline_sync_queue_id])
);

-- ============================================================
-- USUARIOS Y AUTENTICACIÓN
-- ============================================================

CREATE TABLE [User] (
    [user_id]          uniqueidentifier NOT NULL,
    [institution_id]   uniqueidentifier NOT NULL,
    [email]            nvarchar(254)    NOT NULL,
    [first_name]       nvarchar(150)    NOT NULL,
    [last_name]        nvarchar(150)    NOT NULL,
    [phone]            nvarchar(30)     NULL,
    [account_status]   nvarchar(50)     NOT NULL DEFAULT 'active',
    [created_at]       datetime2        NOT NULL,
    [last_access_at]   datetime2        NULL,
    CONSTRAINT [PK_User] PRIMARY KEY ([user_id])
);

CREATE TABLE [UserCredential] (
    [user_credential_id]      uniqueidentifier NOT NULL,
    [user_id]                 uniqueidentifier NOT NULL,
    [password_hash]           nvarchar(500)    NOT NULL,
    [hash_algorithm]          nvarchar(50)     NOT NULL DEFAULT 'bcrypt',
    [mfa_enabled]             bit              NOT NULL DEFAULT 0,
    [failed_attempts]         int              NOT NULL DEFAULT 0,
    [locked_until]            datetime2        NULL,
    [last_password_change_at] datetime2        NULL,
    CONSTRAINT [PK_UserCredential] PRIMARY KEY ([user_credential_id])
);

CREATE TABLE [UserRole] (
    [user_role_id]       uniqueidentifier NOT NULL,
    [user_id]            uniqueidentifier NOT NULL,
    [role_id]            uniqueidentifier NOT NULL,
    [assigned_by_user_id] uniqueidentifier NULL,
    [start_date]         datetime2        NOT NULL,
    [end_date]           datetime2        NULL,
    [is_active]          bit              NOT NULL DEFAULT 1,
    CONSTRAINT [PK_UserRole] PRIMARY KEY ([user_role_id])
);

CREATE TABLE [AuthSession] (
    [auth_session_id]    uniqueidentifier NOT NULL,
    [user_id]            uniqueidentifier NOT NULL,
    [refresh_token_hash] nvarchar(500)    NOT NULL,
    [device]             nvarchar(200)    NULL,
    [ip]                 nvarchar(45)     NULL,
    [user_agent]         nvarchar(500)    NULL,
    [status]             nvarchar(50)     NOT NULL,
    [created_at]         datetime2        NOT NULL,
    [expires_at]         datetime2        NOT NULL,
    [closed_at]          datetime2        NULL,
    CONSTRAINT [PK_AuthSession] PRIMARY KEY ([auth_session_id])
);

CREATE TABLE [PasswordResetToken] (
    [password_reset_token_id] uniqueidentifier NOT NULL,
    [user_id]                 uniqueidentifier NOT NULL,
    [token_hash]              nvarchar(500)    NOT NULL,
    [status]                  nvarchar(50)     NOT NULL DEFAULT 'pending',
    [expires_at]              datetime2        NOT NULL,
    [used_at]                 datetime2        NULL,
    [created_at]              datetime2        NOT NULL,
    CONSTRAINT [PK_PasswordResetToken] PRIMARY KEY ([password_reset_token_id])
);

CREATE TABLE [AccessAudit] (
    [access_audit_id]  uniqueidentifier NOT NULL,
    [user_id]          uniqueidentifier NOT NULL,
    [auth_session_id]  uniqueidentifier NULL,
    [event]            nvarchar(100)    NOT NULL,
    [ip]               nvarchar(45)     NULL,
    [user_agent]       nvarchar(500)    NULL,
    [success]          bit              NOT NULL,
    [occurred_at]      datetime2        NOT NULL,
    [resource_id]      uniqueidentifier NULL,
    [resource_type]    nvarchar(100)    NULL,
    CONSTRAINT [PK_AccessAudit] PRIMARY KEY ([access_audit_id])
);

CREATE TABLE [ChangeAudit] (
    [change_audit_id]       uniqueidentifier NOT NULL,
    [actor_user_id]         uniqueidentifier NOT NULL,
    [entity]                nvarchar(100)    NOT NULL,
    [entity_id]             uniqueidentifier NOT NULL,
    [operation]             nvarchar(50)     NOT NULL,
    [previous_values_json]  nvarchar(max)    NULL,
    [new_values_json]       nvarchar(max)    NULL,
    [occurred_at]           datetime2        NOT NULL,
    CONSTRAINT [PK_ChangeAudit] PRIMARY KEY ([change_audit_id])
);

-- ============================================================
-- PERFILES
-- ============================================================

CREATE TABLE [TraineeFirefighter] (
    [trainee_firefighter_id]    uniqueidentifier NOT NULL,
    [user_id]                   uniqueidentifier NOT NULL,
    [applicant_code]            nvarchar(50)     NULL,
    [birth_date]                date             NULL,
    [sex]                       nvarchar(20)     NULL,
    [blood_type]                nvarchar(10)     NULL,
    [emergency_contact_name]    nvarchar(150)    NULL,
    [emergency_contact_phone]   nvarchar(30)     NULL,
    [training_status]           nvarchar(50)     NULL,
    CONSTRAINT [PK_TraineeFirefighter] PRIMARY KEY ([trainee_firefighter_id])
);

CREATE TABLE [HealthPersonnel] (
    [health_personnel_id]   uniqueidentifier NOT NULL,
    [user_id]               uniqueidentifier NOT NULL,
    [profession]            nvarchar(100)    NULL,
    [specialty]             nvarchar(100)    NULL,
    [license_number]        nvarchar(50)     NULL,
    [can_approve_discharges] bit             NOT NULL DEFAULT 0,
    [is_active]             bit              NOT NULL DEFAULT 1,
    CONSTRAINT [PK_HealthPersonnel] PRIMARY KEY ([health_personnel_id])
);

-- ============================================================
-- SESIONES DE ENTRENAMIENTO
-- ============================================================

CREATE TABLE [TrainingLocation] (
    [training_location_id] uniqueidentifier NOT NULL,
    [institution_id]       uniqueidentifier NOT NULL,
    [name]                 nvarchar(200)    NOT NULL,
    [location_type]        nvarchar(100)    NULL,
    [address]              nvarchar(300)    NULL,
    [max_capacity]         int              NULL,
    CONSTRAINT [PK_TrainingLocation] PRIMARY KEY ([training_location_id])
);

CREATE TABLE [TrainingSession] (
    [training_session_id]  uniqueidentifier NOT NULL,
    [institution_id]       uniqueidentifier NOT NULL,
    [training_location_id] uniqueidentifier NOT NULL,
    [created_by_user_id]   uniqueidentifier NOT NULL,
    [session_code]         nvarchar(50)     NULL,
    [title]                nvarchar(200)    NOT NULL,
    [description]          nvarchar(max)    NULL,
    [status]               nvarchar(50)     NOT NULL,
    [scheduled_start]      datetime2        NOT NULL,
    [scheduled_end]        datetime2        NOT NULL,
    [actual_start]         datetime2        NULL,
    [actual_end]           datetime2        NULL,
    [planned_capacity]     int              NULL,
    CONSTRAINT [PK_TrainingSession] PRIMARY KEY ([training_session_id])
);

CREATE TABLE [Invitation] (
    [invitation_id]         uniqueidentifier NOT NULL,
    [sender_user_id]        uniqueidentifier NOT NULL,
    [target_user_id]        uniqueidentifier NULL,
    [training_session_id]   uniqueidentifier NULL,
    [target_role_id]        uniqueidentifier NULL,
    [target_email]          nvarchar(254)    NOT NULL,
    [invitation_token_hash] nvarchar(500)    NOT NULL,
    [status]                nvarchar(50)     NOT NULL,
    [expires_at]            datetime2        NOT NULL,
    [responded_at]          datetime2        NULL,
    [created_at]            datetime2        NOT NULL,
    CONSTRAINT [PK_Invitation] PRIMARY KEY ([invitation_id])
);

CREATE TABLE [SessionParticipant] (
    [session_participant_id]  uniqueidentifier NOT NULL,
    [training_session_id]     uniqueidentifier NOT NULL,
    [trainee_firefighter_id]  uniqueidentifier NOT NULL,
    [invitation_id]           uniqueidentifier NULL,
    [participation_status]    nvarchar(50)     NULL,
    [attendance_confirmed]    bit              NOT NULL DEFAULT 0,
    [check_in_at]             datetime2        NULL,
    [observations]            nvarchar(max)    NULL,
    CONSTRAINT [PK_SessionParticipant] PRIMARY KEY ([session_participant_id])
);

-- ============================================================
-- DATOS DE SALUD
-- ============================================================

CREATE TABLE [MedicalHistory] (
    [medical_history_id]              uniqueidentifier NOT NULL,
    [trainee_firefighter_id]          uniqueidentifier NOT NULL,
    [created_by_health_personnel_id]  uniqueidentifier NOT NULL,
    [allergies]                       nvarchar(max)    NULL,
    [preexisting_conditions]          nvarchar(max)    NULL,
    [current_medication]              nvarchar(max)    NULL,
    [general_observations]            nvarchar(max)    NULL,
    [updated_at]                      datetime2        NOT NULL,
    CONSTRAINT [PK_MedicalHistory] PRIMARY KEY ([medical_history_id])
);

CREATE TABLE [VitalSignsMeasurement] (
    [vital_signs_measurement_id]          uniqueidentifier NOT NULL,
    [session_participant_id]              uniqueidentifier NOT NULL,
    [registered_by_health_personnel_id]   uniqueidentifier NOT NULL,
    [heart_rate]                          decimal(5,2)     NULL,
    [systolic_pressure]                   decimal(5,2)     NULL,
    [diastolic_pressure]                  decimal(5,2)     NULL,
    [temperature_c]                       decimal(4,2)     NULL,
    -- decimal(5,2), no (4,2): un Spo2 = 100 (lectura sana común) desbordaba (4,2), que
    -- solo llega a 99.99 (ver migración WidenPercentageColumnsPrecision).
    [spo2]                                decimal(5,2)     NULL,
    [practice_role]                       nvarchar(30)     NULL,
    [is_smoker]                           bit              NULL,
    [exposed_to_smoke_48h]                bit              NULL,
    [taken_at]                            datetime2        NOT NULL,
    CONSTRAINT [PK_VitalSignsMeasurement] PRIMARY KEY ([vital_signs_measurement_id])
);

CREATE TABLE [BioimpedanceMeasurement] (
    [bioimpedance_measurement_id]         uniqueidentifier NOT NULL,
    [session_participant_id]              uniqueidentifier NOT NULL,
    [registered_by_health_personnel_id]   uniqueidentifier NOT NULL,
    [weight_kg]                           decimal(5,2)     NULL,
    -- decimal(5,2), no (4,2): 100% de grasa/agua corporal (límite válido del
    -- validador) desbordaba (4,2), que solo llega a 99.99.
    [fat_percentage]                      decimal(5,2)     NULL,
    [muscle_mass_kg]                      decimal(5,2)     NULL,
    [body_water_pct]                      decimal(5,2)     NULL,
    [basal_metabolic_rate]                decimal(7,2)     NULL,
    [metabolic_age_years]                 decimal(5,2)     NULL,
    [lactate_pre_mmol]                    decimal(4,2)     NULL,
    [lactate_post_mmol]                   decimal(4,2)     NULL,
    [stroop_time_seconds]                 decimal(6,2)     NULL,
    [stroop_errors]                       int              NULL,
    [taken_at]                            datetime2        NOT NULL,
    CONSTRAINT [PK_BioimpedanceMeasurement] PRIMARY KEY ([bioimpedance_measurement_id])
);

CREATE TABLE [EnvironmentalData] (
    [environmental_data_id]  uniqueidentifier NOT NULL,
    [training_session_id]    uniqueidentifier NOT NULL,
    [registered_by_user_id]  uniqueidentifier NOT NULL,
    [temperature_c]          decimal(4,2)     NULL,
    -- decimal(5,2), no (4,2): 100% de humedad relativa (lluvia/ambiente saturado, un
    -- valor real) desbordaba (4,2), que solo llega a 99.99.
    [humidity_pct]           decimal(5,2)     NULL,
    [co_ppm]                 decimal(7,2)     NULL,
    [heat_stress_index]      decimal(5,2)     NULL,
    [measured_at]            datetime2        NOT NULL,
    CONSTRAINT [PK_EnvironmentalData] PRIMARY KEY ([environmental_data_id])
);

CREATE TABLE [SymptomReport] (
    [symptom_report_id]      uniqueidentifier NOT NULL,
    [session_participant_id] uniqueidentifier NOT NULL,
    [reported_by_user_id]    uniqueidentifier NOT NULL,
    [severity]               nvarchar(50)     NULL,
    [symptoms]               nvarchar(max)    NULL,
    [requires_alert]         bit              NOT NULL DEFAULT 0,
    [reported_at]            datetime2        NOT NULL,
    CONSTRAINT [PK_SymptomReport] PRIMARY KEY ([symptom_report_id])
);

CREATE TABLE [SessionResult] (
    [session_result_id]      uniqueidentifier NOT NULL,
    [session_participant_id] uniqueidentifier NOT NULL,
    [validated_by_user_id]   uniqueidentifier NOT NULL,
    [performance_score]      decimal(5,2)     NULL,
    [risk_classification]    nvarchar(50)     NULL,
    [fit_to_continue]        bit              NOT NULL DEFAULT 1,
    [summary]                nvarchar(max)    NULL,
    [generated_at]           datetime2        NOT NULL,
    CONSTRAINT [PK_SessionResult] PRIMARY KEY ([session_result_id])
);

CREATE TABLE [CriticalAlert] (
    [critical_alert_id]           uniqueidentifier NOT NULL,
    [session_participant_id]      uniqueidentifier NOT NULL,
    [vital_signs_measurement_id]  uniqueidentifier NULL,
    [symptom_report_id]           uniqueidentifier NULL,
    [environmental_data_id]       uniqueidentifier NULL,
    [attended_by_user_id]         uniqueidentifier NULL,
    [alert_type]                  nvarchar(100)    NOT NULL,
    [severity]                    nvarchar(50)     NOT NULL,
    [status]                      nvarchar(50)     NOT NULL,
    [description]                 nvarchar(max)    NULL,
    [generated_at]                datetime2        NOT NULL,
    [attended_at]                 datetime2        NULL,
    CONSTRAINT [PK_CriticalAlert] PRIMARY KEY ([critical_alert_id])
);

-- ============================================================
-- CONSENTIMIENTO / GDPR
-- ============================================================

CREATE TABLE [UserConsent] (
    [user_consent_id]     uniqueidentifier NOT NULL,
    [user_id]             uniqueidentifier NOT NULL,
    [institution_id]      uniqueidentifier NOT NULL,
    [consent_document_id] uniqueidentifier NOT NULL,
    [status]              nvarchar(50)     NOT NULL DEFAULT 'active',
    [granted_at]          datetime2        NOT NULL,
    [revoked_at]          datetime2        NULL,
    [expires_at]          datetime2        NULL,
    CONSTRAINT [PK_UserConsent] PRIMARY KEY ([user_consent_id])
);

CREATE TABLE [DSARRequest] (
    [dsar_request_id]    uniqueidentifier NOT NULL,
    [user_id]            uniqueidentifier NOT NULL,
    [managed_by_user_id] uniqueidentifier NULL,
    [right_type]         nvarchar(100)    NOT NULL,
    [status]             nvarchar(50)     NOT NULL,
    [description]        nvarchar(max)    NULL,
    [response]           nvarchar(max)    NULL,
    [requested_at]       datetime2        NOT NULL,
    [responded_at]       datetime2        NULL,
    [legal_deadline_at]  datetime2        NULL,
    CONSTRAINT [PK_DSARRequest] PRIMARY KEY ([dsar_request_id])
);

-- ============================================================
-- ÍNDICES ÚNICOS
-- ============================================================

CREATE UNIQUE INDEX [IX_Role_code]              ON [Role]           ([code]);
CREATE UNIQUE INDEX [IX_User_email]             ON [User]           ([email]);
CREATE UNIQUE INDEX [IX_UserCredential_user_id] ON [UserCredential] ([user_id]);
CREATE UNIQUE INDEX [IX_TraineeFirefighter_user_id] ON [TraineeFirefighter] ([user_id]);
CREATE UNIQUE INDEX [IX_HealthPersonnel_user_id]    ON [HealthPersonnel]    ([user_id]);
CREATE UNIQUE INDEX [IX_MedicalHistory_trainee_firefighter_id] ON [MedicalHistory] ([trainee_firefighter_id]);

-- ============================================================
-- ÍNDICES DE RENDIMIENTO EN CLAVES FORÁNEAS
-- ============================================================

-- User
CREATE INDEX [IX_User_institution_id] ON [User] ([institution_id]);

-- UserRole
CREATE INDEX [IX_UserRole_user_id]  ON [UserRole] ([user_id]);
CREATE INDEX [IX_UserRole_role_id]  ON [UserRole] ([role_id]);
CREATE INDEX [IX_UserRole_assigned_by_user_id] ON [UserRole] ([assigned_by_user_id]) WHERE [assigned_by_user_id] IS NOT NULL;

-- AuthSession
CREATE INDEX [IX_AuthSession_user_id] ON [AuthSession] ([user_id]);

-- AccessAudit
CREATE INDEX [IX_AccessAudit_user_id]         ON [AccessAudit] ([user_id]);
CREATE INDEX [IX_AccessAudit_auth_session_id] ON [AccessAudit] ([auth_session_id]) WHERE [auth_session_id] IS NOT NULL;

-- PasswordResetToken
CREATE INDEX [IX_PasswordResetToken_user_id] ON [PasswordResetToken] ([user_id]);

-- ChangeAudit
CREATE INDEX [IX_ChangeAudit_actor_user_id] ON [ChangeAudit] ([actor_user_id]);

-- TrainingLocation
CREATE INDEX [IX_TrainingLocation_institution_id] ON [TrainingLocation] ([institution_id]);

-- TrainingSession
CREATE INDEX [IX_TrainingSession_institution_id]       ON [TrainingSession] ([institution_id]);
CREATE INDEX [IX_TrainingSession_training_location_id] ON [TrainingSession] ([training_location_id]);
CREATE INDEX [IX_TrainingSession_created_by_user_id]   ON [TrainingSession] ([created_by_user_id]);

-- Invitation
CREATE INDEX [IX_Invitation_sender_user_id]      ON [Invitation] ([sender_user_id]);
CREATE INDEX [IX_Invitation_target_user_id]      ON [Invitation] ([target_user_id])      WHERE [target_user_id]      IS NOT NULL;
CREATE INDEX [IX_Invitation_training_session_id] ON [Invitation] ([training_session_id]) WHERE [training_session_id] IS NOT NULL;
CREATE INDEX [IX_Invitation_target_role_id]      ON [Invitation] ([target_role_id])      WHERE [target_role_id]      IS NOT NULL;

-- SessionParticipant
CREATE INDEX [IX_SessionParticipant_training_session_id]    ON [SessionParticipant] ([training_session_id]);
CREATE INDEX [IX_SessionParticipant_trainee_firefighter_id] ON [SessionParticipant] ([trainee_firefighter_id]);
CREATE INDEX [IX_SessionParticipant_invitation_id]          ON [SessionParticipant] ([invitation_id]) WHERE [invitation_id] IS NOT NULL;

-- MedicalHistory
CREATE INDEX [IX_MedicalHistory_created_by_health_personnel_id] ON [MedicalHistory] ([created_by_health_personnel_id]);

-- VitalSignsMeasurement
CREATE INDEX [IX_VitalSignsMeasurement_session_participant_id]            ON [VitalSignsMeasurement] ([session_participant_id]);
CREATE INDEX [IX_VitalSignsMeasurement_registered_by_health_personnel_id] ON [VitalSignsMeasurement] ([registered_by_health_personnel_id]);

-- BioimpedanceMeasurement
CREATE INDEX [IX_BioimpedanceMeasurement_session_participant_id]            ON [BioimpedanceMeasurement] ([session_participant_id]);
CREATE INDEX [IX_BioimpedanceMeasurement_registered_by_health_personnel_id] ON [BioimpedanceMeasurement] ([registered_by_health_personnel_id]);

-- EnvironmentalData
CREATE INDEX [IX_EnvironmentalData_training_session_id]   ON [EnvironmentalData] ([training_session_id]);
CREATE INDEX [IX_EnvironmentalData_registered_by_user_id] ON [EnvironmentalData] ([registered_by_user_id]);

-- SymptomReport
CREATE INDEX [IX_SymptomReport_session_participant_id] ON [SymptomReport] ([session_participant_id]);
CREATE INDEX [IX_SymptomReport_reported_by_user_id]    ON [SymptomReport] ([reported_by_user_id]);

-- SessionResult
CREATE INDEX [IX_SessionResult_session_participant_id] ON [SessionResult] ([session_participant_id]);
CREATE INDEX [IX_SessionResult_validated_by_user_id]   ON [SessionResult] ([validated_by_user_id]);

-- CriticalAlert
CREATE INDEX [IX_CriticalAlert_session_participant_id]     ON [CriticalAlert] ([session_participant_id]);
CREATE INDEX [IX_CriticalAlert_vital_signs_measurement_id] ON [CriticalAlert] ([vital_signs_measurement_id]) WHERE [vital_signs_measurement_id] IS NOT NULL;
CREATE INDEX [IX_CriticalAlert_symptom_report_id]          ON [CriticalAlert] ([symptom_report_id])          WHERE [symptom_report_id]          IS NOT NULL;
CREATE INDEX [IX_CriticalAlert_environmental_data_id]      ON [CriticalAlert] ([environmental_data_id])      WHERE [environmental_data_id]      IS NOT NULL;

-- UserConsent
CREATE INDEX [IX_UserConsent_user_id]             ON [UserConsent] ([user_id]);
CREATE INDEX [IX_UserConsent_institution_id]      ON [UserConsent] ([institution_id]);
CREATE INDEX [IX_UserConsent_consent_document_id] ON [UserConsent] ([consent_document_id]);

-- DSARRequest
CREATE INDEX [IX_DSARRequest_user_id]            ON [DSARRequest] ([user_id]);
CREATE INDEX [IX_DSARRequest_managed_by_user_id] ON [DSARRequest] ([managed_by_user_id]) WHERE [managed_by_user_id] IS NOT NULL;

-- ============================================================
-- FOREIGN KEYS
-- ============================================================

-- User → TrainingInstitution
ALTER TABLE [User] ADD CONSTRAINT [FK_User_TrainingInstitution]
    FOREIGN KEY ([institution_id]) REFERENCES [TrainingInstitution] ([institution_id]) ON DELETE NO ACTION;

-- UserCredential → User
ALTER TABLE [UserCredential] ADD CONSTRAINT [FK_UserCredential_User]
    FOREIGN KEY ([user_id]) REFERENCES [User] ([user_id]) ON DELETE CASCADE;

-- UserRole → User
ALTER TABLE [UserRole] ADD CONSTRAINT [FK_UserRole_User]
    FOREIGN KEY ([user_id]) REFERENCES [User] ([user_id]) ON DELETE CASCADE;

-- UserRole → Role
ALTER TABLE [UserRole] ADD CONSTRAINT [FK_UserRole_Role]
    FOREIGN KEY ([role_id]) REFERENCES [Role] ([role_id]) ON DELETE NO ACTION;

-- UserRole → User (quien asignó el rol)
ALTER TABLE [UserRole] ADD CONSTRAINT [FK_UserRole_AssignedByUser]
    FOREIGN KEY ([assigned_by_user_id]) REFERENCES [User] ([user_id]) ON DELETE NO ACTION;

-- AuthSession → User
ALTER TABLE [AuthSession] ADD CONSTRAINT [FK_AuthSession_User]
    FOREIGN KEY ([user_id]) REFERENCES [User] ([user_id]) ON DELETE CASCADE;

-- PasswordResetToken → User
ALTER TABLE [PasswordResetToken] ADD CONSTRAINT [FK_PasswordResetToken_User]
    FOREIGN KEY ([user_id]) REFERENCES [User] ([user_id]) ON DELETE CASCADE;

-- AccessAudit → User
ALTER TABLE [AccessAudit] ADD CONSTRAINT [FK_AccessAudit_User]
    FOREIGN KEY ([user_id]) REFERENCES [User] ([user_id]) ON DELETE NO ACTION;

-- AccessAudit → AuthSession
ALTER TABLE [AccessAudit] ADD CONSTRAINT [FK_AccessAudit_AuthSession]
    FOREIGN KEY ([auth_session_id]) REFERENCES [AuthSession] ([auth_session_id]) ON DELETE NO ACTION;

-- ChangeAudit → User
ALTER TABLE [ChangeAudit] ADD CONSTRAINT [FK_ChangeAudit_User]
    FOREIGN KEY ([actor_user_id]) REFERENCES [User] ([user_id]) ON DELETE NO ACTION;

-- TraineeFirefighter → User
ALTER TABLE [TraineeFirefighter] ADD CONSTRAINT [FK_TraineeFirefighter_User]
    FOREIGN KEY ([user_id]) REFERENCES [User] ([user_id]) ON DELETE NO ACTION;

-- HealthPersonnel → User
ALTER TABLE [HealthPersonnel] ADD CONSTRAINT [FK_HealthPersonnel_User]
    FOREIGN KEY ([user_id]) REFERENCES [User] ([user_id]) ON DELETE NO ACTION;

-- TrainingLocation → TrainingInstitution
ALTER TABLE [TrainingLocation] ADD CONSTRAINT [FK_TrainingLocation_TrainingInstitution]
    FOREIGN KEY ([institution_id]) REFERENCES [TrainingInstitution] ([institution_id]) ON DELETE NO ACTION;

-- TrainingSession → TrainingInstitution
ALTER TABLE [TrainingSession] ADD CONSTRAINT [FK_TrainingSession_TrainingInstitution]
    FOREIGN KEY ([institution_id]) REFERENCES [TrainingInstitution] ([institution_id]) ON DELETE NO ACTION;

-- TrainingSession → TrainingLocation
ALTER TABLE [TrainingSession] ADD CONSTRAINT [FK_TrainingSession_TrainingLocation]
    FOREIGN KEY ([training_location_id]) REFERENCES [TrainingLocation] ([training_location_id]) ON DELETE NO ACTION;

-- TrainingSession → User (creador)
ALTER TABLE [TrainingSession] ADD CONSTRAINT [FK_TrainingSession_User]
    FOREIGN KEY ([created_by_user_id]) REFERENCES [User] ([user_id]) ON DELETE NO ACTION;

-- Invitation → User (quien envía)
ALTER TABLE [Invitation] ADD CONSTRAINT [FK_Invitation_SenderUser]
    FOREIGN KEY ([sender_user_id]) REFERENCES [User] ([user_id]) ON DELETE NO ACTION;

-- Invitation → User (destinatario, opcional)
ALTER TABLE [Invitation] ADD CONSTRAINT [FK_Invitation_TargetUser]
    FOREIGN KEY ([target_user_id]) REFERENCES [User] ([user_id]) ON DELETE NO ACTION;

-- Invitation → TrainingSession (opcional)
ALTER TABLE [Invitation] ADD CONSTRAINT [FK_Invitation_TrainingSession]
    FOREIGN KEY ([training_session_id]) REFERENCES [TrainingSession] ([training_session_id]) ON DELETE NO ACTION;

-- Invitation → Role (opcional)
ALTER TABLE [Invitation] ADD CONSTRAINT [FK_Invitation_Role]
    FOREIGN KEY ([target_role_id]) REFERENCES [Role] ([role_id]) ON DELETE NO ACTION;

-- SessionParticipant → TrainingSession
ALTER TABLE [SessionParticipant] ADD CONSTRAINT [FK_SessionParticipant_TrainingSession]
    FOREIGN KEY ([training_session_id]) REFERENCES [TrainingSession] ([training_session_id]) ON DELETE NO ACTION;

-- SessionParticipant → TraineeFirefighter
ALTER TABLE [SessionParticipant] ADD CONSTRAINT [FK_SessionParticipant_TraineeFirefighter]
    FOREIGN KEY ([trainee_firefighter_id]) REFERENCES [TraineeFirefighter] ([trainee_firefighter_id]) ON DELETE NO ACTION;

-- SessionParticipant → Invitation (opcional)
ALTER TABLE [SessionParticipant] ADD CONSTRAINT [FK_SessionParticipant_Invitation]
    FOREIGN KEY ([invitation_id]) REFERENCES [Invitation] ([invitation_id]) ON DELETE NO ACTION;

-- MedicalHistory → TraineeFirefighter
ALTER TABLE [MedicalHistory] ADD CONSTRAINT [FK_MedicalHistory_TraineeFirefighter]
    FOREIGN KEY ([trainee_firefighter_id]) REFERENCES [TraineeFirefighter] ([trainee_firefighter_id]) ON DELETE NO ACTION;

-- MedicalHistory → HealthPersonnel
ALTER TABLE [MedicalHistory] ADD CONSTRAINT [FK_MedicalHistory_HealthPersonnel]
    FOREIGN KEY ([created_by_health_personnel_id]) REFERENCES [HealthPersonnel] ([health_personnel_id]) ON DELETE NO ACTION;

-- VitalSignsMeasurement → SessionParticipant
ALTER TABLE [VitalSignsMeasurement] ADD CONSTRAINT [FK_VitalSignsMeasurement_SessionParticipant]
    FOREIGN KEY ([session_participant_id]) REFERENCES [SessionParticipant] ([session_participant_id]) ON DELETE NO ACTION;

-- VitalSignsMeasurement → HealthPersonnel
ALTER TABLE [VitalSignsMeasurement] ADD CONSTRAINT [FK_VitalSignsMeasurement_HealthPersonnel]
    FOREIGN KEY ([registered_by_health_personnel_id]) REFERENCES [HealthPersonnel] ([health_personnel_id]) ON DELETE NO ACTION;

-- BioimpedanceMeasurement → SessionParticipant
ALTER TABLE [BioimpedanceMeasurement] ADD CONSTRAINT [FK_BioimpedanceMeasurement_SessionParticipant]
    FOREIGN KEY ([session_participant_id]) REFERENCES [SessionParticipant] ([session_participant_id]) ON DELETE NO ACTION;

-- BioimpedanceMeasurement → HealthPersonnel
ALTER TABLE [BioimpedanceMeasurement] ADD CONSTRAINT [FK_BioimpedanceMeasurement_HealthPersonnel]
    FOREIGN KEY ([registered_by_health_personnel_id]) REFERENCES [HealthPersonnel] ([health_personnel_id]) ON DELETE NO ACTION;

-- EnvironmentalData → TrainingSession
ALTER TABLE [EnvironmentalData] ADD CONSTRAINT [FK_EnvironmentalData_TrainingSession]
    FOREIGN KEY ([training_session_id]) REFERENCES [TrainingSession] ([training_session_id]) ON DELETE NO ACTION;

-- EnvironmentalData → User (quien registró)
ALTER TABLE [EnvironmentalData] ADD CONSTRAINT [FK_EnvironmentalData_User]
    FOREIGN KEY ([registered_by_user_id]) REFERENCES [User] ([user_id]) ON DELETE NO ACTION;

-- SymptomReport → SessionParticipant
ALTER TABLE [SymptomReport] ADD CONSTRAINT [FK_SymptomReport_SessionParticipant]
    FOREIGN KEY ([session_participant_id]) REFERENCES [SessionParticipant] ([session_participant_id]) ON DELETE NO ACTION;

-- SymptomReport → User (quien reporta)
ALTER TABLE [SymptomReport] ADD CONSTRAINT [FK_SymptomReport_User]
    FOREIGN KEY ([reported_by_user_id]) REFERENCES [User] ([user_id]) ON DELETE NO ACTION;

-- SessionResult → SessionParticipant
ALTER TABLE [SessionResult] ADD CONSTRAINT [FK_SessionResult_SessionParticipant]
    FOREIGN KEY ([session_participant_id]) REFERENCES [SessionParticipant] ([session_participant_id]) ON DELETE NO ACTION;

-- SessionResult → User (quien valida)
ALTER TABLE [SessionResult] ADD CONSTRAINT [FK_SessionResult_User]
    FOREIGN KEY ([validated_by_user_id]) REFERENCES [User] ([user_id]) ON DELETE NO ACTION;

-- CriticalAlert → SessionParticipant
ALTER TABLE [CriticalAlert] ADD CONSTRAINT [FK_CriticalAlert_SessionParticipant]
    FOREIGN KEY ([session_participant_id]) REFERENCES [SessionParticipant] ([session_participant_id]) ON DELETE NO ACTION;

-- CriticalAlert → VitalSignsMeasurement (opcional)
ALTER TABLE [CriticalAlert] ADD CONSTRAINT [FK_CriticalAlert_VitalSignsMeasurement]
    FOREIGN KEY ([vital_signs_measurement_id]) REFERENCES [VitalSignsMeasurement] ([vital_signs_measurement_id]) ON DELETE NO ACTION;

-- CriticalAlert → SymptomReport (opcional)
ALTER TABLE [CriticalAlert] ADD CONSTRAINT [FK_CriticalAlert_SymptomReport]
    FOREIGN KEY ([symptom_report_id]) REFERENCES [SymptomReport] ([symptom_report_id]) ON DELETE NO ACTION;

-- CriticalAlert → EnvironmentalData (opcional)
ALTER TABLE [CriticalAlert] ADD CONSTRAINT [FK_CriticalAlert_EnvironmentalData]
    FOREIGN KEY ([environmental_data_id]) REFERENCES [EnvironmentalData] ([environmental_data_id]) ON DELETE NO ACTION;

-- CriticalAlert → User (quien atendió, opcional)
ALTER TABLE [CriticalAlert] ADD CONSTRAINT [FK_CriticalAlert_AttendedByUser]
    FOREIGN KEY ([attended_by_user_id]) REFERENCES [User] ([user_id]) ON DELETE NO ACTION;

-- UserConsent → User
ALTER TABLE [UserConsent] ADD CONSTRAINT [FK_UserConsent_User]
    FOREIGN KEY ([user_id]) REFERENCES [User] ([user_id]) ON DELETE NO ACTION;

-- UserConsent → TrainingInstitution
ALTER TABLE [UserConsent] ADD CONSTRAINT [FK_UserConsent_TrainingInstitution]
    FOREIGN KEY ([institution_id]) REFERENCES [TrainingInstitution] ([institution_id]) ON DELETE NO ACTION;

-- UserConsent → ConsentDocument
ALTER TABLE [UserConsent] ADD CONSTRAINT [FK_UserConsent_ConsentDocument]
    FOREIGN KEY ([consent_document_id]) REFERENCES [ConsentDocument] ([consent_document_id]) ON DELETE NO ACTION;

-- DSARRequest → User (solicitante)
ALTER TABLE [DSARRequest] ADD CONSTRAINT [FK_DSARRequest_User]
    FOREIGN KEY ([user_id]) REFERENCES [User] ([user_id]) ON DELETE NO ACTION;

-- DSARRequest → User (quien gestiona, opcional)
ALTER TABLE [DSARRequest] ADD CONSTRAINT [FK_DSARRequest_ManagedByUser]
    FOREIGN KEY ([managed_by_user_id]) REFERENCES [User] ([user_id]) ON DELETE NO ACTION;

-- ============================================================
-- REGISTRO DE MIGRACIÓN EF CORE
-- ============================================================

INSERT INTO [__EFMigrationsHistory] ([MigrationId], [ProductVersion])
VALUES
    (N'20260623233804_InitialCreate', N'10.0.8'),
    (N'20260730023207_AddHealthPersonnelStatusResearchMarkersAndAuditResource', N'10.0.8'),
    (N'20260802052343_AddVitalSignsPracticeContextFields', N'10.0.8'),
    (N'20260808182911_WidenPercentageColumnsPrecision', N'10.0.8');

COMMIT;
GO
