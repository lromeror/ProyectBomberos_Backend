<#
.SYNOPSIS
  Instala BomberosAPI.API como Windows Service (arranca solo, sin sesión abierta).

.DESCRIPTION
  Requiere ejecutarse como Administrador. Crea el servicio, lo configura para arrancar
  con Windows, fija ASPNETCORE_ENVIRONMENT=Production (vía el registro del servicio —
  es el mecanismo soportado, `New-Service` no acepta variables de entorno), abre el
  puerto en el firewall de Windows y arranca el servicio.

  Si el servicio ya existe (reinstalación tras publicar una versión nueva), lo
  reemplaza.

.PARAMETER AppPath
  Carpeta con el resultado de `dotnet publish` (ver publish.ps1).

.PARAMETER Port
  Puerto donde escucha Kestrel. Debe coincidir con appsettings.Production.json /
  ASPNETCORE_URLS. Por defecto 5054, el mismo que en desarrollo local.

.EXAMPLE
  .\deploy\install-service.ps1 -AppPath 'C:\BomberosAPI\app'
#>
param(
    [string]$AppPath = (Join-Path $PSScriptRoot 'publish'),
    [string]$ServiceName = 'BomberosAPI',
    [string]$DisplayName = 'BomberosAPI Backend',
    [int]$Port = 5054
)

$ErrorActionPreference = 'Stop'

$currentPrincipal = New-Object Security.Principal.WindowsPrincipal([Security.Principal.WindowsIdentity]::GetCurrent())
if (-not $currentPrincipal.IsInRole([Security.Principal.WindowsBuiltInRole]::Administrator)) {
    throw "Este script necesita ejecutarse como Administrador (clic derecho > Ejecutar como administrador en PowerShell)."
}

$exePath = Join-Path $AppPath 'BomberosAPI.API.exe'
if (-not (Test-Path $exePath)) {
    throw "No se encontró $exePath. Corre primero .\deploy\publish.ps1 (o pasa -AppPath con la carpeta correcta)."
}

if (Get-Service -Name $ServiceName -ErrorAction SilentlyContinue) {
    Write-Host "El servicio '$ServiceName' ya existe. Deteniéndolo para reinstalar..." -ForegroundColor Yellow
    Stop-Service -Name $ServiceName -Force -ErrorAction SilentlyContinue
    & sc.exe delete $ServiceName | Out-Null
    Start-Sleep -Seconds 2
}

New-Service -Name $ServiceName `
    -BinaryPathName "`"$exePath`" --urls http://0.0.0.0:$Port" `
    -DisplayName $DisplayName `
    -Description 'API de FireHealth / SMAB (BomberosAPI). Instalado por deploy/install-service.ps1.' `
    -StartupType Automatic | Out-Null

# ASPNETCORE_ENVIRONMENT no se puede pasar a New-Service directamente; se fija vía el
# valor "Environment" (REG_MULTI_SZ) del registro del propio servicio.
$regPath = "HKLM:\SYSTEM\CurrentControlSet\Services\$ServiceName"
Set-ItemProperty -Path $regPath -Name Environment -Value @('ASPNETCORE_ENVIRONMENT=Production') -Type MultiString

$fwRuleName = "BomberosAPI (puerto $Port)"
if (-not (Get-NetFirewallRule -DisplayName $fwRuleName -ErrorAction SilentlyContinue)) {
    New-NetFirewallRule -DisplayName $fwRuleName -Direction Inbound -Action Allow -Protocol TCP -LocalPort $Port | Out-Null
    Write-Host "Regla de firewall creada para el puerto $Port." -ForegroundColor Cyan
}

Start-Service -Name $ServiceName

Write-Host ""
Write-Host "Servicio '$ServiceName' instalado e iniciado en el puerto $Port." -ForegroundColor Green
Write-Host "Estado:    Get-Service $ServiceName"
Write-Host "Reiniciar: Restart-Service $ServiceName"
Write-Host "Logs:      Visor de eventos > Registros de Windows > Aplicación"
