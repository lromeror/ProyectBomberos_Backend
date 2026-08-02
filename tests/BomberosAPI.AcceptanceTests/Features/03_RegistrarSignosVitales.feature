# language: es
Requisito: Registro y validación fisiológica de signos vitales (US-34, US-36)
  Como Personal Médico / Nutricionista (medical-001)
  Quiero registrar las mediciones de signos vitales de un bombero aspirante durante una sesión
  Para validar los rangos fisiológicos y asociar las mediciones al historial clínico del aspirante

  Criterio de aceptación (Tabla 9.1):
  - El personal médico registra frecuencia cardíaca, saturación de oxígeno SpO2, presión arterial, temperatura corporal y rol en la práctica.
  - El sistema valida las mediciones contra sus rangos fisiológicos permitidos.
  - Las mediciones válidas se vinculan al participante y quedan disponibles para consulta médica y diagramas interactivos.
  - Mediciones fuera de rangos fisiológicos son rechazadas con error 400 Bad Request.

  @acceptance @vital_signs
  Escenario: Registro exitoso de signos vitales dentro de rangos fisiológicos
    Dado que existe una sesión de entrenamiento activa con el aspirante "bombero@smab.app" como participante registrado
    Y el usuario "medical-001" con correo "medico@smab.app" ha iniciado sesión como "MEDICAL"
    Cuando el médico registra una medición de signos vitales para el aspirante con los siguientes valores:
      | FrecuenciaCardiaca | PresionSistolica | PresionDiastolica | Temperatura | SaturacionOxigeno | RolPractica | EsFumador | ExpuestoHumo48h |
      | 78                 | 120              | 80                | 36.6        | 98                | Pitonero    | false     | false           |
    Entonces la respuesta debe tener código de estado 201
    Y la medición de signos vitales queda registrada y vinculada al participante de la sesión

  @acceptance @vital_signs @validation
  Escenario: Rechazo de medición de signos vitales fuera de rangos fisiológicos razonables
    Dado que existe una sesión de entrenamiento activa con el aspirante "bombero@smab.app" como participante registrado
    Y el usuario "medical-001" con correo "medico@smab.app" ha iniciado sesión como "MEDICAL"
    Cuando el médico intenta registrar signos vitales con valores fisiológicamente inválidos:
      | FrecuenciaCardiaca | PresionSistolica | PresionDiastolica | Temperatura | SaturacionOxigeno |
      | 450                | 350              | 200               | 65.0        | 150               |
    Entonces la respuesta debe tener código de estado 400
    Y la respuesta debe indicar errores de validación en los rangos fisiológicos
