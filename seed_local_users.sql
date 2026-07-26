-- Script de siembra manual para pruebas locales (entorno "Local", DB bd_bomberos en SQLEXPRESS).
-- Crea un usuario por cada uno de los 7 roles del sistema.
-- Password para TODOS los usuarios: Smab2026!
-- Idempotente: se puede correr varias veces sin duplicar (usa IF NOT EXISTS por email).
--
-- ⚠ CODIFICACIÓN: este archivo está en UTF-8 y contiene tildes. `sqlcmd` lee la
-- entrada con la codepage ANSI del sistema salvo que se le indique lo contrario,
-- y sin el flag correcto las tildes se guardan corruptas («Médico» -> «MÃ©dico»).
-- Ejecuta SIEMPRE con -f 65001:
--
--   sqlcmd -S "localhost\SQLEXPRESS" -E -C -f 65001 -i seed_local_users.sql

USE bd_bomberos;
GO

SET QUOTED_IDENTIFIER ON;
SET ANSI_NULLS ON;
GO

-- Aborta si el archivo se está leyendo con la codepage equivocada, en vez de
-- dejar texto corrupto en la base.
IF NCHAR(243) <> N'ó'
BEGIN
    RAISERROR('Codificación incorrecta: relanza con  sqlcmd -f 65001 -i seed_local_users.sql', 16, 1);
    SET NOEXEC ON;
END
GO

DECLARE @InstitutionId UNIQUEIDENTIFIER;
SELECT @InstitutionId = institution_id FROM TrainingInstitution WHERE acronym = 'SMAB';

IF @InstitutionId IS NULL
BEGIN
    SET @InstitutionId = NEWID();
    INSERT INTO TrainingInstitution (institution_id, name, acronym, country, city, is_active)
    VALUES (@InstitutionId, 'Sistema de Monitoreo y Análisis de Bomberos', 'SMAB', 'Ecuador', 'Quito', 1);
END

-- Roles (código debe coincidir con Roles.cs del backend y ROLES del frontend)
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
SELECT NEWID(), r.Code, r.Name
FROM @Roles r
WHERE NOT EXISTS (SELECT 1 FROM Role WHERE code = r.Code);

-- Password hash bcrypt (work factor 12) para "Smab2026!" — mismo algoritmo que usa el backend.
DECLARE @PasswordHash NVARCHAR(200) = '$2a$12$ogXeZJzvmBlQcTPXRnJMVOEWHN5920sKYvZKPUb7I0W97HrBWgtZO';

-- Usuarios de prueba: uno por rol
DECLARE @Users TABLE (Email NVARCHAR(200), FirstName NVARCHAR(100), LastName NVARCHAR(100), RoleCode NVARCHAR(50));
INSERT INTO @Users (Email, FirstName, LastName, RoleCode) VALUES
    ('sysadmin@smab.app',     'Admin',   'Sistema',  'SYSTEM_ADMIN'),
    ('admin@smab.app',        'Sara',    'Flores',   'ADMIN'),
    ('medico@smab.app',       'Michael', 'Poveda',   'MEDICAL'),
    ('bombero@smab.app',      'Carlos',  'Ruiz',     'FIREFIGHTER_TRAINEE'),
    ('capacitador@smab.app',  'Luis',    'Herrera',  'CAPACITATOR'),
    ('investigador@smab.app', 'Ana',     'Torres',   'RESEARCHER'),
    ('jefe@smab.app',         'Roberto', 'Mendoza',  'FIRE_CHIEF');

DECLARE @Email NVARCHAR(200), @First NVARCHAR(100), @Last NVARCHAR(100), @RoleCode NVARCHAR(50);
DECLARE @UserId UNIQUEIDENTIFIER, @RoleId UNIQUEIDENTIFIER;

DECLARE cur CURSOR FOR SELECT Email, FirstName, LastName, RoleCode FROM @Users;
OPEN cur;
FETCH NEXT FROM cur INTO @Email, @First, @Last, @RoleCode;

WHILE @@FETCH_STATUS = 0
BEGIN
    IF NOT EXISTS (SELECT 1 FROM [User] WHERE email = @Email)
    BEGIN
        SET @UserId = NEWID();

        INSERT INTO [User] (user_id, institution_id, email, first_name, last_name, phone, account_status, created_at)
        VALUES (@UserId, @InstitutionId, @Email, @First, @Last, NULL, 'active', GETUTCDATE());

        INSERT INTO UserCredential (user_credential_id, user_id, password_hash, hash_algorithm, mfa_enabled, failed_attempts, last_password_change_at)
        VALUES (NEWID(), @UserId, @PasswordHash, 'bcrypt', 0, 0, GETUTCDATE());

        SELECT @RoleId = role_id FROM Role WHERE code = @RoleCode;

        INSERT INTO UserRole (user_role_id, user_id, role_id, start_date, is_active)
        VALUES (NEWID(), @UserId, @RoleId, GETUTCDATE(), 1);

        IF @RoleCode = 'MEDICAL'
            INSERT INTO HealthPersonnel (health_personnel_id, user_id, profession, specialty, license_number, can_approve_discharges)
            VALUES (NEWID(), @UserId, 'Médico', 'Medicina General', 'MED-LOCAL-001', 1);

        IF @RoleCode = 'FIREFIGHTER_TRAINEE'
            INSERT INTO TraineeFirefighter (trainee_firefighter_id, user_id, applicant_code, sex, blood_type, training_status)
            VALUES (NEWID(), @UserId, 'BOM-LOCAL-001', 'M', 'O+', 'Active');
    END

    FETCH NEXT FROM cur INTO @Email, @First, @Last, @RoleCode;
END

CLOSE cur;
DEALLOCATE cur;

PRINT 'Listo. Password para todos los usuarios: Smab2026!';
SELECT u.email, r.name AS rol
FROM [User] u
JOIN UserRole ur ON ur.user_id = u.user_id
JOIN Role r ON r.role_id = ur.role_id
ORDER BY r.name;

GO
SET NOEXEC OFF;
GO
