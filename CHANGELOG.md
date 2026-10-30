# Historial de cambios

Formato basado en [Keep a Changelog](https://keepachangelog.com/es-ES/1.1.0/) y
versionado [SemVer](https://semver.org/lang/es/).

## [No publicado]

### Cambiado

- Roadmap reorientado a un motor personal para terror lineal y supervivencia, con
  puente 2.5D antes del 3D y etapas reordenadas (4–8).

### Planeado

- Audio 2D y prefabs.
- Luces 2D, triggers y diálogos para terror.
- Puente 2.5D y 3D.
- Exportación de un juego a un ejecutable.

## [0.11.0] — 2026-10-30

### Añadido

- **Luces 2D**: componente `Light2D` puntual o en cono, con color, radio, intensidad,
  ángulo y parpadeo. Se dibujan de forma aditiva sobre la escena.
- **Luz ambiente** por cámara: oscurece lo que no está iluminado.
- **Viñeta** sutil en el viewport para dar profundidad.
- **Cámara con seguimiento**: `FollowTag` y suavizado (por defecto sigue a `player`).
- **Paralaje** en sprites: factor 0 (fijo a la cámara) a 1 (mundo).
- Luces de ejemplo en la escena de demostración y en las plantillas (linterna en Terror).
- 5 pruebas nuevas: 88 en total.

### Pendiente

- Sombras por oclusión y post-proceso (niebla, grano, aberración cromática).

## [0.10.0] — 2026-10-30

### Añadido

- **Sistema de diálogos**: archivos `dialogs/<id>.json`, base de datos por proyecto,
  reproductor línea a línea y API `Dialogue.Play(id)` para scripts.
- **Interacción con E**: componente `Interactable` con aviso, radio, diálogo y uso
  único; el runtime busca al jugador por la etiqueta `player`.
- **Etiquetas de entidad** para identificar jugador, enemigos y objetos.
- **Interfaz de diálogo** en el viewport durante la reproducción (hablante, texto y
  aviso de continuación).
- **Callback `OnInteract`** para scripts y notas interactivas en las plantillas.
- Carpeta `dialogs/` en los proyectos nuevos con un diálogo de bienvenida.
- 11 pruebas nuevas: 83 en total.

## [0.9.0] — 2026-10-30

### Añadido

- Logo del motor: icono de la aplicación, del instalador y de la ventana, y logo en
  el diálogo «Acerca de» y en el README.
- Guía de uso completa en `docs/GUIA.md`: interfaz, componentes, scripts, audio,
  prefabs, atajos, problemas comunes y flujo recomendado.
- Sección de descargas en el README y notas de versión para GitHub Releases.
- Script `tools/release.ps1` que compila el instalador y publica el release (con `gh`).
- Iconos generados a partir del logo en `docs/images/` y `src/Ollin.Editor/Assets/`.

### Corregido

- El instalador mostraba la versión 0.4.0: ahora la lee del ensamblado, igual que el
  diálogo «Acerca de» y las notas del registro de Windows.
- El diálogo de proyecto nuevo tiene desplazamiento y altura mínima; sus botones ya no
  se pierden en pantallas pequeñas.
- El mismo diálogo ya no se cierra al fallar la validación y evita un bloqueo al
  envolver su contenido.

## [0.8.0] — 2026-10-30

### Añadido

- Arrastrar scripts desde el explorador a una entidad (jerarquía o viewport) para
  enlazarlos, y prefabs para instanciarlos en el punto de destino.
- Menú contextual de entidad: crear hijo, añadir componente, duplicar, enfocar,
  guardar como prefab y eliminar.
- Botones para añadir componentes desde el inspector y quitar el script de una entidad.
- Eliminar archivos del proyecto con clic derecho en el explorador (con confirmación).
- Doble clic en la jerarquía centra la cámara en la entidad.
- Autocompletado C# en el editor: `Ctrl+Espacio` y tras `.`, con palabras clave,
  tipos, API, entidades de la escena y clases de script.
- 2 pruebas del catálogo de autocompletado: 76 en total.

## [0.7.1] — 2026-10-30

### Corregido

- El diálogo de proyecto nuevo valida nombre y carpeta en línea y ya no se cierra
  perdiendo los datos si la carpeta está ocupada.
- Guardar un prefab como `torreta.json` producía `torreta.json.prefab.json`;
  ahora la extensión se normaliza siempre a `.prefab.json`.
- Cerrar una escena con cambios pide confirmación en lugar de no hacer nada.
- No se puede cambiar ni cerrar una escena mientras se reproduce (evita desincronizar
  la escena del runtime).
- El pintado de tilemap funciona arrastrando y avisa si no hay un tilemap seleccionado.
- Las pestañas de script se sincronizan con la recarga en caliente cuando el archivo
  cambia por fuera y no hay cambios sin guardar.
- 6 pruebas nuevas de regresión: 74 en total.

## [0.7.0] — 2026-10-30

### Añadido

- Audio 2D con NAudio: componente `AudioSource`, canales de efecto y música,
  fundidos y API `Audio.Play` / `Audio.Music` para scripts. Sonidos de ejemplo.
- Prefabs `*.prefab.json`: guardar la entidad seleccionada e instanciarla desde el
  explorador de archivos.
- Copiar y pegar entidades con `Ctrl+C` / `Ctrl+V`.
- Buscador de entidades en la jerarquía.
- Plantillas de proyecto: Vacío, Plataformas y Terror, con escena y script propios.
- Colisión por celda en tilemaps y cuerpos cinemáticos.
- 8 pruebas nuevas (prefabs, audio, colisión por celda y plantillas): 68 en total.

## [0.6.0] — 2026-10-30

### Añadido

- Resaltado de sintaxis C# en el editor integrado, con numeración de líneas y tema
  oscuro propio (AvaloniaEdit).
- Escenas múltiples: cada pestaña conserva su escena, cámara, selección e historial.
- Pestañas de escena numeradas, cerrables y reutilizables al abrir la misma escena.

### Corregido

- El editor de código no cargaba el texto si el script se abría antes de crear la ventana.

## [0.5.0] — 2026-10-30

### Añadido

- Sistema de proyectos: crear, abrir y cerrar proyectos con carpetas `scenes`,
  `scripts`, `assets` y `prefabs`, archivo `project.ollin` y contenido inicial.
- Recordatorio del último proyecto y pantalla de bienvenida con acciones.
- Pestañas de documentos en el área central (escena + scripts abiertos).
- Editor de scripts integrado con guardado (`Ctrl+S`) y recarga en caliente.
- Dock inferior con pestañas **Consola** y **Archivos**, con explorador del proyecto
  que abre scripts, escenas y texturas con doble clic.
- Inspector reorganizado en pestañas: Entidad, Visual, Mundo, Tilemap y Cámara.
- Apertura por línea de comandos: `--project` y `--script`.
- 5 pruebas del sistema de proyectos (60 en total).

### Corregido

- El editor de scripts heredaba la altura fija del estilo global de `TextBox`.

## [0.4.0] — 2026-10-30

### Añadido

- `SpriteAnimator`: animación por hoja de sprites con cuadros, FPS, bucle y recorte.
- `Tilemap`: rejilla con tileset, redimensionado y pintado con el ratón en el viewport.
- Física 2D: `BoxCollider`, `Rigidbody2D` con gravedad, resolución AABB y disparadores.
- Avisos de física a los scripts: `OnCollisionEnter`, `OnTriggerEnter` y `OnTriggerExit`.
- Rutinas en scripts: `StartCoroutine` con `WaitForSeconds`.
- `DebugDraw.Line/Rect` para depurar desde los scripts.
- Hoja de animación del héroe y tileset de ejemplo; la escena por defecto usa tilemap.
- Instalador de Windows con accesos directos y desinstalador (`tools/Ollin.Setup`).
- Guía de la beta y de generación del instalador en `docs/BETA.md`.
- 8 pruebas nuevas (55 en total).

### Corregido

- Los tilemaps nuevos nacían con el mosaico 0 en todas las celdas.
- El recorte de animación no se aplicaba en modo edición (se veía la hoja completa).
- El desinstalador no podía borrar su propia carpeta; ahora se relanza desde `%TEMP%`.

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
