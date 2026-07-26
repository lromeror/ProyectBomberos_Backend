-- ============================================================================
--  DATOS DE PRUEBA COMPLETOS — entorno local (bd_bomberos en SQLEXPRESS)
-- ============================================================================
--
--  Genera datos suficientes para ejercitar TODAS las pantallas: sesiones en los
--  cuatro estados y en volumen (para paginar), historial de signos vitales para
--  las gráficas de progreso y el diagrama corporal, invitaciones pendientes,
--  participantes, datos ambientales e historiales médicos.
--
--  Requisito: haber corrido antes `seed_local_users.sql`.
--
--  ⚠ CODIFICACIÓN — IMPORTANTE
--  Este archivo está en UTF-8. `sqlcmd` lee los archivos de entrada con la
--  codepage ANSI del sistema salvo que se le indique lo contrario, y sin el flag
--  correcto las tildes se guardan corruptas («Capacitación» -> «CapacitaciÃ³n»).
--  Ejecuta SIEMPRE con -f 65001:
--
--    sqlcmd -S "localhost\SQLEXPRESS" -E -C -f 65001 -i seed_local_demo_data.sql
--
--  El script además REPARA el texto ya corrupto de ejecuciones anteriores.
--
--  IDEMPOTENTE: marca lo suyo con el prefijo 'DEMO-' y lo borra antes de
--  reinsertarlo; los usuarios y centros se actualizan en vez de duplicarse.
--
--  Contraseña de todos los usuarios: Smab2026!
-- ============================================================================

USE bd_bomberos;
GO

SET QUOTED_IDENTIFIER ON;
SET ANSI_NULLS ON;
SET NOCOUNT ON;
GO

DECLARE @Now           DATETIME2 = GETUTCDATE();
DECLARE @PasswordHash  NVARCHAR(200) = '$2a$12$ogXeZJzvmBlQcTPXRnJMVOEWHN5920sKYvZKPUb7I0W97HrBWgtZO';
DECLARE @InstitutionId UNIQUEIDENTIFIER;
DECLARE @AdminId       UNIQUEIDENTIFIER;

SELECT @InstitutionId = institution_id FROM TrainingInstitution WHERE acronym = 'SMAB';
SELECT @AdminId       = user_id        FROM [User] WHERE email = 'admin@smab.app';

IF @InstitutionId IS NULL OR @AdminId IS NULL
BEGIN
    RAISERROR('Falta la institución SMAB o admin@smab.app. Corre primero seed_local_users.sql.', 16, 1);
    RETURN;
END

-- Detecta si el archivo se está leyendo con la codepage equivocada. Si esto salta,
-- relanza con -f 65001 en vez de dejar datos corruptos en la base.
IF NCHAR(243) <> N'ó'
BEGIN
    RAISERROR('Codificación incorrecta: relanza con  sqlcmd -f 65001 -i seed_local_demo_data.sql', 16, 1);
    RETURN;
END

-- ============================================================================
--  0. REPARACIÓN del texto corrupto por ejecuciones previas sin -f 65001
-- ============================================================================

UPDATE TrainingInstitution
   SET name = N'Sistema de Monitoreo y Análisis de Bomberos'
 WHERE acronym = 'SMAB' AND name <> N'Sistema de Monitoreo y Análisis de Bomberos';

UPDATE hp
   SET profession = N'Médico'
  FROM HealthPersonnel hp
 WHERE hp.profession LIKE '%dico' AND hp.profession <> N'Médico';

UPDATE hp
   SET specialty = N'Medicina General'
  FROM HealthPersonnel hp
 WHERE hp.specialty LIKE 'Medicina%' AND hp.specialty <> N'Medicina General';

PRINT 'Reparación de texto previo: OK';

-- ============================================================================
--  1. LIMPIEZA de datos DEMO previos (en orden de dependencias)
-- ============================================================================

DECLARE @DemoSessions TABLE (id UNIQUEIDENTIFIER);
INSERT INTO @DemoSessions
SELECT training_session_id FROM TrainingSession WHERE session_code LIKE 'DEMO-%';

DECLARE @DemoParticipants TABLE (id UNIQUEIDENTIFIER);
INSERT INTO @DemoParticipants
SELECT session_participant_id FROM SessionParticipant
WHERE training_session_id IN (SELECT id FROM @DemoSessions);

DELETE FROM VitalSignsMeasurement   WHERE session_participant_id IN (SELECT id FROM @DemoParticipants);
DELETE FROM BioimpedanceMeasurement WHERE session_participant_id IN (SELECT id FROM @DemoParticipants);
DELETE FROM SymptomReport           WHERE session_participant_id IN (SELECT id FROM @DemoParticipants);
DELETE FROM SessionResult           WHERE session_participant_id IN (SELECT id FROM @DemoParticipants);
DELETE FROM CriticalAlert           WHERE session_participant_id IN (SELECT id FROM @DemoParticipants);
DELETE FROM SessionParticipant      WHERE training_session_id     IN (SELECT id FROM @DemoSessions);
DELETE FROM EnvironmentalData       WHERE training_session_id     IN (SELECT id FROM @DemoSessions);
DELETE FROM Invitation              WHERE training_session_id     IN (SELECT id FROM @DemoSessions);
DELETE FROM TrainingSession         WHERE training_session_id     IN (SELECT id FROM @DemoSessions);

PRINT 'Limpieza de datos DEMO previos: OK';

-- ============================================================================
--  2. USUARIOS (inserta si faltan, ACTUALIZA el nombre si ya existen)
--     La clave es el correo, que es ASCII y por tanto nunca se corrompe.
-- ============================================================================

DECLARE @NewUsers TABLE (
    Email NVARCHAR(200), FirstName NVARCHAR(100), LastName NVARCHAR(100),
    RoleCode NVARCHAR(50), Phone NVARCHAR(30),
    Profession NVARCHAR(100), Specialty NVARCHAR(100),
    ApplicantCode NVARCHAR(50), Sex NVARCHAR(20), BloodType NVARCHAR(10)
);

INSERT INTO @NewUsers VALUES
 ('bombero2@smab.app', N'Marco',  N'Torres',   'FIREFIGHTER_TRAINEE','+593 98-200-0002',NULL,NULL,'BOM-2026-002','M','A+'),
 ('bombero3@smab.app', N'Sara',   N'Vega',     'FIREFIGHTER_TRAINEE','+593 98-200-0003',NULL,NULL,'BOM-2026-003','F','B+'),
 ('bombero4@smab.app', N'Luis',   N'Paredes',  'FIREFIGHTER_TRAINEE','+593 98-200-0004',NULL,NULL,'BOM-2026-004','M','AB+'),
 ('bombero5@smab.app', N'Diego',  N'Carrillo', 'FIREFIGHTER_TRAINEE','+593 98-200-0005',NULL,NULL,'BOM-2026-005','M','O-'),
 ('bombero6@smab.app', N'Andrea', N'Suárez',   'FIREFIGHTER_TRAINEE','+593 98-200-0006',NULL,NULL,'BOM-2026-006','F','O+'),
 ('bombero7@smab.app', N'Pablo',  N'Jiménez',  'FIREFIGHTER_TRAINEE','+593 98-200-0007',NULL,NULL,'BOM-2026-007','M','A-'),
 ('bombero8@smab.app', N'Karla',  N'Moreno',   'FIREFIGHTER_TRAINEE','+593 98-200-0008',NULL,NULL,'BOM-2026-008','F','B-'),
 ('bombero9@smab.app', N'Iván',   N'Salazar',  'FIREFIGHTER_TRAINEE','+593 98-200-0009',NULL,NULL,'BOM-2026-009','M','O+'),
 ('bombero10@smab.app',N'Mónica', N'Cedeño',   'FIREFIGHTER_TRAINEE','+593 98-200-0010',NULL,NULL,'BOM-2026-010','F','AB-'),
 ('bombero11@smab.app',N'Esteban',N'Vaca',     'FIREFIGHTER_TRAINEE','+593 98-200-0011',NULL,NULL,'BOM-2026-011','M','A+'),
 ('bombero12@smab.app',N'Lucía',  N'Andrade',  'FIREFIGHTER_TRAINEE','+593 98-200-0012',NULL,NULL,'BOM-2026-012','F','O+'),
 ('enfermera@smab.app',    N'Valeria',N'Castro', 'MEDICAL','+593 99-100-0002',N'Enfermero',     N'Urgencias',            NULL,NULL,NULL),
 ('nutricionista@smab.app',N'Andrea', N'Rivas',  'MEDICAL','+593 99-100-0003',N'Nutricionista', N'Deportiva',            NULL,NULL,NULL),
 ('medico2@smab.app',      N'Daniel', N'Ortega', 'MEDICAL','+593 99-100-0004',N'Médico',        N'Medicina General',     NULL,NULL,NULL),
 ('enfermero2@smab.app',   N'Jorge',  N'Bravo',  'MEDICAL','+593 99-100-0005',N'Enfermero',     N'Cuidados intensivos',  NULL,NULL,NULL),
 ('capacitador2@smab.app', N'Elena',  N'Ruiz',   'CAPACITATOR','+593 97-300-0002',NULL,NULL,NULL,NULL,NULL),
 ('capacitador3@smab.app', N'Óscar',  N'Naranjo','CAPACITATOR','+593 97-300-0003',NULL,NULL,NULL,NULL,NULL);

-- Repara nombres de usuarios ya existentes (arregla las tildes corruptas).
UPDATE u
   SET first_name = n.FirstName,
       last_name  = n.LastName,
       phone      = n.Phone
  FROM [User] u
  JOIN @NewUsers n ON n.Email = u.email;

UPDATE hp
   SET profession = n.Profession,
       specialty  = n.Specialty
  FROM HealthPersonnel hp
  JOIN [User] u      ON u.user_id = hp.user_id
  JOIN @NewUsers n   ON n.Email = u.email
 WHERE n.Profession IS NOT NULL;

DECLARE @Email NVARCHAR(200), @First NVARCHAR(100), @Last NVARCHAR(100), @RoleCode NVARCHAR(50),
        @Phone NVARCHAR(30), @Profession NVARCHAR(100), @Specialty NVARCHAR(100),
        @AppCode NVARCHAR(50), @Sex NVARCHAR(20), @Blood NVARCHAR(10);
DECLARE @UserId UNIQUEIDENTIFIER, @RoleId UNIQUEIDENTIFIER;

DECLARE userCur CURSOR FOR SELECT * FROM @NewUsers;
OPEN userCur;
FETCH NEXT FROM userCur INTO @Email, @First, @Last, @RoleCode, @Phone, @Profession, @Specialty, @AppCode, @Sex, @Blood;

WHILE @@FETCH_STATUS = 0
BEGIN
    IF NOT EXISTS (SELECT 1 FROM [User] WHERE email = @Email)
    BEGIN
        SET @UserId = NEWID();

        INSERT INTO [User] (user_id, institution_id, email, first_name, last_name, phone, account_status, created_at)
        VALUES (@UserId, @InstitutionId, @Email, @First, @Last, @Phone, 'active', DATEADD(DAY, -120, @Now));

        INSERT INTO UserCredential (user_credential_id, user_id, password_hash, hash_algorithm, mfa_enabled, failed_attempts, last_password_change_at)
        VALUES (NEWID(), @UserId, @PasswordHash, 'bcrypt', 0, 0, @Now);

        SELECT @RoleId = role_id FROM Role WHERE code = @RoleCode;
        INSERT INTO UserRole (user_role_id, user_id, role_id, start_date, is_active)
        VALUES (NEWID(), @UserId, @RoleId, DATEADD(DAY, -120, @Now), 1);

        IF @RoleCode = 'MEDICAL'
            INSERT INTO HealthPersonnel (health_personnel_id, user_id, profession, specialty, license_number, can_approve_discharges)
            VALUES (NEWID(), @UserId, @Profession, @Specialty,
                    CONCAT('LIC-', RIGHT('000' + CAST(ABS(CHECKSUM(NEWID())) % 999 AS NVARCHAR), 3)),
                    CASE WHEN @Profession = N'Médico' THEN 1 ELSE 0 END);

        IF @RoleCode = 'FIREFIGHTER_TRAINEE'
            INSERT INTO TraineeFirefighter (trainee_firefighter_id, user_id, applicant_code, birth_date, sex, blood_type,
                                            emergency_contact_name, emergency_contact_phone, training_status)
            VALUES (NEWID(), @UserId, @AppCode,
                    DATEADD(YEAR, -(22 + ABS(CHECKSUM(NEWID())) % 15), CAST(@Now AS DATE)),
                    @Sex, @Blood, CONCAT(N'Contacto de ', @First), '+593 99-000-0000', 'Active');
    END

    FETCH NEXT FROM userCur INTO @Email, @First, @Last, @RoleCode, @Phone, @Profession, @Specialty, @AppCode, @Sex, @Blood;
END
CLOSE userCur; DEALLOCATE userCur;

PRINT 'Usuarios: OK';

-- ============================================================================
--  3. CENTROS DE ENTRENAMIENTO
--     Se emparejan por prefijo ASCII para poder reparar nombres con tilde.
-- ============================================================================

MERGE TrainingLocation AS tgt
USING (VALUES
    ('Centro de Entrenamiento Alpha%', N'Centro de Entrenamiento Alpha', 'Outdoor', N'Av. Bomberos 4500, Quito',   30),
    ('Casa de Ataque Beta%',           N'Casa de Ataque Beta',           'Indoor',  N'Calle Rocafuerte 220, Quito',18),
    ('Casa de Progres%',               N'Casa de Progresión Gamma',      'Indoor',  N'Av. Amazonas 1180, Quito',   24)
) AS src (MatchPattern, Name, LocType, Addr, Cap)
   ON tgt.name LIKE src.MatchPattern
WHEN MATCHED THEN
    UPDATE SET name = src.Name, location_type = src.LocType, address = src.Addr, max_capacity = src.Cap
WHEN NOT MATCHED BY TARGET THEN
    INSERT (training_location_id, institution_id, name, location_type, address, max_capacity)
    VALUES (NEWID(), @InstitutionId, src.Name, src.LocType, src.Addr, src.Cap);

DECLARE @LocAlpha UNIQUEIDENTIFIER, @LocBeta UNIQUEIDENTIFIER, @LocGamma UNIQUEIDENTIFIER;
SELECT @LocAlpha = training_location_id FROM TrainingLocation WHERE name = N'Centro de Entrenamiento Alpha';
SELECT @LocBeta  = training_location_id FROM TrainingLocation WHERE name = N'Casa de Ataque Beta';
SELECT @LocGamma = training_location_id FROM TrainingLocation WHERE name = N'Casa de Progresión Gamma';

PRINT 'Centros de entrenamiento: OK';

-- ============================================================================
--  4. SESIONES — 26 en total, repartidas en los cuatro estados
-- ============================================================================

DECLARE @Sessions TABLE (
    Code NVARCHAR(50), Title NVARCHAR(200), Descr NVARCHAR(MAX),
    Status NVARCHAR(50), StartAt DATETIME2, DurH INT, Capacity INT, LocId UNIQUEIDENTIFIER
);

INSERT INTO @Sessions VALUES
 -- ── En curso (3) ──
 ('DEMO-A1', N'Capacitación A1 — Evaluación Física',     N'Punto de quema: Casa de ataque. Número de quemas: 2.',    'InProgress', DATEADD(HOUR,-2,@Now), 4, 12, @LocBeta),
 ('DEMO-A2', N'Capacitación A2 — Simulacro Interior',    N'Punto de quema: Casa de progresión. Número de quemas: 3.','InProgress', DATEADD(HOUR,-1,@Now), 5, 10, @LocGamma),
 ('DEMO-A3', N'Capacitación A3 — Chequeo Rutinario',     N'Punto de quema: Casa COEPT. Número de quemas: 1.',        'InProgress', DATEADD(HOUR,-3,@Now), 3,  8, @LocAlpha),

 -- ── Planificadas (6) ──
 ('DEMO-P1', N'Capacitación P1 — Chequeo Rutinario',     N'Punto de quema: Casa COEPT. Número de quemas: 1.',        'Scheduled', DATEADD(DAY, 1,@Now), 3,  8, @LocAlpha),
 ('DEMO-P2', N'Capacitación P2 — Evaluación Térmica',    N'Punto de quema: Casa de ataque. Número de quemas: 2.',    'Scheduled', DATEADD(DAY, 3,@Now), 4, 14, @LocBeta),
 ('DEMO-P3', N'Capacitación P3 — Rescate en Altura',     N'Punto de quema: Casa de progresión. Número de quemas: 2.','Scheduled', DATEADD(DAY, 6,@Now), 4,  9, @LocGamma),
 ('DEMO-P4', N'Capacitación P4 — Manejo de Manguera',    N'Punto de quema: Casa COEPT. Número de quemas: 3.',        'Scheduled', DATEADD(DAY,10,@Now), 4, 16, @LocAlpha),
 ('DEMO-P5', N'Capacitación P5 — Evaluación Post-Rescate',N'Punto de quema: Casa de ataque. Número de quemas: 2.',   'Scheduled', DATEADD(DAY,14,@Now), 4, 11, @LocBeta),
 ('DEMO-P6', N'Capacitación P6 — Ventilación Táctica',   N'Punto de quema: Casa de progresión. Número de quemas: 2.','Scheduled', DATEADD(DAY,18,@Now), 4, 13, @LocGamma),

 -- ── Finalizadas (12) — historial largo para las gráficas de progreso ──
 ('DEMO-F01',N'Capacitación F01 — Evaluación Inicial',   N'Sesión completada sin novedades.',        'Finished', DATEADD(DAY,-150,@Now), 4, 12, @LocAlpha),
 ('DEMO-F02',N'Capacitación F02 — Chequeo Rutinario',    N'Sesión completada sin novedades.',        'Finished', DATEADD(DAY,-135,@Now), 3, 10, @LocBeta),
 ('DEMO-F03',N'Capacitación F03 — Evaluación Física',    N'Sesión completada.',                      'Finished', DATEADD(DAY,-120,@Now), 4, 14, @LocAlpha),
 ('DEMO-F04',N'Capacitación F04 — Simulacro Casa Fuego', N'Sesión completada con alta exigencia.',   'Finished', DATEADD(DAY,-105,@Now), 5, 15, @LocGamma),
 ('DEMO-F05',N'Capacitación F05 — Evaluación Térmica',   N'Sesión completada.',                      'Finished', DATEADD(DAY, -90,@Now), 4, 11, @LocBeta),
 ('DEMO-F06',N'Capacitación F06 — Rescate Vehicular',    N'Sesión completada.',                      'Finished', DATEADD(DAY, -75,@Now), 4, 13, @LocGamma),
 ('DEMO-F07',N'Capacitación F07 — Manejo de Manguera',   N'Sesión completada.',                      'Finished', DATEADD(DAY, -60,@Now), 3, 12, @LocAlpha),
 ('DEMO-F08',N'Capacitación F08 — Evaluación Post-Rescate',N'Sesión completada.',                    'Finished', DATEADD(DAY, -45,@Now), 4, 10, @LocBeta),
 ('DEMO-F09',N'Capacitación F09 — Simulacro Interior',   N'Sesión completada.',                      'Finished', DATEADD(DAY, -30,@Now), 5, 15, @LocGamma),
 ('DEMO-F10',N'Capacitación F10 — Evaluación Física',    N'Sesión completada.',                      'Finished', DATEADD(DAY, -20,@Now), 4, 13, @LocAlpha),
 ('DEMO-F11',N'Capacitación F11 — Ventilación Táctica',  N'Sesión completada.',                      'Finished', DATEADD(DAY, -12,@Now), 4, 12, @LocGamma),
 ('DEMO-F12',N'Capacitación F12 — Chequeo Rutinario',    N'Sesión completada ayer.',                 'Finished', DATEADD(DAY,  -4,@Now), 3, 11, @LocBeta),

 -- ── Canceladas (5) ──
 ('DEMO-C1', N'Capacitación C1 — Evaluación Inicial',    N'Cancelada por condiciones climáticas.',   'Cancelled', DATEADD(DAY,-100,@Now), 4,  6, @LocAlpha),
 ('DEMO-C2', N'Capacitación C2 — Simulacro Nocturno',    N'Cancelada por falta de personal médico.', 'Cancelled', DATEADD(DAY, -70,@Now), 4,  9, @LocGamma),
 ('DEMO-C3', N'Capacitación C3 — Rescate en Altura',     N'Cancelada por mantenimiento del centro.', 'Cancelled', DATEADD(DAY, -40,@Now), 4,  8, @LocBeta),
 ('DEMO-C4', N'Capacitación C4 — Evaluación Térmica',    N'Cancelada por alerta de calidad del aire.','Cancelled',DATEADD(DAY, -22,@Now), 4, 10, @LocAlpha),
 ('DEMO-C5', N'Capacitación C5 — Manejo de Manguera',    N'Cancelada a petición de la jefatura.',    'Cancelled', DATEADD(DAY,  -6,@Now), 3,  7, @LocGamma);

INSERT INTO TrainingSession (training_session_id, institution_id, training_location_id, created_by_user_id,
                             session_code, title, description, status,
                             scheduled_start, scheduled_end, actual_start, actual_end, planned_capacity)
SELECT NEWID(), @InstitutionId, s.LocId, @AdminId, s.Code, s.Title, s.Descr, s.Status,
       s.StartAt, DATEADD(HOUR, s.DurH, s.StartAt),
       CASE WHEN s.Status IN ('InProgress','Finished') THEN s.StartAt END,
       CASE WHEN s.Status = 'Finished' THEN DATEADD(HOUR, s.DurH, s.StartAt) END,
       s.Capacity
FROM @Sessions s;

PRINT 'Sesiones: OK (26)';

-- ============================================================================
--  5. INVITACIONES
--     El detalle de sesión deriva los INSTRUCTORES de las invitaciones vigentes,
--     así que estas alimentan también esa sección.
-- ============================================================================

DECLARE @Inv TABLE (SessionCode NVARCHAR(50), Email NVARCHAR(200), Status NVARCHAR(50), HoursAgo INT);
INSERT INTO @Inv VALUES
 -- Pendientes de personal de salud -> cola de validación
 ('DEMO-P1','enfermera@smab.app',    'Pending',  2),
 ('DEMO-P1','nutricionista@smab.app','Pending',  4),
 ('DEMO-P2','medico2@smab.app',      'Pending',  5),
 ('DEMO-P2','enfermero2@smab.app',   'Pending',  7),
 ('DEMO-P3','enfermera@smab.app',    'Pending',  9),
 ('DEMO-P4','medico2@smab.app',      'Pending', 11),
 ('DEMO-P5','nutricionista@smab.app','Pending', 13),
 ('DEMO-P6','enfermero2@smab.app',   'Pending', 15),
 -- Instructores aceptados
 ('DEMO-A1','medico@smab.app',       'Accepted', 30),
 ('DEMO-A1','enfermera@smab.app',    'Accepted', 30),
 ('DEMO-A2','medico2@smab.app',      'Accepted', 28),
 ('DEMO-A3','nutricionista@smab.app','Accepted', 26),
 ('DEMO-F09','medico@smab.app',      'Accepted', 720),
 ('DEMO-F12','nutricionista@smab.app','Accepted',100),
 -- Rechazadas -> actividad reciente
 ('DEMO-C1','enfermero2@smab.app',   'Rejected', 600),
 ('DEMO-C2','medico2@smab.app',      'Rejected', 300),
 ('DEMO-C4','enfermera@smab.app',    'Rejected', 150),
 -- Pendientes de aspirantes -> tarjeta de invitación en su panel
 ('DEMO-P1','bombero@smab.app',      'Pending',  3),
 ('DEMO-P2','bombero2@smab.app',     'Pending',  5),
 ('DEMO-P3','bombero3@smab.app',     'Pending',  6),
 ('DEMO-P4','bombero4@smab.app',     'Pending',  8);

INSERT INTO Invitation (invitation_id, sender_user_id, target_user_id, training_session_id, target_email,
                        invitation_token_hash, status, expires_at, responded_at, created_at)
SELECT NEWID(), @AdminId, u.user_id, ts.training_session_id, i.Email,
       REPLACE(CAST(NEWID() AS NVARCHAR(50)), '-', ''),
       i.Status, DATEADD(DAY, 7, @Now),
       CASE WHEN i.Status <> 'Pending' THEN DATEADD(HOUR, -i.HoursAgo + 1, @Now) END,
       DATEADD(HOUR, -i.HoursAgo, @Now)
FROM @Inv i
JOIN TrainingSession ts ON ts.session_code = i.SessionCode
LEFT JOIN [User] u      ON u.email = i.Email;

PRINT 'Invitaciones: OK (21)';

-- ============================================================================
--  6. PARTICIPANTES (todos los aspirantes en sesiones finalizadas y en curso)
-- ============================================================================

INSERT INTO SessionParticipant (session_participant_id, training_session_id, trainee_firefighter_id,
                                participation_status, attendance_confirmed, check_in_at, observations)
SELECT NEWID(), ts.training_session_id, tf.trainee_firefighter_id,
       CASE WHEN ts.status = 'Finished' THEN 'Attended' ELSE 'Registered' END,
       1,
       ts.scheduled_start,
       NULL
FROM TrainingSession ts
CROSS JOIN TraineeFirefighter tf
WHERE ts.session_code LIKE 'DEMO-%'
  AND ts.status IN ('Finished','InProgress');

PRINT 'Participantes: OK';

-- ============================================================================
--  7. SIGNOS VITALES
--     Alimenta las gráficas de progreso Y el diagrama corporal (esqueleto),
--     que lee la ÚLTIMA medición de cada participante.
--     OJO: spo2 y temperature_c son decimal(4,2) -> máximo 99.99.
-- ============================================================================

DECLARE @MedicoHpId UNIQUEIDENTIFIER;
SELECT TOP 1 @MedicoHpId = hp.health_personnel_id
FROM HealthPersonnel hp JOIN [User] u ON u.user_id = hp.user_id
WHERE u.email = 'medico@smab.app';

DECLARE @Offsets TABLE (Slot INT, MinsAfterStart INT, HrBase INT, SysBase INT, DiaBase INT, TempBase DECIMAL(4,2), SpoBase INT);
INSERT INTO @Offsets VALUES
 (1,  0,  72, 118, 76, 36.60, 97),   -- basal        -> SpO2 97..99
 (2, 90, 128, 142, 88, 37.90, 93),   -- esfuerzo     -> 93..95
 (3,210,  84, 124, 79, 37.10, 95);   -- recuperación -> 95..97

INSERT INTO VitalSignsMeasurement (vital_signs_measurement_id, session_participant_id,
                                   registered_by_health_personnel_id,
                                   heart_rate, systolic_pressure, diastolic_pressure,
                                   temperature_c, spo2, taken_at)
SELECT NEWID(), sp.session_participant_id, @MedicoHpId,
       o.HrBase   + (ABS(CHECKSUM(NEWID())) % 13) - 6,
       o.SysBase  + (ABS(CHECKSUM(NEWID())) % 15) - 7,
       o.DiaBase  + (ABS(CHECKSUM(NEWID())) % 11) - 5,
       o.TempBase + ((ABS(CHECKSUM(NEWID())) % 9) - 4) / 10.0,
       o.SpoBase  + (ABS(CHECKSUM(NEWID())) % 3),
       DATEADD(MINUTE, o.MinsAfterStart, ts.scheduled_start)
FROM SessionParticipant sp
JOIN TrainingSession ts ON ts.training_session_id = sp.training_session_id
CROSS JOIN @Offsets o
WHERE ts.session_code LIKE 'DEMO-%' AND ts.status = 'Finished';

-- Sesiones en curso: solo la toma basal.
INSERT INTO VitalSignsMeasurement (vital_signs_measurement_id, session_participant_id,
                                   registered_by_health_personnel_id,
                                   heart_rate, systolic_pressure, diastolic_pressure,
                                   temperature_c, spo2, taken_at)
SELECT NEWID(), sp.session_participant_id, @MedicoHpId,
       70 + (ABS(CHECKSUM(NEWID())) % 15),
       115 + (ABS(CHECKSUM(NEWID())) % 12),
       74 + (ABS(CHECKSUM(NEWID())) % 9),
       36.4 + ((ABS(CHECKSUM(NEWID())) % 8)) / 10.0,
       96 + (ABS(CHECKSUM(NEWID())) % 3),
       DATEADD(MINUTE, 10, ts.scheduled_start)
FROM SessionParticipant sp
JOIN TrainingSession ts ON ts.training_session_id = sp.training_session_id
WHERE ts.session_code LIKE 'DEMO-%' AND ts.status = 'InProgress';

PRINT 'Signos vitales: OK';

-- ============================================================================
--  8. DATOS AMBIENTALES
-- ============================================================================

INSERT INTO EnvironmentalData (environmental_data_id, training_session_id, registered_by_user_id,
                               temperature_c, humidity_pct, co_ppm, heat_stress_index, measured_at)
SELECT NEWID(), ts.training_session_id, @AdminId,
       21 + (ABS(CHECKSUM(NEWID())) % 12),
       45 + (ABS(CHECKSUM(NEWID())) % 35),
       (ABS(CHECKSUM(NEWID())) % 25),
       24 + (ABS(CHECKSUM(NEWID())) % 10),
       ts.scheduled_start
FROM TrainingSession ts
WHERE ts.session_code LIKE 'DEMO-%' AND ts.status IN ('Finished','InProgress');

PRINT 'Datos ambientales: OK';

-- ============================================================================
--  9. HISTORIALES MÉDICOS
-- ============================================================================

DELETE FROM MedicalHistory;

INSERT INTO MedicalHistory (medical_history_id, trainee_firefighter_id, created_by_health_personnel_id,
                            allergies, preexisting_conditions, current_medication, general_observations, updated_at)
SELECT NEWID(), tf.trainee_firefighter_id, @MedicoHpId,
       CASE ABS(CHECKSUM(NEWID())) % 4
            WHEN 0 THEN N'Polen' WHEN 1 THEN N'Penicilina' WHEN 2 THEN N'Ninguna conocida' ELSE N'Ácaros' END,
       CASE ABS(CHECKSUM(NEWID())) % 4
            WHEN 0 THEN N'Asma leve' WHEN 1 THEN N'Ninguna' WHEN 2 THEN N'Hipertensión controlada' ELSE N'Ninguna' END,
       CASE ABS(CHECKSUM(NEWID())) % 3
            WHEN 0 THEN N'Salbutamol inhalador' WHEN 1 THEN N'Ninguno' ELSE N'Losartán 50 mg' END,
       N'Apto para actividad física de alta demanda. Control periódico.',
       DATEADD(DAY, -(ABS(CHECKSUM(NEWID())) % 60), @Now)
FROM TraineeFirefighter tf;

PRINT 'Historiales médicos: OK';

-- ============================================================================
--  RESUMEN
-- ============================================================================

PRINT '';
PRINT '================ RESUMEN ================';

SELECT 'Usuarios'             AS Entidad, COUNT(*) AS Total FROM [User]
UNION ALL SELECT 'Aspirantes',            COUNT(*) FROM TraineeFirefighter
UNION ALL SELECT 'Personal de salud',     COUNT(*) FROM HealthPersonnel
UNION ALL SELECT 'Centros',               COUNT(*) FROM TrainingLocation
UNION ALL SELECT 'Sesiones (total)',      COUNT(*) FROM TrainingSession
UNION ALL SELECT '  en curso',            COUNT(*) FROM TrainingSession WHERE status = 'InProgress'
UNION ALL SELECT '  planificadas',        COUNT(*) FROM TrainingSession WHERE status = 'Scheduled'
UNION ALL SELECT '  finalizadas',         COUNT(*) FROM TrainingSession WHERE status = 'Finished'
UNION ALL SELECT '  canceladas',          COUNT(*) FROM TrainingSession WHERE status = 'Cancelled'
UNION ALL SELECT 'Invitaciones pend.',    COUNT(*) FROM Invitation WHERE status = 'Pending'
UNION ALL SELECT 'Participantes',         COUNT(*) FROM SessionParticipant
UNION ALL SELECT 'Signos vitales',        COUNT(*) FROM VitalSignsMeasurement
UNION ALL SELECT 'Datos ambientales',     COUNT(*) FROM EnvironmentalData
UNION ALL SELECT 'Historiales médicos',   COUNT(*) FROM MedicalHistory;

PRINT '';
PRINT 'Verificación de tildes (debe leerse correctamente):';
SELECT TOP 3 session_code, title FROM TrainingSession WHERE session_code LIKE 'DEMO-%' ORDER BY session_code;

PRINT 'Listo. Contraseña de todos los usuarios: Smab2026!';
GO
