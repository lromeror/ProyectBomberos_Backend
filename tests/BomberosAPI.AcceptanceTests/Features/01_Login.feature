# language: es
Requisito: Autenticación y control de acceso por roles (US-12, US-34, US-41)
  Como usuario del sistema SMAB (Jefe de Bomberos, Personal Médico, Bombero Aspirante)
  Quiero autenticarme con mis credenciales
  Para acceder a las funcionalidades autorizadas según mi rol

  Criterio de aceptación:
  - Se debe validar el correo y contraseña contra la base de datos.
  - Al autenticarse correctamente, el sistema responde 200 OK con un token JWT firmado y la lista de roles del usuario.

  @acceptance @auth
  Esquema del escenario: Inicio de sesión exitoso por rol
    Dado que existe un usuario registrado con correo "<Email>" y contraseña "<Password>" con rol "<RoleCode>"
    Cuando el usuario envía una solicitud de inicio de sesión con correo "<Email>" y contraseña "<Password>"
    Entonces la respuesta de autenticación debe tener código de estado 200
    Y la respuesta debe contener un token JWT válido
    Y el perfil del usuario autenticado debe incluir el rol "<RoleCode>"

    Ejemplos:
      | RolDescription      | Email             | Password  | RoleCode            |
      | Fire Chief          | jefe@smab.app     | Smab2026! | FIRE_CHIEF          |
      | Medical             | medico@smab.app   | Smab2026! | MEDICAL             |
      | Firefighter Trainee | bombero@smab.app  | Smab2026! | FIREFIGHTER_TRAINEE |
