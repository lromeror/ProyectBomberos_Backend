<#
.SYNOPSIS
  Publica BomberosAPI.API en modo Release, listo para copiar/ejecutar en el servidor.

.DESCRIPTION
  Genera un build self-contained-ready (usa el runtime instalado en la máquina, no
  incluye el propio .NET runtime) en la carpeta de salida indicada. Ejecutar esto EN el
  servidor (o en cualquier máquina y luego copiar la carpeta de salida al servidor).

.PARAMETER OutputPath
  Carpeta donde queda el resultado publicado. Por defecto: .\publish junto a este script.

.EXAMPLE
  .\deploy\publish.ps1
  .\deploy\publish.ps1 -OutputPath 'C:\BomberosAPI\app'
#>
param(
    [string]$OutputPath = (Join-Path $PSScriptRoot 'publish')
)

$ErrorActionPreference = 'Stop'
$repoRoot = Split-Path -Parent $PSScriptRoot
$project = Join-Path $repoRoot 'src\BomberosAPI.API\BomberosAPI.API.csproj'

Write-Host "Publicando $project -> $OutputPath" -ForegroundColor Cyan

dotnet publish $project -c Release -o $OutputPath --self-contained false

if ($LASTEXITCODE -ne 0) {
    throw "dotnet publish falló (código $LASTEXITCODE)"
}

Write-Host ""
Write-Host "Listo. Antes de instalar el servicio:" -ForegroundColor Green
Write-Host "  1. Copia '$OutputPath\appsettings.Production.json' y edítalo con los datos reales del servidor"
Write-Host "     (connection string, dominio/IP para CORS) — el que trae el repo son solo placeholders."
Write-Host "  2. Aplica las migraciones: .\deploy\apply-migrations.ps1"
Write-Host "  3. Instala el servicio: .\deploy\install-service.ps1 -AppPath '$OutputPath'"
