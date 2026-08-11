-- ============================================================================
--  DATOS DE PRUEBA COMPLETOS — para el servidor (contenedor Docker `db`)
-- ============================================================================
--
--  Script único y autocontenido: crea usuarios de los 7 roles, institución,
--  centros de entrenamiento, sesiones en los cuatro estados (con volumen para
--  probar paginación), invitaciones, participantes, signos vitales,
--  bioimpedancia + marcadores de investigación, reportes de síntomas,
--  resultados de sesión, alertas críticas, datos ambientales e historiales
--  médicos. Cubre todas las pantallas de la app para los 7 roles.
--
--  IDEMPOTENTE: se puede correr varias veces sin duplicar. Usuarios y centros
--  se actualizan si ya existen; las sesiones/invitaciones/mediciones DEMO se
--  borran y se vuelven a crear en cada corrida (prefijo 'DEMO-').
--
--  Contraseña de TODOS los usuarios creados: Smab2026!
--
--  ── Cómo ejecutarlo contra el servidor (contenedor Docker) ──
--  Desde el repo del frontend en el servidor (ahí vive docker-compose.yml),
--  con el stack ya levantado (`db` corriendo):
--
--    cd ~/Fronted_App_Bomberos
--    docker compose exec -T db bash -c '
--      SQLCMD=$(command -v sqlcmd || true)
--      [ -x "$SQLCMD" ] || SQLCMD=/opt/mssql-tools18/bin/sqlcmd
--      [ -x "$SQLCMD" ] || SQLCMD=/opt/mssql-tools/bin/sqlcmd
--      exec "$SQLCMD" -S localhost -U sa -P "$MSSQL_SA_PASSWORD" -C -d bd_bomberos -f 65001 -i /dev/stdin
--    ' < ../ProyectBomberos_Backend/seed_all_dummy_data.sql
--
--  (No hace falta pasar la contraseña a mano: el contenedor `db` ya la tiene
--  en su propia variable de entorno MSSQL_SA_PASSWORD, la misma con la que se
--  inicializó SQL Server.)
--
--  ── Si lo corres en local (SQLEXPRESS, Windows) ──
--    sqlcmd -S "localhost\SQLEXPRESS" -E -C -f 65001 -i seed_all_dummy_data.sql
--
--  ⚠ CODIFICACIÓN: el archivo está en UTF-8 y tiene tildes/ñ. sqlcmd lee la
--  entrada con la codepage ANSI del sistema salvo que se le indique lo
--  contrario — por eso el flag -f 65001 es obligatorio en ambos casos. El
--  script aborta solo si detecta que se está leyendo mal, en vez de guardar
--  texto corrupto.
-- ============================================================================

USE bd_bomberos;
GO

SET QUOTED_IDENTIFIER ON;
SET ANSI_NULLS ON;
SET NOCOUNT ON;
GO

IF NCHAR(243) <> N'ó'
BEGIN
    RAISERROR('Codificación incorrecta: relanza con  -f 65001  (ver instrucciones al inicio del archivo).', 16, 1);
    RETURN;
END
GO

DECLARE @Now          DATETIME2 = GETUTCDATE();
DECLARE @PasswordHash NVARCHAR(200) = '$2a$12$ogXeZJzvmBlQcTPXRnJMVOEWHN5920sKYvZKPUb7I0W97HrBWgtZO'; -- "Smab2026!"

-- ============================================================================
--  1. INSTITUCIÓN + ROLES
-- ============================================================================

DECLARE @InstitutionId UNIQUEIDENTIFIER;
SELECT @InstitutionId = institution_id FROM TrainingInstitution WHERE acronym = 'SMAB';

IF @InstitutionId IS NULL
BEGIN
    SET @InstitutionId = NEWID();
    INSERT INTO TrainingInstitution (institution_id, name, acronym, country, city, is_active)
    VALUES (@InstitutionId, N'Sistema de Monitoreo y Análisis de Bomberos', 'SMAB', 'Ecuador', 'Quito', 1);
END
ELSE
    UPDATE TrainingInstitution SET name = N'Sistema de Monitoreo y Análisis de Bomberos' WHERE institution_id = @InstitutionId;

DECLARE @Roles TABLE (Code NVARCHAR(50), Name NVARCHAR(100));
INSERT INTO @Roles (Code, Name) VALUES
    ('SYSTEM_ADMIN',        'System Administrator'),
    ('ADMIN',               'Administrator'),
    ('MEDICAL',             'Medical Personnel'),
    ('FIREFIGHTER_TRAINEE', 'Firefighter Trainee'),
    ('CAPACITATOR',         'Capacitator / Instructor'),
    ('RESEARCHER',          'Researcher'),
    ('FIRE_CHIEF',          'Fire Chief');

INSERT INTO Role (role_id, code, name)
SELECT NEWID(), r.Code, r.Name FROM @Roles r
WHERE NOT EXISTS (SELECT 1 FROM Role WHERE code = r.Code);

PRINT 'Institución y roles: OK';

-- ============================================================================
--  2. USUARIOS — uno por rol + extras (aspirantes, personal de salud,
--     capacitadores) para poder ejercitar filtros, listas y paginación.
-- ============================================================================

DECLARE @NewUsers TABLE (
    Email NVARCHAR(200), FirstName NVARCHAR(100), LastName NVARCHAR(100),
    RoleCode NVARCHAR(50), Phone NVARCHAR(30),
    Profession NVARCHAR(100), Specialty NVARCHAR(100),
    ApplicantCode NVARCHAR(50), Sex NVARCHAR(20), BloodType NVARCHAR(10)
);

INSERT INTO @NewUsers VALUES
 -- Un usuario base por rol
 ('sysadmin@smab.app',     N'Admin',    N'Sistema',  'SYSTEM_ADMIN',        '+593 98-000-0001', NULL, NULL, NULL, NULL, NULL),
 ('admin@smab.app',        N'Sara',     N'Flores',   'ADMIN',               '+593 98-000-0002', NULL, NULL, NULL, NULL, NULL),
 ('medico@smab.app',       N'Michael',  N'Poveda',   'MEDICAL',             '+593 99-100-0001', N'Médico', N'Medicina General', NULL, NULL, NULL),
 ('bombero@smab.app',      N'Carlos',   N'Ruiz',     'FIREFIGHTER_TRAINEE', '+593 98-200-0001', NULL, NULL, 'BOM-2026-001', 'M', 'O+'),
 ('capacitador@smab.app',  N'Luis',     N'Herrera',  'CAPACITATOR',         '+593 97-300-0001', NULL, NULL, NULL, NULL, NULL),
 ('investigador@smab.app', N'Ana',      N'Torres',   'RESEARCHER',          '+593 96-400-0001', NULL, NULL, NULL, NULL, NULL),
 ('jefe@smab.app',         N'Roberto',  N'Mendoza',  'FIRE_CHIEF',          '+593 95-500-0001', NULL, NULL, NULL, NULL, NULL),

 -- Aspirantes extra (para listas largas, sesiones con varios participantes)
 ('bombero2@smab.app', N'Marco',   N'Torres',   'FIREFIGHTER_TRAINEE','+593 98-200-0002',NULL,NULL,'BOM-2026-002','M','A+'),
 ('bombero3@smab.app', N'Sara',    N'Vega',     'FIREFIGHTER_TRAINEE','+593 98-200-0003',NULL,NULL,'BOM-2026-003','F','B+'),
 ('bombero4@smab.app', N'Luis',    N'Paredes',  'FIREFIGHTER_TRAINEE','+593 98-200-0004',NULL,NULL,'BOM-2026-004','M','AB+'),
 ('bombero5@smab.app', N'Diego',   N'Carrillo', 'FIREFIGHTER_TRAINEE','+593 98-200-0005',NULL,NULL,'BOM-2026-005','M','O-'),
 ('bombero6@smab.app', N'Andrea',  N'Suárez',   'FIREFIGHTER_TRAINEE','+593 98-200-0006',NULL,NULL,'BOM-2026-006','F','O+'),
 ('bombero7@smab.app', N'Pablo',   N'Jiménez',  'FIREFIGHTER_TRAINEE','+593 98-200-0007',NULL,NULL,'BOM-2026-007','M','A-'),
 ('bombero8@smab.app', N'Karla',   N'Moreno',   'FIREFIGHTER_TRAINEE','+593 98-200-0008',NULL,NULL,'BOM-2026-008','F','B-'),
 ('bombero9@smab.app', N'Iván',    N'Salazar',  'FIREFIGHTER_TRAINEE','+593 98-200-0009',NULL,NULL,'BOM-2026-009','M','O+'),
 ('bombero10@smab.app',N'Mónica',  N'Cedeño',   'FIREFIGHTER_TRAINEE','+593 98-200-0010',NULL,NULL,'BOM-2026-010','F','AB-'),
 ('bombero11@smab.app',N'Esteban', N'Vaca',     'FIREFIGHTER_TRAINEE','+593 98-200-0011',NULL,NULL,'BOM-2026-011','M','A+'),
 ('bombero12@smab.app',N'Lucía',   N'Andrade',  'FIREFIGHTER_TRAINEE','+593 98-200-0012',NULL,NULL,'BOM-2026-012','F','O+'),

 -- Personal de salud extra (médico, enfermería, nutrición)
 ('enfermera@smab.app',     N'Valeria', N'Castro', 'MEDICAL','+593 99-100-0002',N'Enfermero',     N'Urgencias',           NULL,NULL,NULL),
 ('nutricionista@smab.app', N'Andrea',  N'Rivas',  'MEDICAL','+593 99-100-0003',N'Nutricionista', N'Deportiva',           NULL,NULL,NULL),
 ('medico2@smab.app',       N'Daniel',  N'Ortega', 'MEDICAL','+593 99-100-0004',N'Médico',        N'Medicina General',    NULL,NULL,NULL),
 ('enfermero2@smab.app',    N'Jorge',   N'Bravo',  'MEDICAL','+593 99-100-0005',N'Enfermero',     N'Cuidados intensivos', NULL,NULL,NULL),

 -- Capacitadores extra
 ('capacitador2@smab.app', N'Elena', N'Ruiz',    'CAPACITATOR','+593 97-300-0002',NULL,NULL,NULL,NULL,NULL),
 ('capacitador3@smab.app', N'Óscar', N'Naranjo', 'CAPACITATOR','+593 97-300-0003',NULL,NULL,NULL,NULL,NULL);

-- Repara nombres/teléfonos de usuarios ya existentes (corrige tildes de corridas
-- previas sin -f 65001, y mantiene los datos al día si se vuelve a correr).
UPDATE u
   SET first_name = n.FirstName, last_name = n.LastName, phone = n.Phone
  FROM [User] u JOIN @NewUsers n ON n.Email = u.email;

UPDATE hp
   SET profession = n.Profession, specialty = n.Specialty
  FROM HealthPersonnel hp
  JOIN [User] u    ON u.user_id = hp.user_id
  JOIN @NewUsers n ON n.Email = u.email
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

        INSERT INTO [User] (user_id, institution_id, email, first_name, last_name, phone, account_status, email_verified, created_at)
        VALUES (@UserId, @InstitutionId, @Email, @First, @Last, @Phone, 'active', 1, DATEADD(DAY, -120, @Now));

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

DECLARE @AdminId UNIQUEIDENTIFIER;
SELECT @AdminId = user_id FROM [User] WHERE email = 'admin@smab.app';

PRINT 'Usuarios: OK (23)';

-- ============================================================================
--  3. LIMPIEZA de datos DEMO previos (en orden de dependencias) — así el
--     script se puede correr varias veces sin ir acumulando duplicados.
-- ============================================================================

DECLARE @DemoSessions TABLE (id UNIQUEIDENTIFIER);
INSERT INTO @DemoSessions SELECT training_session_id FROM TrainingSession WHERE session_code LIKE 'DEMO-%';

DECLARE @DemoParticipants TABLE (id UNIQUEIDENTIFIER);
INSERT INTO @DemoParticipants
SELECT session_participant_id FROM SessionParticipant WHERE training_session_id IN (SELECT id FROM @DemoSessions);

DELETE FROM CriticalAlert           WHERE session_participant_id IN (SELECT id FROM @DemoParticipants);
DELETE FROM SessionResult           WHERE session_participant_id IN (SELECT id FROM @DemoParticipants);
DELETE FROM SymptomReport           WHERE session_participant_id IN (SELECT id FROM @DemoParticipants);
DELETE FROM BioimpedanceMeasurement WHERE session_participant_id IN (SELECT id FROM @DemoParticipants);
DELETE FROM VitalSignsMeasurement   WHERE session_participant_id IN (SELECT id FROM @DemoParticipants);
DELETE FROM SessionParticipant      WHERE training_session_id     IN (SELECT id FROM @DemoSessions);
DELETE FROM EnvironmentalData       WHERE training_session_id     IN (SELECT id FROM @DemoSessions);
DELETE FROM Invitation              WHERE training_session_id     IN (SELECT id FROM @DemoSessions);
DELETE FROM TrainingSession         WHERE training_session_id     IN (SELECT id FROM @DemoSessions);

PRINT 'Limpieza de datos DEMO previos: OK';

-- ============================================================================
--  4. CENTROS DE ENTRENAMIENTO
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
--  5. SESIONES — 26 en total, repartidas en los cuatro estados
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
--  6. INVITACIONES (también alimentan "instructores" en el detalle de sesión
--     y la Cola de Validaciones del personal médico/admin)
-- ============================================================================

DECLARE @Inv TABLE (SessionCode NVARCHAR(50), Email NVARCHAR(200), Status NVARCHAR(50), HoursAgo INT);
INSERT INTO @Inv VALUES
 ('DEMO-P1','enfermera@smab.app',    'Pending',  2),
 ('DEMO-P1','nutricionista@smab.app','Pending',  4),
 ('DEMO-P2','medico2@smab.app',      'Pending',  5),
 ('DEMO-P2','enfermero2@smab.app',   'Pending',  7),
 ('DEMO-P3','enfermera@smab.app',    'Pending',  9),
 ('DEMO-P4','medico2@smab.app',      'Pending', 11),
 ('DEMO-P5','nutricionista@smab.app','Pending', 13),
 ('DEMO-P6','enfermero2@smab.app',   'Pending', 15),
 ('DEMO-A1','medico@smab.app',       'Accepted', 30),
 ('DEMO-A1','enfermera@smab.app',    'Accepted', 30),
 ('DEMO-A2','medico2@smab.app',      'Accepted', 28),
 ('DEMO-A3','nutricionista@smab.app','Accepted', 26),
 ('DEMO-F09','medico@smab.app',      'Accepted', 720),
 ('DEMO-F12','nutricionista@smab.app','Accepted',100),
 ('DEMO-C1','enfermero2@smab.app',   'Rejected', 600),
 ('DEMO-C2','medico2@smab.app',      'Rejected', 300),
 ('DEMO-C4','enfermera@smab.app',    'Rejected', 150),
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
--  7. PARTICIPANTES (todos los aspirantes en sesiones finalizadas y en curso)
-- ============================================================================

INSERT INTO SessionParticipant (session_participant_id, training_session_id, trainee_firefighter_id,
                                participation_status, attendance_confirmed, check_in_at, observations)
SELECT NEWID(), ts.training_session_id, tf.trainee_firefighter_id,
       CASE WHEN ts.status = 'Finished' THEN 'Attended' ELSE 'Registered' END,
       1, ts.scheduled_start, NULL
FROM TrainingSession ts
CROSS JOIN TraineeFirefighter tf
WHERE ts.session_code LIKE 'DEMO-%' AND ts.status IN ('Finished','InProgress');

PRINT 'Participantes: OK';

DECLARE @MedicoHpId UNIQUEIDENTIFIER;
SELECT TOP 1 @MedicoHpId = hp.health_personnel_id
FROM HealthPersonnel hp JOIN [User] u ON u.user_id = hp.user_id
WHERE u.email = 'medico@smab.app';

-- ============================================================================
--  8. SIGNOS VITALES (basal / esfuerzo / recuperación por sesión finalizada;
--     solo basal para las que están en curso). spo2 y temperature_c son
--     decimal(4,2) -> máximo 99.99.
-- ============================================================================

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
--  9. BIOIMPEDANCIA + MARCADORES DE INVESTIGACIÓN
--     Una medición por participante de sesión finalizada — alimenta el
--     diagrama corporal, "Marcadores de Investigación" y el dashboard de
--     Investigador (exportación/reportes).
-- ============================================================================

INSERT INTO BioimpedanceMeasurement (bioimpedance_measurement_id, session_participant_id,
                                     registered_by_health_personnel_id,
                                     weight_kg, fat_percentage, muscle_mass_kg, body_water_pct, basal_metabolic_rate,
                                     metabolic_age_years, lactate_pre_mmol, lactate_post_mmol,
                                     stroop_time_seconds, stroop_errors, taken_at)
SELECT NEWID(), sp.session_participant_id, @MedicoHpId,
       70 + (ABS(CHECKSUM(NEWID())) % 25),                              -- peso 70-94 kg
       12 + (ABS(CHECKSUM(NEWID())) % 15),                              -- grasa 12-26 %
       32 + (ABS(CHECKSUM(NEWID())) % 12),                              -- masa muscular 32-43 kg
       55 + (ABS(CHECKSUM(NEWID())) % 10),                              -- agua corporal 55-64 %
       1600 + (ABS(CHECKSUM(NEWID())) % 500),                           -- metabolismo basal
       22 + (ABS(CHECKSUM(NEWID())) % 20),                              -- edad metabólica
       1.0 + (ABS(CHECKSUM(NEWID())) % 10) / 10.0,                      -- lactato pre 1.0-1.9
       3.0 + (ABS(CHECKSUM(NEWID())) % 40) / 10.0,                      -- lactato post 3.0-6.9
       12 + (ABS(CHECKSUM(NEWID())) % 15),                              -- tiempo Stroop 12-26 s
       ABS(CHECKSUM(NEWID())) % 4,                                      -- errores Stroop 0-3
       DATEADD(MINUTE, 30, ts.scheduled_start)
FROM SessionParticipant sp
JOIN TrainingSession ts ON ts.training_session_id = sp.training_session_id
WHERE ts.session_code LIKE 'DEMO-%' AND ts.status = 'Finished';

PRINT 'Bioimpedancia + marcadores de investigación: OK';

-- ============================================================================
--  10. REPORTES DE SÍNTOMAS
--      Solo a una parte de los participantes (no todos reportan síntomas) —
--      alimenta el historial de síntomas del aspirante y, para los graves,
--      dispara una alerta crítica (sección 12).
-- ============================================================================

DECLARE @SymptomOptions TABLE (Idx INT, Combo NVARCHAR(200), Severity NVARCHAR(20));
INSERT INTO @SymptomOptions VALUES
 (0, N'Fatiga, Mareo',                    'Low'),
 (1, N'Dolor de cabeza',                  'Low'),
 (2, N'Náusea, Mareo',                    'Medium'),
 (3, N'Dolor muscular',                   'Low'),
 (4, N'Dificultad respiratoria, Tos',     'High'),
 (5, N'Irritación ocular',                'Low'),
 (6, N'Fatiga, Dolor muscular, Náusea',   'Medium');

-- Un reporte por cada 3er participante de sesión finalizada/en curso (variedad
-- sin saturar), eligiendo el combo de síntomas por un hash determinístico del
-- propio participante (no aleatorio en cada corrida, para que sea reproducible).
-- `WHERE Idx = ...` en vez de `ORDER BY ... TOP 1`: ese valor no depende de
-- ninguna columna de @SymptomOptions, así que ordenar por él dejaba las 7 filas
-- empatadas y el TOP 1 salía en un orden no garantizado por el motor.
INSERT INTO SymptomReport (symptom_report_id, session_participant_id, reported_by_user_id,
                           severity, symptoms, requires_alert, reported_at)
SELECT NEWID(), sp.session_participant_id, tf_user.user_id,
       so.Severity, so.Combo,
       CASE WHEN so.Severity = 'High' THEN 1 ELSE 0 END,
       DATEADD(MINUTE, 100, ts.scheduled_start)
FROM SessionParticipant sp
JOIN TrainingSession ts        ON ts.training_session_id = sp.training_session_id
JOIN TraineeFirefighter tf     ON tf.trainee_firefighter_id = sp.trainee_firefighter_id
JOIN [User] tf_user            ON tf_user.user_id = tf.user_id
CROSS APPLY (
    SELECT Combo, Severity FROM @SymptomOptions
    WHERE Idx = ABS(CHECKSUM(sp.session_participant_id)) % 7
) so
WHERE ts.session_code LIKE 'DEMO-%' AND ts.status IN ('Finished','InProgress')
  AND ABS(CHECKSUM(sp.session_participant_id)) % 3 = 0;

PRINT 'Reportes de síntomas: OK';

-- ============================================================================
--  11. RESULTADOS DE SESIÓN
--      Uno por participante de sesión finalizada — validado por Médico.
--      RiskClassification válido: Low / Medium / High.
-- ============================================================================

DECLARE @RiskOptions TABLE (Idx INT, Risk NVARCHAR(20), Fit BIT, Summary NVARCHAR(300));
INSERT INTO @RiskOptions VALUES
 (0, 'Low',    1, N'Desempeño dentro de parámetros normales. Apto para continuar el programa sin restricciones.'),
 (1, 'Medium', 1, N'Signos de fatiga moderada durante el esfuerzo. Se recomienda hidratación reforzada en la próxima sesión.'),
 (2, 'High',   0, N'Variación significativa en signos vitales durante el esfuerzo. Requiere evaluación médica antes de la próxima sesión.');

-- Mismo motivo que en @SymptomOptions: se busca por `Idx` en vez de `ORDER BY
-- <expresión constante> ... TOP 1`, que no garantizaba cuál de las 3 filas salía.
INSERT INTO SessionResult (session_result_id, session_participant_id, validated_by_user_id,
                           performance_score, risk_classification, fit_to_continue, summary, generated_at)
SELECT NEWID(), sp.session_participant_id, mu.user_id,
       60 + (ABS(CHECKSUM(NEWID())) % 40),
       ro.Risk, ro.Fit, ro.Summary,
       DATEADD(MINUTE, 240, ts.scheduled_start)
FROM SessionParticipant sp
JOIN TrainingSession ts ON ts.training_session_id = sp.training_session_id
JOIN [User] mu           ON mu.email = 'medico@smab.app'
CROSS APPLY (
    SELECT Risk, Fit, Summary FROM @RiskOptions
    WHERE Idx = CASE
        WHEN ABS(CHECKSUM(sp.session_participant_id)) % 10 < 7 THEN 0  -- 70% Low
        WHEN ABS(CHECKSUM(sp.session_participant_id)) % 10 < 9 THEN 1  -- 20% Medium
        ELSE 2                                                        -- 10% High
    END
) ro
WHERE ts.session_code LIKE 'DEMO-%' AND ts.status = 'Finished';

PRINT 'Resultados de sesión: OK';

-- ============================================================================
--  12. ALERTAS CRÍTICAS
--      Una por cada reporte de síntoma "High" (severidad alta = requiere
--      atención). La mitad quedan "Open" (para probar el flujo de atenderlas)
--      y la otra mitad "Attended".
-- ============================================================================

INSERT INTO CriticalAlert (critical_alert_id, session_participant_id, symptom_report_id,
                           attended_by_user_id, alert_type, severity, status, description,
                           generated_at, attended_at)
SELECT NEWID(), sr.session_participant_id, sr.symptom_report_id,
       CASE WHEN ABS(CHECKSUM(sr.symptom_report_id)) % 2 = 0 THEN mu.user_id END,
       'Symptom', 'High',
       CASE WHEN ABS(CHECKSUM(sr.symptom_report_id)) % 2 = 0 THEN 'Attended' ELSE 'Open' END,
       CONCAT(N'Síntomas reportados: ', sr.symptoms),
       sr.reported_at,
       CASE WHEN ABS(CHECKSUM(sr.symptom_report_id)) % 2 = 0 THEN DATEADD(MINUTE, 15, sr.reported_at) END
FROM SymptomReport sr
JOIN SessionParticipant sp ON sp.session_participant_id = sr.session_participant_id
JOIN TrainingSession ts    ON ts.training_session_id = sp.training_session_id
JOIN [User] mu              ON mu.email = 'medico@smab.app'
WHERE ts.session_code LIKE 'DEMO-%' AND sr.severity = 'High';

PRINT 'Alertas críticas: OK';

-- ============================================================================
--  13. DATOS AMBIENTALES
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
--  14. HISTORIALES MÉDICOS (uno por aspirante que todavía no tenga uno)
--      A diferencia del resto de las secciones, esta tabla NO se limpia antes
--      de reinsertar (podría haber historiales reales creados a mano desde la
--      app, no solo de este script) — solo se completa lo que falte.
-- ============================================================================

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
FROM TraineeFirefighter tf
WHERE NOT EXISTS (SELECT 1 FROM MedicalHistory mh WHERE mh.trainee_firefighter_id = tf.trainee_firefighter_id);

PRINT 'Historiales médicos: OK';

-- ============================================================================
--  RESUMEN
-- ============================================================================

PRINT '';
PRINT '================ RESUMEN ================';

SELECT 'Usuarios'               AS Entidad, COUNT(*) AS Total FROM [User]
UNION ALL SELECT 'Aspirantes',              COUNT(*) FROM TraineeFirefighter
UNION ALL SELECT 'Personal de salud',       COUNT(*) FROM HealthPersonnel
UNION ALL SELECT 'Centros',                 COUNT(*) FROM TrainingLocation
UNION ALL SELECT 'Sesiones (total)',        COUNT(*) FROM TrainingSession
UNION ALL SELECT '  en curso',              COUNT(*) FROM TrainingSession WHERE status = 'InProgress'
UNION ALL SELECT '  planificadas',          COUNT(*) FROM TrainingSession WHERE status = 'Scheduled'
UNION ALL SELECT '  finalizadas',           COUNT(*) FROM TrainingSession WHERE status = 'Finished'
UNION ALL SELECT '  canceladas',            COUNT(*) FROM TrainingSession WHERE status = 'Cancelled'
UNION ALL SELECT 'Invitaciones pend.',      COUNT(*) FROM Invitation WHERE status = 'Pending'
UNION ALL SELECT 'Participantes',           COUNT(*) FROM SessionParticipant
UNION ALL SELECT 'Signos vitales',          COUNT(*) FROM VitalSignsMeasurement
UNION ALL SELECT 'Bioimpedancia',           COUNT(*) FROM BioimpedanceMeasurement
UNION ALL SELECT 'Reportes de síntomas',    COUNT(*) FROM SymptomReport
UNION ALL SELECT 'Alertas críticas',        COUNT(*) FROM CriticalAlert
UNION ALL SELECT 'Resultados de sesión',    COUNT(*) FROM SessionResult
UNION ALL SELECT 'Datos ambientales',       COUNT(*) FROM EnvironmentalData
UNION ALL SELECT 'Historiales médicos',     COUNT(*) FROM MedicalHistory;

PRINT '';
PRINT 'Verificación de tildes (debe leerse correctamente):';
SELECT TOP 3 session_code, title FROM TrainingSession WHERE session_code LIKE 'DEMO-%' ORDER BY session_code;

PRINT '';
PRINT '=========================================================';
PRINT ' Listo. Password de TODOS los usuarios: Smab2026!';
PRINT ' Roles: sysadmin@ · admin@ · medico@ · bombero@ ·';
PRINT '        capacitador@ · investigador@ · jefe@   (@smab.app)';
PRINT '=========================================================';
GO
