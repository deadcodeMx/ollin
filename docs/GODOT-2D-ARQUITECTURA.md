# Arquitectura 2D de Ollin inspirada en Godot

Este documento fija el enfoque: **no se copia código de Godot**. Se estudian sus
responsabilidades y se implementan equivalentes propios sobre el modelo ECS de
Ollin.

## Lo que hace Godot

Godot organiza el 2D en capas:

- **Object/Node**: ciclo de vida, árbol, grupos, señales, escena.
- **CanvasItem**: visibilidad, modulación, Z-index, Y-sort, material, dibujo.
- **Node2D**: transform 2D local/global, rotación, escala, skew.
- **Viewport**: cámara, canvas transform, input, mundo 2D, render target.
- **CanvasLayer**: capa de canvas independiente del mundo.
- **Resource**: texturas, tilesets, escenas empaquetadas, animaciones.
- **Servers**: RenderingServer, PhysicsServer2D, AudioServer, NavigationServer2D.

Referencias del repositorio oficial:

- [`scene/2d/node_2d.h`](https://github.com/godotengine/godot/blob/master/scene/2d/node_2d.h)
- [`scene/main/canvas_item.h`](https://github.com/godotengine/godot/blob/master/scene/main/canvas_item.h)
- [`scene/main/viewport.h`](https://github.com/godotengine/godot/blob/master/scene/main/viewport.h)

## Equivalencias en Ollin

| Godot | Ollin |
|---|---|
| `Node` / escena | `Entity` + `Scene` + `ParentId` |
| `Node2D` | `Entity.Transform` + `Scene.GetWorldMatrix` |
| `CanvasItem` | `CanvasItem2D` (visibilidad, modulación, Z, Y-sort) |
| `CanvasLayer` | `CanvasLayer` |
| `Viewport` / cámara | `Camera2D` + `Camera2D.Viewport` |
| `Resource` | `TileSet`, texturas y assets JSON |
| `SceneTree` | `Scene` + `SceneRuntime` |
| Scripts | `Ollin.Scripting` (Roslyn) |

## Decisión de diseño

Ollin usa **ECS** en lugar de un árbol de nodos con herencia:

- `Entity` es el nodo.
- `Component` es el comportamiento.
- `Scene` es el árbol.
- `SceneRuntime` es el bucle.

Esto evita el acoplamiento de `Node2D`/`CanvasItem`/`Control` en una sola
jerarquía, pero exige que cada capacidad tenga su componente y su sistema.

## Estado actual

Implementado:

- `CanvasItem2D`.
- `CanvasLayer`.
- `Camera2D` con viewport, pixel-perfect, zoom y follow.
- Tilemap, TileSet, capas, autotile, navegación y oclusión.
- Física 2D, joints, materiales, raycast/shapecast.
- Animación con timeline, eventos, easing, Hermite y blend tree.
- UI runtime con anclajes, contenedores, listas, foco, accesibilidad y rich text.
- Audio 2D, red UDP, skinning y profiler.

Implementado en recursos:
- `OllinResource`, `TileSetResource`, cache y dependencias.

Pendiente:
- Editor visual de UI completo (drag/drop, contenedores).


- Servidores desacoplados (render/physics/audio/navigation).
- `RenderServer2D` con display list de sprites.
- `PhysicsServer2D` con áreas y señales.
- `SubViewport2D` con segunda cámara y rectángulo.
- `CanvasMaterial2D` con shader GLSL y cache.
- Recursos empaquetados con `ResourceLoader`.
- Recursos empaquetados con `ResourceLoader`.
- Render-to-texture y viewport como textura.
- Shaders completos: uniforms, hot-reload y materiales por capa.
- Física de áreas/consultas completas.
- Editor visual de UI, mallas, pesos y navegación.

## Fases siguientes

1. **ResourceLoader**: recursos con GUID, dependencias y cache.
2. **RenderServer2D**: desacoplar el renderer de la escena.
3. **PhysicsServer2D**: cuerpos, formas, áreas y consultas.
4. **Viewport/SubViewport**: render-to-texture y múltiples cámaras.
5. **CanvasMaterial**: shaders y blend modes completos.
6. **Editor de UI**: jerarquía de controles y temas.
7. **Editor de skinning/navegación**: gizmos y pesos.
