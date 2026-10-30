# Beta 0.15.0 de Ollin

Primera compilación de prueba para instalar y usar fuera del repositorio.

## Qué incluye

- Editor completo con escena de ejemplo (animación, tilemap, física y scripts).
- Scripts en C# con recarga en caliente (`assets/` y `scripts/` incluidos).
- Instalador con accesos directos y desinstalador.

## Requisitos

- Windows 10/11 de 64 bits.
- **.NET 8 Desktop Runtime** ([descarga](https://dotnet.microsoft.com/download/dotnet/8.0)).
  El instalador no lo incluye; si falta, el programa no arranca.

## Instalar

1. Ejecuta `Ollin-Setup-0.15.0-beta.exe`.
2. Pulsa **Instalar**. Se copia en `%LOCALAPPDATA%\Ollin`.
3. Se crean accesos directos en el escritorio y en el menú Inicio.
4. Al terminar se abre el editor.

Para desinstalar: **Configuración → Aplicaciones → Ollin → Desinstalar**, o ejecuta
`Desinstalar.exe` dentro de la carpeta de instalación (también se puede lanzar con
`--uninstall --silent`).

## Cómo se genera la beta

```powershell
# 1. Aplicación (dependiente del runtime, ~44 MB)
dotnet publish src/Ollin.Editor -c Release -r win-x64 --self-contained false -o dist/app
Copy-Item -Recurse assets, scripts dist/app
Copy-Item README.md, LICENSE, CHANGELOG.md dist/app

# 2. Carga útil comprimida (~17 MB)
Compress-Archive -Path dist/app/* -DestinationPath tools/payload.zip -Force

# 3. Instalador de un solo archivo
dotnet publish tools/Ollin.Setup -c Release -r win-x64 --self-contained false `
  -p:PublishSingleFile=true -o dist/installer
```

El resultado es `dist/installer/Ollin-Setup.exe`.

## Prueba automática del instalador

```powershell
$setup = "dist/installer/Ollin-Setup.exe"
Start-Process $setup -ArgumentList "--silent" -Wait          # instala
Start-Process "$env:LOCALAPPDATA/Ollin/Desinstalar.exe" -ArgumentList "--uninstall --silent" -Wait
```

## Limitaciones conocidas de esta beta

- Sin audio: el sonido llega en la siguiente entrega.
- Sin prefabs ni escenas múltiples todavía.
- El instalador no firma el ejecutable: Windows puede mostrar un aviso de SmartScreen.
- Requiere .NET 8 instalado (no es autocontenido para mantener el instalador pequeño).
