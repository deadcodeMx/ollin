# Genera el instalador de Ollin y, opcionalmente, publica el release en GitHub.
# Uso:
#   powershell -ExecutionPolicy Bypass -File tools/release.ps1
#   powershell -ExecutionPolicy Bypass -File tools/release.ps1 -Publish

param(
    [switch]$Publish
)

$ErrorActionPreference = 'Stop'
$root = Split-Path -Parent $PSScriptRoot
Push-Location $root

try {
    $props = Get-Content 'Directory.Build.props' -Raw
    if ($props -notmatch '<Version>([^<]+)</Version>') {
        throw 'No se encontro <Version> en Directory.Build.props'
    }

    $version = $Matches[1]
    $installer = 'dist/installer/Ollin-Setup-' + $version + '-beta.exe'

    Write-Host ('Ollin ' + $version + ' - compilando') -ForegroundColor Cyan

    Remove-Item -Recurse -Force 'dist/app' -ErrorAction SilentlyContinue
    dotnet publish 'src/Ollin.Editor/Ollin.Editor.csproj' -c Release -r win-x64 --self-contained false -o 'dist/app' -v q --nologo
    if ($LASTEXITCODE -ne 0) { throw 'Fallo la publicacion del editor' }

    dotnet publish 'src/Ollin.Player/Ollin.Player.csproj' -c Release -r win-x64 --self-contained false -o 'dist/app/player' -v q --nologo
    if ($LASTEXITCODE -ne 0) { throw 'Fallo la publicacion del reproductor' }

    Copy-Item -Recurse -Force assets 'dist/app'
    Copy-Item -Recurse -Force scripts 'dist/app'
    Copy-Item -Force README.md, LICENSE, CHANGELOG.md 'dist/app'
    Compress-Archive -Path 'dist/app/*' -DestinationPath 'tools/payload.zip' -Force

    dotnet publish 'tools/Ollin.Setup/Ollin.Setup.csproj' -c Release -r win-x64 --self-contained false -p:PublishSingleFile=true -o 'dist/installer' -v q --nologo
    if ($LASTEXITCODE -ne 0) { throw 'Fallo la publicacion del instalador' }

    Copy-Item -Force 'dist/installer/Ollin-Setup.exe' $installer

    $size = '{0:N1} MB' -f ((Get-Item $installer).Length / 1MB)
    Write-Host ('Instalador: ' + $installer + ' (' + $size + ')') -ForegroundColor Green

    if (-not $Publish) {
        Write-Host ''
        Write-Host 'Para publicarlo en GitHub (instala GitHub CLI si no lo tienes):' -ForegroundColor Yellow
        Write-Host ('  gh release create v' + $version + ' ' + $installer + ' --title "Ollin ' + $version + ' beta" --notes-file docs/RELEASE-NOTES.md')
        Write-Host 'O subelo a mano desde Releases -> Draft a new release.'
        return
    }

    $gh = Get-Command gh -ErrorAction SilentlyContinue
    if (-not $gh) {
        throw 'GitHub CLI (gh) no esta instalado. Instalalo con: winget install GitHub.cli'
    }

    gh release create ('v' + $version) $installer --title ('Ollin ' + $version + ' beta') --notes-file docs/RELEASE-NOTES.md
    if ($LASTEXITCODE -ne 0) { throw 'Fallo la creacion del release' }

    Write-Host ('Release v' + $version + ' publicado') -ForegroundColor Green
}
finally {
    Pop-Location
}
