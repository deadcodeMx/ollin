# Beta 0.42.0 de Ollin

Compilación de prueba para instalar y usar el motor fuera del repositorio. Cierra la
etapa 5 del roadmap (herramientas de juego 2D).

## Qué incluye

- **Lanzador de proyectos** con recientes, crear y abrir carpeta.
- **Tres temas** de interfaz: oscuro, claro y Ollin (colores del logo).
- Editor con proyectos independientes, escenas múltiples y explorador de archivos.
- Viewport OpenGL ES 3.0, gizmos, deshacer/rehacer y **ventana de juego** aparte
  (pantalla completa con F11 en otro monitor).
- Scripts en C# con resaltado, autocompletado y recarga en caliente.
- **Diálogos**, interacción con E, etiquetas y **HUD** para scripts.
- **Luces 2D**, ambiente, viñeta, paralaje y cámara con seguimiento.
- **Inventario**, **estadísticas**, **ciclo día/noche**, **IA con A\*** y **guardado de
  partida**.
- **Prefabs** y **blueprints** (entidad + jerarquía completa).
- **Plantilla Tutorial** guiada (HUD, inventario, diálogo, IA y guardado).
- **Exportar juego** en un único ejecutable, con `game.pak` cifrado y scripts
  compilados (sin código fuente), pantalla de inicio y marca de agua opcionales.
- 119 pruebas automatizadas.

## Requisitos

- Windows 10/11 de 64 bits.
- **.NET 8 Desktop Runtime** ([descarga](https://dotnet.microsoft.com/download/dotnet/8.0)).
  El instalador no lo incluye; si falta, el programa no arranca.
- GPU con OpenGL 3.0 o superior.

## Instalar

1. Ejecuta `Ollin-Setup-0.42.0-beta.exe`.
2. Pulsa **Instalar**. Se copia en `%LOCALAPPDATA%\Ollin`.
3. Se crean accesos directos en el escritorio y en el menú Inicio.
4. Abre Ollin y elige un proyecto en el lanzador, o crea uno con la plantilla
   **Tutorial** para ver todo lo que puede hacer el motor.

Para desinstalar: **Configuración → Aplicaciones → Ollin → Desinstalar**, o ejecuta
`Desinstalar.exe` dentro de la carpeta de instalación (también admite
`--uninstall --silent`).

## Primeros pasos

1. Crea un proyecto con la plantilla **Tutorial**.
2. Pulsa **Reproducir** (F5): verás HUD, nota interactiva (E), moneda y un
   merodeador con IA.
3. Abre `scripts/Tutorial.cs` para ver movimiento, estadísticas, inventario,
   interacción y guardado comentados.
4. Prueba `Escena → Guardar partida`, cierra y `Cargar partida`.

## Limitaciones conocidas

- El instalador no está firmado: Windows puede mostrar SmartScreen
  («Más información → Ejecutar de todas formas»).
- Requiere .NET 8 instalado (no es autocontenido para mantener el instalador pequeño).
- Las **sombras 2D** son experimentales y están desactivadas por defecto.
- No hay clima ni crafteo como sistemas del motor; el crafteo se resuelve en scripts
  (ejemplo en la guía).
- El guardado incluye la escena y `Save.Set/Get`, pero no variables internas de scripts
  que no uses `Save`.
- Sin audio posicional (el audio es por canales de efecto/música).

## Cómo se genera la beta

```powershell
powershell -ExecutionPolicy Bypass -File tools/release.ps1
```

Ese script publica la aplicación, comprime la carga útil, genera el instalador y deja
`dist/installer/Ollin-Setup-<versión>-beta.exe`. Con `-Publish` y GitHub CLI crea el
release automáticamente.

Equivalente manual:

```powershell
dotnet publish src/Ollin.Editor -c Release -r win-x64 --self-contained false -o dist/app
Copy-Item -Recurse assets, scripts dist/app
Copy-Item README.md, LICENSE, CHANGELOG.md dist/app
Compress-Archive -Path dist/app/* -DestinationPath tools/payload.zip -Force
dotnet publish tools/Ollin.Setup -c Release -r win-x64 --self-contained false `
  -p:PublishSingleFile=true -o dist/installer
```

## Prueba automática del instalador

```powershell
$setup = "dist/installer/Ollin-Setup-0.42.0-beta.exe"
Start-Process $setup -ArgumentList "--silent" -Wait
Start-Process "$env:LOCALAPPDATA/Ollin/Desinstalar.exe" -ArgumentList "--uninstall --silent" -Wait
```
