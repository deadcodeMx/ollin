# Comparativa Godot 2D vs Ollin 2D

Referencia: documentación oficial de Godot 4 (stable) y estado de Ollin **6.118.0**.

- [Godot 2D](https://docs.godotengine.org/en/stable/tutorials/2d/index.html)
- [Lista de características](https://docs.godotengine.org/en/stable/about/list_of_features.html)
- [CharacterBody2D](https://docs.godotengine.org/en/stable/tutorials/physics/using_character_body_2d.html)
- [TileMaps](https://docs.godotengine.org/en/stable/tutorials/2d/using_tilemaps.html)
- [Luces y sombras 2D](https://docs.godotengine.org/en/stable/tutorials/2d/2d_lights_and_shadows.html)
- [Partículas 2D](https://docs.godotengine.org/en/stable/tutorials/2d/particle_systems_2d.html)
- [AnimationTree](https://docs.godotengine.org/en/4.2/classes/class_animationnodestatemachine.html)
- [Audio buses](https://docs.godotengine.org/en/stable/tutorials/audio/audio_buses.html)
- [Navegación 2D](https://docs.godotengine.org/en/stable/tutorials/navigation/navigation_overview_2d.html)
- [Internacionalización](https://docs.godotengine.org/en/stable/tutorials/i18n/internationalizing_games.html)

## Veredicto

Ollin ya tiene una **base jugable** para plataformas, acción y aventura 2D:
personaje, colisiones, cámara, combate, tilemap básico, partículas, HUD,
inventario, diálogos, misiones, guardado, Input Map y exportación.

No está cerca de la cobertura 2D de Godot. Las diferencias importantes no son
"más componentes", sino **motor de física, pipeline de render, herramientas de
tilemap/animación/UI y audio/navegación**.

## Estado por área

| Área | Godot 2D | Ollin 2D | Falta |
|---|---|---|---|
| Física | CharacterBody2D con pendientes, floor snap, plataformas móviles, CCD, joints, raycast/shapecast, consultas | Contactos suelo/pared/techo, pendientes por normal, floor snap, arrastre de plataforma, subpasos adaptativos, RayCast/ShapeCast y excepciones por tags | Joints, materiales, CCD swept real, consultas de área completas |
| Formas | Box, circle, capsule, convex/concave polygon, segment, world boundary | Box, circle, capsule aproximada, polygon, edge | Segmento, world boundary, convex/concave real, separation ray |
| Tilemap | TileMapLayer, TileSet, terrenos/autotile, colisiones/navegación/oclusión por tile, capas, Y-sort | TileMapLayer, TileSet inline, colisión por tile, autotile básico | Terrenos, navegación/oclusión por tile, Y-sort, editor de TileSet |
| Render 2D | CanvasLayer, shaders de canvas, blend modes, meshes, MultiMesh, Line2D/Polygon2D, Y-sort, viewports, pixel snap/AA | SpriteBatch propio, sprites, líneas, partículas, luces | Shaders, blend modes, meshes, CanvasLayer, Y-sort, viewports, antialiasing, pixel perfect |
| Cámara | Zoom, rotación, drag margins, límites, smoothing, viewport, pixel perfect | Follow, deadzone, bounds, zoom | Rotación, drag margins, pixel perfect, shake, transiciones entre cámaras |
| Animación | AnimationPlayer, AnimationTree, blend trees, method/audio tracks, SpriteFrames | SpriteAnimator + FSM simple | Timeline, tracks, blend tree, eventos, recurso de animación, editor visual |
| Luces | Point/directional, normal/specular maps, occluders, sombras suaves, SDF, CanvasModulate | Point/cone, flicker, sombras básicas | Direccional, normal maps, oclusores editables, sombras suaves, SDF, blend modes |
| Partículas | GPU/CPU, emission shapes, gradientes, colisión, subemitters, trails | Emisor CPU simple y Trail2D | GPU, formas, gradientes, colisión, subemitters, trails con mesh |
| Parallax | Parallax2D/Background con repeat, scroll, follow, autoscroll | `Parallax` escalar por sprite | Capas, repetición, scroll/follow/autoscroll, editor |
| UI | Control, anchors, containers, themes, RichText, focus, accesibilidad | HUD, pausa, inventario y menús concretos | Sistema general de UI, layouts, temas, fuentes, foco, accesibilidad |
| Audio | Buses/efectos, 2D positional, listener, Doppler, pitch, polyphony, PulseAudio/ALSA | AudioSource, música/efectos, mixer básico; Linux silencioso | Posicional 2D, buses/efectos, pitch, polyphony, backend Linux |
| Input | InputMap remapeable, deadzone, touch, mouse, 8 gamepads | Input Map con captura, deadzone, touch y XInput/js | Múltiples gamepads simultáneos |
| Navegación | NavigationRegion2D/Polygon, agentes, obstáculos, avoidance | A* sobre tilemap | Navmesh/polígono, agentes, obstáculos dinámicos, avoidance |
| Esqueleto/IK | Skeleton2D, Bone2D, skinning, IK | Huesos, preview e IK de dos huesos | Skinning, render de malla, IK de más huesos |
| Localización | CSV/gettext, plurales, contextos, RTL, fuentes del sistema | JSON plano | Plurales, contextos, RTL, fallback de fuentes, selector de idioma |
| Multijugador | MultiplayerSpawner/Synchronizer, RPC, ENet/WebSocket | No implementado | Todo |
| Herramientas | Editor de TileSet, animación, shaders, perfiles, inspector remoto | Editor de escenas, inspector, scripts, prefabs | Editores de TileSet/animación/shader, profiler, inspector remoto |

## Limitaciones actuales de la física de personaje (6.118.0)

Ya están implementados:

- Contactos de suelo, pared y techo.
- Pendiente máxima y normal de suelo.
- Floor snap.
- Arrastre por plataformas móviles.
- Subpasos adaptativos a alta velocidad.
- Coyote time, jump buffer y salto variable.

Todavía falta:

- Las pendientes solo funcionan si el collider aporta una normal; **el tilemap no tiene pendientes dedicadas**.
- El CCD son **subpasos adaptativos**, no un swept collision continuo.
- No hay `RayCast2D` ni `ShapeCast2D` para el personaje.
- No hay joints ni materiales de fricción/rebote.
- No hay excepciones de colisión por cuerpo.
- El floor snap es heurístico y puede engancharse a colliders dinámicos cercanos.
- `MovingPlatform2D` solo oscila en línea; no tiene waypoints, curva ni editor de recorrido.
- No hay pared-deslizamiento, escaleras ni plataformas atravesables desde abajo con lógica avanzada.
- El one-way por tile es básico.

## Limitaciones actuales de TileSet/TileMapLayer (6.119.0)

Ya están implementados:

- `TileSet` inline con textura, tamaño y columnas.
- `TileDefinition` con región, colisión, offset, one-way y tag.
- Varias capas `TileMapLayer` por entidad.
- Colisión y render por capa.
- Autotile básico de 16 vecinos.

Todavía falta:

- No hay editor visual de definiciones de `TileSet`; las colisiones por tile se editan en JSON.
- `TileSet` no es un asset independiente reutilizable; vive dentro de la capa.
- No hay terrenos ni reglas de autotile; solo la variante por vecinos.
- No hay navegación, oclusión ni datos personalizados por tile.
- No hay Y-sort entre capas.
- El inspector solo edita la primera capa de una entidad.
- No hay pintado por capa en el viewport.
- No hay TileSet de varios atlas ni tiles alternativos.

## Limitaciones actuales de Render, Animación y UI (6.120.0)

### Canvas
Implementado:
- `CanvasLayer` con orden, visibilidad y Y-sort de sprites.
- Orden de dibujo por capa.

Falta:
- Shaders de canvas (vertex/fragment/light).
- Blend modes por item.
- 2D meshes, `Line2D`, `Polygon2D` y MultiMesh.
- Viewports/CanvasLayer con cámara propia.
- Pixel perfect, antialiasing y snap.
- Y-sort para tilemaps, partículas y huesos.

### Animación
Implementado:
- `AnimationPlayer2D` con timelines, tracks de posición, rotación, escala, frame y audio.
- Reproducción, loop y velocidad.

Falta:
- Editor visual de timeline.
- Blend trees y transiciones.
- Tracks de método/eventos y curvas Bézier.
- Animación de propiedades arbitrarias del inspector.
- Previsualización en el editor.

### UI runtime
Implementado:
- `UiCanvas` con panel, etiqueta, botón y barra.
- Overlay en editor y juego exportado.
- Botones que escriben acciones en `GameState`.

Falta:
- Anchors, layouts y contenedores.
- Temas, fuentes y estilos.
- Listas, texto enriquecido, scroll y foco.
- Navegación con mando y accesibilidad.
- Editor visual de UI.

## Lista priorizada para "2D completo"

### P0 — Sin esto no se puede llamar motor 2D completo

1. **Física de personaje real**
   - Pendientes.
   - Floor snap.
   - Velocidad de plataforma y arrastre de pasajero.
   - CCD.
   - Colisión por capas y excepciones.
   - Materiales de fricción/rebote.

2. **Consultas físicas**
   - `RayCast2D`.
   - `ShapeCast2D`.
   - Consultas de área/solape desde script.
   - Joints (pin, groove, spring).

3. **TileSet/TileMapLayer**
   - Capas múltiples.
   - Terrenos/autotile por reglas.
   - Colisión, navegación y oclusión por tile.
   - Variantes y datos personalizados.
   - Y-sort.

4. **Render de canvas**
   - CanvasLayer.
   - Shaders de canvas (vertex/fragment/light).
   - Blend modes.
   - Y-sort.
   - 2D meshes / Line2D / Polygon2D.
   - Pixel perfect y antialiasing.

5. **Animación**
   - Timeline con tracks.
   - Method/audio tracks.
   - Blend trees.
   - Editor visual.
   - Eventos de animación.

6. **UI runtime general**
   - Layouts/anchors/containers.
   - Temas y fuentes.
   - Botones, listas, texto enriquecido.
   - Foco y navegación con mando.
   - Accesibilidad.

7. **Audio 2D**
   - Posicional con listener.
   - Buses y efectos.
   - Pitch/polyphony.
   - Backend Linux (PulseAudio/ALSA).

### P1 — Necesario para producción

8. **Navegación 2D**: navmesh, agentes, obstáculos, avoidance.
9. **Cámara completa**: rotación, drag margins, pixel perfect, shake.
10. **Luces avanzadas**: direccional, normal maps, oclusores, sombras suaves.
11. **Partículas GPU**: formas, gradientes, colisión, subemitters, trails.
12. **Parallax2D**: capas, repetición, follow/autoscroll.
13. **Input runtime**: captura de rebinding, touch, deadzone.
14. **Localización completa**: plurales, contextos, RTL, selector.
15. **Señales/grupos/event bus** para desacoplar scripts.

### P2 — Paridad de ecosistema

16. **Skeletal/IK real**.
17. **Multijugador** (transporte, autoridad, sincronización, RPC).
18. **Herramientas**: editor de TileSet, editor de animación, editor de shaders, profiler, inspector remoto.
19. **2D HDR/glow/post-proceso**.
20. **Accesibilidad y screen readers**.

## Definición de "2D completo" para Ollin

Se puede considerar completo cuando un juego 2D comercial medio pueda hacerse
sin escribir sistemas de motor:

- Plataformas con pendientes, one-way y plataformas móviles que arrastran.
- Tilemap con terrenos, colisión y navegación.
- Personaje con raycast/shapecast y combate.
- Animación con timeline/eventos/blend.
- UI de menús, ajustes, inventario y diálogo con mando.
- Audio posicional y buses.
- Navegación con obstáculos.
- Localización con plurales y RTL.
- Editor de tilemap/animación usable por no programadores.

## SubViewport2D

Validado en 6.187.0: render-to-texture, dos cámaras, overlay y borde. El fallo era que RenderTarget2D ligaba el framebuffer 0 al construirse y perdía el FBO de Avalonia.

