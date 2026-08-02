# language: es
Requisito: Consulta de resultados propios y protección de privacidad (US-41, US-45)
  Como Bombero Aspirante (trainee-001)
  Quiero iniciar sesión y consultar exclusivamente mis resultados de sesión y mi historial de signos vitales
  Para monitorear mi rendimiento y evolución médica sin acceder a datos de otros aspirantes

  Criterio de aceptación (Tabla 9.1):
  - El aspirante inicia sesión con sus credenciales.
  - Al consultar su historial de signos vitales (/api/vital-signs/by-trainee/{id}), visualiza únicamente sus propias lecturas históricas.
  - Al consultar resultados de sus sesiones (/api/session-results/by-participant/{id}), recibe su información respectiva.
  - El sistema no permite que un aspirante acceda a lecturas médicas no filtradas o de otros aspirantes (403 Forbidden o filtrado estricto).

  @acceptance @trainee_results
  Escenario: El bombero aspirante consulta sus propios resultados de sesión e historial de signos vitales
    Dado que existen mediciones de signos vitales registradas para el aspirante "bombero@smab.app"
    Y existen mediciones de signos vitales registradas para otro aspirante "bombero2@smab.app"
    Cuando el usuario "trainee-001" con correo "bombero@smab.app" ha iniciado sesión como "FIREFIGHTER_TRAINEE"
    Y el aspirante consulta su propio historial de signos vitales
    Entonces la respuesta debe tener código de estado 200
    Y todas las lecturas de signos vitales devueltas deben corresponder únicamente al aspirante "bombero@smab.app"
    Y ninguna lectura debe pertenecer al aspirante "bombero2@smab.app"

  @acceptance @trainee_results @security
  Escenario: El bombero aspirante tiene restringido el acceso a la lista global de mediciones médicas
    Dado que el usuario "trainee-001" con correo "bombero@smab.app" ha iniciado sesión como "FIREFIGHTER_TRAINEE"
    Cuando el aspirante intenta consultar el listado global no filtrado de signos vitales
    Entonces la respuesta debe tener código de estado 403
