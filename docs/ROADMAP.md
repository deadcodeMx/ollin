# Roadmap de Ollin

Desarrollo por etapas: cada una entrega algo usable y verificable, sin dejar el editor
a medias. El orden está pensado para que el scripting en C# llegue cuando el motor ya
tiene algo que ejecutar.

## Etapa 0 — Cimientos (completada)

- Elección de stack: Avalonia (GUI) + OpenGL/Silk.NET (render) + .NET 8.
- Verificación del entorno: SDK, NuGet, contexto OpenGL ES 3.0 (ANGLE).
- Estructura de solución, `Directory.Build.props`, `.gitignore`.

## Etapa 1 — Editor funcional (completada)

**Objetivo:** poder crear y guardar una escena 2D viéndola en el viewport.

- [x] Ventana del editor con menú, barra de herramientas, paneles y barra de estado.
- [x] Tema oscuro propio (jade + ámbar) sobre Fluent.
- [x] Modelo de escena: `Scene`, `Entity`, `Component`, `Transform2D` con jerarquía.
- [x] Componentes `SpriteRenderer` y `Camera2D`.
- [x] Viewport OpenGL embebido con contexto propio y bucle de dibujo.
- [x] Sprite batch con tint, rotación, escala, origen y orden de dibujo.
- [x] Cuadrícula con ejes, líneas menores y mayores.
- [x] Cámara ortográfica: paneo, zoom anclado al cursor, encuadre.
- [x] Selección por árbol y por clic en el viewport, con contorno de selección.
- [x] Inspector con edición en vivo (transformación, sprite, cámara).
- [x] Guardar/abrir escena en JSON, marca de cambios sin guardar.
- [x] Consola de mensajes con niveles.
- [x] 17 pruebas del núcleo.

**Limitaciones conocidas:** sin modo Play, sin arrastre de gizmos en el viewport, sin
texturas de ejemplo (los sprites sin textura se dibujan con color sólido), sin
deshacer/rehacer.

## Etapa 2 — Runtime y edición directa (en curso)

**Objetivo:** que la escena "viva" y se pueda manipular con el ratón.

- [x] Bucle de juego separado (`Ollin.Runtime`): reloj, delta recortado, pausa.
- [x] Botón **Reproducir / Pausar / Detener** (F5 / F6) en el mismo viewport.
- [x] Restauración exacta del estado de la escena al detener (instantánea JSON).
- [x] Componentes animables de ejemplo: `RotatorComponent` y `OscillatorComponent`, editables en el inspector.
- [x] Bloqueo de la edición mientras se reproduce.
- [ ] Gizmos arrastrables: mover, rotar, escalar desde el viewport (snap con `Ctrl`).
- [ ] Deshacer/rehacer (pila de comandos sobre la escena).
- [ ] Panel de assets: importar PNG, arrastrar textura al sprite, vista de miniaturas.
- [ ] Exportación del juego a carpeta ejecutable (Windows).

> Nota de diseño: `ITickable` es el contrato que en la etapa 3 implementarán los
> scripts de usuario compilados con Roslyn, sin tocar el runtime.

## Etapa 3 — Scripting en C#

**Objetivo:** escribir comportamiento en C# con recarga en caliente.

- [ ] Compilación en memoria con **Roslyn** (`Microsoft.CodeAnalysis.CSharp`).
- [ ] Ciclo de vida: `Awake`, `Start`, `Update(dt)`, `OnDestroy`.
- [ ] API pública: `Entity`, `Transform`, `Input` (teclado/ratón), `Time`, `Debug`, `Scene`.
- [ ] Recarga en caliente: al guardar un `.cs` se recompila y se conservan los valores.
- [ ] Errores de compilación en la consola, con archivo y línea.
- [ ] Componente `ScriptComponent` que enlaza la entidad con su script.
- [ ] Plantillas al crear un script desde el editor.

## Etapa 4 — 2D completo

- [ ] Animación por sprites (secuencias, FPS, eventos de frame).
- [ ] Tilemaps con editor de capas y *autotiling* básico.
- [ ] Física 2D (cuerpos rígidos, colisiones, triggers) — Box2D o solver propio.
- [ ] Audio 2D (efectos y música) con mezcla por canales.
- [ ] UI de juego (texto, botones, barras) y fuentes bitmap.
- [ ] Escenas múltiples y transiciones.

## Etapa 5 — Juego real de punta a punta

- [ ] Prefabs y herencia de escena.
- [ ] Build de jugador con escenas embebidas y scripts compilados AOT/IL.
- [ ] Perfilador básico (FPS, draw calls, memoria, tiempo de scripts).
- [ ] Depuración de scripts con puntos de interrupción básicos.

## Etapa 6 — Multiplataforma y ecosistema

- [ ] Compilación y pruebas en Linux y macOS.
- [ ] Editor de extensiones/plugins.
- [ ] Documentación de API y tutoriales.
- [ ] Plantilla de proyecto de juego (`dotnet new ollin-game`).

## Criterio de "etapa terminada"

Una etapa se cierra cuando: compila sin errores, las pruebas pasan, la funcionalidad se
verificó ejecutando el editor y quedó documentada en este roadmap.
