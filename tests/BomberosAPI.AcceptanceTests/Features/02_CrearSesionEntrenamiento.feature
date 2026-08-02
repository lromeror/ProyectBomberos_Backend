# language: es
Requisito: Creación de sesión de entrenamiento y gestión de participantes (US-12, US-27)
  Como Jefe de Bomberos (chief-001)
  Quiero crear una nueva sesión de entrenamiento en la casa de fuego, asignar personal médico/capacitadores y enviar invitaciones a aspirantes
  Para organizar y planificar las prácticas de entrenamiento con control de participantes

  Criterio de aceptación (Tabla 9.1):
  - El Jefe de Bomberos crea la sesión especificando nombre, fecha, lugar (Burn House) y capacidad de quemas.
  - Se asigna personal médico y capacitadores.
  - Se agregan bomberos aspirantes enviándoles invitaciones por correo electrónico.
  - La sesión se crea con sus participantes y las invitaciones quedan en estado Pendiente.

  @acceptance @training_sessions
  Escenario: El Jefe de Bomberos crea exitosamente una sesión de entrenamiento con participantes e invitaciones
    Dado que el usuario "chief-001" con correo "jefe@smab.app" ha iniciado sesión como "FIRE_CHIEF"
    Cuando el Jefe de Bomberos crea una sesión de entrenamiento con los siguientes datos:
      | Titulo                                         | Tipo              | Ubicacion                  | CapacidadPlanificada | DiasEnElFuturo |
      | Capacitación Quemas Vivas - Casa de Fuego Alpha| Live Fire Training| Casa de Fuego (Burn House) | 12                   | 5              |
    Entonces la respuesta debe tener código de estado 201
    Y la sesión de entrenamiento creada debe tener estado "Scheduled"
    Cuando el Jefe de Bomberos asigna al personal médico "medico@smab.app" a la sesión
    Y el Jefe de Bomberos envía una invitación por correo al bombero aspirante "bombero@smab.app" para la sesión
    Y el Jefe de Bomberos envía una invitación por correo al bombero aspirante "bombero2@smab.app" para la sesión
    Entonces las invitaciones para la sesión son creadas exitosamente con estado "Pending"
