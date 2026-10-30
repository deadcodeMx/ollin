# Historial de cambios

Formato basado en [Keep a Changelog](https://keepachangelog.com/es-ES/1.1.0/) y
versionado [SemVer](https://semver.org/lang/es/).

## [No publicado]

### Cambiado

- Roadmap reorientado a un motor personal para terror lineal y supervivencia, con
  puente 2.5D antes del 3D y etapas reordenadas (4–8).

### Planeado

- Animación por sprites, tilemaps, física 2D y audio.
- Luces 2D, triggers y diálogos para terror.
- Puente 2.5D y 3D.
- Exportación del juego a un ejecutable de Windows.

## [0.3.0] — 2026-10-30

### Añadido

- Proyecto `Ollin.Scripting` con compilación en memoria mediante Roslyn.
- Clase base `OllinScript` con `OnStart`, `OnUpdate` y `OnDestroy`.
- `ScriptWorkspace` con `AssemblyLoadContext` recolectable para recargar scripts.
- Recarga en caliente al guardar un script **sin detener** la reproducción, conservando
  los campos compatibles y llamando a `OnReload`.
- Vigilancia automática de la carpeta `scripts/` para recompilar al guardar.
- API de scripts `Input` (teclado y ratón) y `Time` (`DeltaTime`, `Elapsed`, `Frame`).
- `Warn` y `Error` en la API de depuración de scripts.
- Sección de script en el inspector y creación de plantillas desde el editor.
- Ciclo de vida de scripts en `SceneRuntime`, con aislamiento de errores.
- Errores de compilación en la consola con archivo, línea y código (por ejemplo `CS0029`).
- Script de ejemplo `PlayerBob` enlazado al jugador de la escena por defecto.
- Seis pruebas del compilador y del workspace, más seis de entrada, tiempo y recarga en caliente (47 en total).

### Corregido

- La consola no seguía los mensajes nuevos (desplazamiento diferido al final del layout).
- Un proyecto sin scripts se reportaba como error de compilación.

## [0.2.0] — 2026-10-30

### Añadido

- Proyecto `Ollin.Runtime` con reloj, pausa y restauración exacta de la escena.
- Controles Reproducir, Pausar y Detener (F5/F6) con bloqueo de la edición.
- Componentes animables `RotatorComponent` y `OscillatorComponent`.
- Pipeline de assets: panel con miniaturas, importación y asignación de texturas.
- `ProjectLocator` y rutas relativas para que el proyecto se pueda mover.
- Gizmos de mover, rotar y escalar en el viewport, con ajuste manteniendo `Ctrl`.
- Historial de deshacer/rehacer con agrupación de ediciones continuas.
- Sprites de ejemplo (héroe, caja, moneda, suelo).

### Corregido

- El gizmo no registraba el paso de deshacer al soltar el ratón.
- El inspector y el gizmo se retroalimentaban generando pasos intermedios.

## [0.1.0] — 2026-10-30

### Añadido

- Solución con `Ollin.Core`, `Ollin.Rendering` y `Ollin.Editor`.
- Modelo de escena con jerarquía, componentes y serialización JSON.
- Viewport OpenGL ES 3.0 con sprite batch, cuadrícula y ejes.
- Cámara ortográfica con paneo, zoom y encuadre.
- Jerarquía, inspector en vivo, consola y barra de estado.
- Selección por árbol y por clic, con contorno de selección.
- Guardar y abrir escenas, marca de cambios sin guardar.
- 17 pruebas del núcleo.
