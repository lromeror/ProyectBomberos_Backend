-- Restablece la contraseña de admin@smab.app al valor estándar de las cuentas de
-- prueba (Smab2026!) — el mismo hash bcrypt que ya usan sysadmin@, medico@, etc.
-- Se le olvidó/cambió en algún momento de pruebas y no hay flujo de "admin resetea
-- la contraseña de otro usuario" en la API (solo reset-por-correo, y admin@smab.app
-- no es una bandeja real) — por eso el ajuste directo por SQL.

USE bd_bomberos;
GO

UPDATE UserCredential
   SET password_hash = '$2a$12$ogXeZJzvmBlQcTPXRnJMVOEWHN5920sKYvZKPUb7I0W97HrBWgtZO',
       hash_algorithm = 'bcrypt',
       failed_attempts = 0,
       last_password_change_at = GETUTCDATE()
 WHERE user_id = (SELECT user_id FROM [User] WHERE email = 'admin@smab.app');

SELECT 'Filas actualizadas' AS resultado, @@ROWCOUNT AS n;
GO
