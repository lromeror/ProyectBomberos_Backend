<#
.SYNOPSIS
  Aplica las migraciones de EF Core contra la base de datos de producción.

.DESCRIPTION
  Crea (o actualiza) el esquema en la BD apuntada por appsettings.Production.json.
  Requiere el SDK de .NET completo (no solo el runtime) y la herramienta `dotnet-ef`
  — si no la tienes: dotnet tool install --global dotnet-ef

  Ejecutar UNA sola vez cuando la BD del servidor esté vacía, y de nuevo cada vez que
  se publique una versión con migraciones nuevas.

.EXAMPLE
  .\deploy\apply-migrations.ps1
#>
$ErrorActionPreference = 'Stop'
$repoRoot = Split-Path -Parent $PSScriptRoot

Write-Host "Aplicando migraciones contra la conexión de appsettings.Production.json..." -ForegroundColor Cyan

$env:ASPNETCORE_ENVIRONMENT = 'Production'
dotnet ef database update `
    --project (Join-Path $repoRoot 'src\BomberosAPI.Infrastructure') `
    --startup-project (Join-Path $repoRoot 'src\BomberosAPI.API')

if ($LASTEXITCODE -ne 0) {
    throw "dotnet ef database update falló (código $LASTEXITCODE). Revisa la connection string en appsettings.Production.json."
}

Write-Host "Migraciones aplicadas correctamente." -ForegroundColor Green
