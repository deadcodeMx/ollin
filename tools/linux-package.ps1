# Genera el paquete Linux x64 de Ollin (framework-dependent).
# Uso: powershell -ExecutionPolicy Bypass -File tools/linux-package.ps1

$ErrorActionPreference = 'Stop'
$root = Split-Path -Parent $PSScriptRoot
Push-Location $root

try {
    $props = Get-Content 'Directory.Build.props' -Raw
    if ($props -notmatch '<Version>([^<]+)</Version>') { throw 'No se encontro <Version> en Directory.Build.props' }
    $version = $Matches[1]
    $out = 'dist/linux'
    $app = Join-Path $out 'app'
    $archive = Join-Path $out ('Ollin-' + $version + '-linux-x64.tar.gz')

    Remove-Item -Recurse -Force $app -ErrorAction SilentlyContinue
    New-Item -ItemType Directory -Force $app | Out-Null

    Write-Host ('Ollin ' + $version + ' - publicando Linux x64') -ForegroundColor Cyan
    dotnet publish 'src/Ollin.Editor/Ollin.Editor.csproj' -c Release -r linux-x64 --self-contained false -o $app -v q --nologo
    if ($LASTEXITCODE -ne 0) { throw 'Fallo la publicacion Linux' }

    Copy-Item -Recurse -Force assets, scripts $app
    Copy-Item -Force README.md, LICENSE, CHANGELOG.md $app

    $launcher = @'
#!/usr/bin/env bash
set -e
cd "$(dirname "$0")"
exec ./Ollin.Editor "$@"
'@
    Set-Content (Join-Path $app 'Ollin') $launcher -Encoding UTF8 -NoNewline
    Set-Content (Join-Path $app 'LEEME-LINUX.txt') @'
Ollin para Linux x64
====================

Requisitos:
- .NET 8 Desktop Runtime.
- OpenGL 3.0 o superior.
- Audio: paplay (PulseAudio), aplay (ALSA) o ffplay.

Ejecutar:
  chmod +x Ollin Ollin.Editor
  ./Ollin --project /ruta/proyecto/project.ollin

El backend de audio Linux usa el reproductor disponible. paplay/aplay no
permiten cambiar el volumen por proceso; ffplay acepta volumen inicial.

Limitacion conocida: este paquete se genera en Windows y no se ha validado
en hardware Linux en este entorno.
'@ -Encoding UTF8 -NoNewline

    Remove-Item $archive -Force -ErrorAction SilentlyContinue
    tar -czf $archive -C $app .
    if ($LASTEXITCODE -ne 0) { throw 'Fallo el empaquetado tar.gz' }
    $size = '{0:N1} MB' -f ((Get-Item $archive).Length / 1MB)
    Write-Host ('Paquete Linux: ' + $archive + ' (' + $size + ')') -ForegroundColor Green
}
finally {
    Pop-Location
}
