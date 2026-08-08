# Despliegue en el servidor (Windows)

Esta guía es para cuando ya tengan la máquina que va a quedar siempre prendida como
servidor. El desarrollo local (perfil `local`, `appsettings.Local.json`, SQL Express en
tu propia máquina) **no cambia** — sigue funcionando exactamente igual que hasta ahora,
esto es una configuración nueva y separada que solo se usa en el servidor.

## Prerrequisitos en el servidor

1. **ASP.NET Core Runtime 10** (no hace falta el SDK completo, solo el runtime) —
   descargar el "Hosting Bundle" o el "ASP.NET Core Runtime" para Windows desde
   https://dotnet.microsoft.com/download/dotnet/10.0
   - Si van a aplicar migraciones DESDE el propio servidor (en vez de desde tu máquina
     de desarrollo apuntando a la BD remota), instala el **SDK** completo en vez del
     runtime, y además `dotnet tool install --global dotnet-ef`.
2. **SQL Server** (Express es gratis y alcanza de sobra) — instalar con una instancia
   con el mismo nombre que en desarrollo (`SQLEXPRESS`) para poder reusar la connection
   string tal cual, o con el nombre que prefieran (solo hay que ajustar un valor).
3. (Opcional) SQL Server Management Studio, para poder inspeccionar la BD del servidor
   como ya lo haces en local.

## Primer despliegue

1. Copia el repo `ProyectBomberos_Backend` al servidor (clonar el git, o copiar la
   carpeta completa).
2. Abre PowerShell en la carpeta del repo y corre:
   ```powershell
   .\deploy\publish.ps1
   ```
   Esto genera `deploy\publish\` con el build listo para correr.
3. **Edita `src\BomberosAPI.API\appsettings.Production.json`** (el del código fuente,
   no el que quedó copiado dentro de `deploy\publish\` — ese se sobreescribe cada vez
   que vuelvas a publicar, así que los cambios deben ir en el original):
   - `ConnectionStrings:DefaultConnection` — apunta a la instancia de SQL Server del
     servidor (el valor que trae por defecto asume SQL Server en la misma máquina con
     autenticación de Windows, igual que en local — probablemente no haga falta
     tocarlo si instalaste SQL Server igual que en tu equipo de desarrollo).
   - `Cors:AllowedOrigins` — reemplaza el placeholder por la URL real donde va a vivir
     el frontend (ver `Fronted_App_Bomberos/DEPLOY.md`). **Mientras esto no se edite,
     el navegador va a bloquear todas las peticiones del frontend.**
   - `JwtSettings:SecretKey` — el repo trae uno generado para este propósito. Si
     prefieren generar el suyo: cualquier string aleatorio largo sirve (ej. en
     PowerShell: `[Convert]::ToBase64String((1..48 | ForEach-Object { Get-Random -Maximum 256 }))`).
   - Después de editar, vuelve a correr `.\deploy\publish.ps1` para que el cambio se
     copie a `deploy\publish\`.
4. Aplica el esquema a la base de datos del servidor:
   ```powershell
   .\deploy\apply-migrations.ps1
   ```
5. Abre PowerShell **como Administrador** e instala el servicio de Windows:
   ```powershell
   .\deploy\install-service.ps1
   ```
   Esto crea el servicio `BomberosAPI`, lo configura para arrancar solo con Windows
   (sin que nadie tenga que iniciar sesión), abre el puerto 5054 en el firewall, y lo
   arranca.
6. Verifica que responde: `http://localhost:5054/swagger` desde el propio servidor
   (Swagger se apaga en Production por seguridad — para probar rápido, revisa en
   cambio que `Get-Service BomberosAPI` diga `Running`, o intenta un login real desde
   el frontend ya apuntando a este servidor).

## Actualizar a una versión nueva

1. `git pull` (o copiar los archivos nuevos).
2. `.\deploy\publish.ps1`
3. Si la actualización trae migraciones nuevas: `.\deploy\apply-migrations.ps1`
4. Como Administrador: `.\deploy\install-service.ps1` — reinstala y reinicia el
   servicio con el build nuevo. (Como `appsettings.Production.json` es un archivo del
   propio repo con tus datos ya editados —y está en `.gitignore`, así que `git pull`
   nunca lo toca— tus cambios de conexión/CORS se conservan entre actualizaciones sin
   que tengas que volver a editarlos.)

## Cosas a tener en cuenta más adelante (no bloquean el primer despliegue)

- **HTTPS**: hoy el servidor corre en `http://` plano, igual que en desarrollo. Para
  una app médica accesible más allá de la red interna, conviene un certificado TLS más
  adelante (Let's Encrypt si hay dominio propio, o un certificado interno si es una
  red corporativa cerrada).
- **Cuentas sembradas (`DbSeeder`)**: solo corre en el entorno `Development`, así que
  en Production la base de datos queda vacía de usuarios — hay que crear la primera
  cuenta de administrador manualmente (por SQL directo, o corriendo el seeder una vez
  con `ASPNETCORE_ENVIRONMENT=Development` apuntado a la BD de producción y luego
  cambiando esa contraseña sembrada de inmediato). No se sembraron automáticamente las
  13 cuentas de prueba con la contraseña `Smab2026!` a propósito — esa contraseña ya
  se ha escrito muchas veces en este chat y en el código, no es apta para producción.
- **Backups de la base de datos**: SQL Server no hace backups automáticos solo por
  instalarlo — hay que configurar un plan de mantenimiento (SQL Server Agent) o un
  script programado si esta base de datos va a tener información real de bomberos.
