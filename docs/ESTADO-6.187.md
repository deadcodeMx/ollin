# Estado de Ollin 6.187.0

## Núcleo 2D

- ECS con `Entity`, `ParentId`, `Transform2D` y jerarquía.
- `CanvasItem2D`: visibilidad, `Modulate`, `SelfModulate`, `ZIndex`, Y-sort.
- `CanvasItem2D` aplicado a sprites, tilemaps, líneas, polígonos, meshes y skinning.
- `RenderServer2D` con display list.
- `PhysicsServer2D` con cuerpos, áreas y consultas.
- `ResourceLoader` con `res://`, caché y dependencias.
- `TileSet`, tilemaps y editor de TileSet.
- `Mesh2D`, `MultiMesh2D`, `SkinnedMesh2D`, `Skeleton2D`.
- `NavigationPolygon2D` y editor de puntos.
- `AnimationPlayer2D`, blend tree y editor de animación.
- `UiCanvas`, tema de UI, presets y editor visual.
- `SubViewport2D` con render-to-texture.

## Materiales y shaders

- `CanvasMaterial2D` con GLSL ES fragmentario.
- Parámetros `Vector4`, tinte y tiempo.
- Sin editor visual de shaders.

## Editores visuales

- TileSet.
- Animación.
- UI Canvas.
- Tema de UI.
- Mesh2D.
- SkinnedMesh2D.
- NavigationPolygon2D.

## Distribución

- Instalador Windows 6.187.0.
- Paquete Linux 6.187.0.
- Releases incrementales en GitHub.

## Verificación

- **236/236 pruebas**.
- Build: **0 errores**.
- SubViewport validado con dos cámaras y borde visible.
- `RenderTarget2D` restaura framebuffer y textura anteriores.

## Límites conocidos

- Linux no probado en hardware real.
- El backend de audio Linux no está validado en hardware.
- No hay editor visual de shaders.
- Multijugador y predicción son básicos.
- 2.5D/3D está oculto; el foco es 2D.
- No se copió código de Godot; solo se estudió su arquitectura.

## Evidencia

- Release: https://github.com/deadcodeMx/ollin/releases/tag/v6.187.0
- Commit de SubViewport: `b39f631`
- Commit de Linux: `5791f80`
