# Auditoría para recorte 2D

## Métricas (4 nov 2026)

- Motor (`src`): **33.660 líneas** (31.403 C# + 2.257 AXAML).
- Pruebas: **5.003**.
- Instalador: **434**.
- Script de ejemplo: **52**.
- **Total: 39.149 líneas**.

## Candidatos fuera del núcleo Godot 2D

### 3D
- 18 archivos, **2.176 líneas**.
- Integración en Renderer2D, ViewportControl, EntityNode, MainWindow y StarterProject.

### Sistemas de juego
- 23 archivos, **1.878 líneas**.
- Salud, inventario, objetos, diálogo, quests, combate, cartas, turnos, mazmorras, armas, proyectiles, HUD de juego.

### Node graph / visual script propio
- Núcleo: 539 líneas.
- Editor: 2.113 líneas.
- Tests asociados.

## Por qué no se borró en esta pasada

El 3D y los sistemas de juego están entrelazados con:

- Registro JSON de componentes.
- Inspector manual de `EntityNode`.
- Secciones XAML de `MainWindow`.
- `StarterProject`.
- Pruebas de etapa.
- Acciones del node graph.

Un borrado directo deja el repositorio sin compilar.

## Plan por fases

1. Extraer `ActionKind`/`ActionExecutor` genéricos.
2. Quitar 3D de Core, renderer y editor; borrar tests 3D.
3. Quitar sistemas RPG y sus secciones de inspector.
4. Quitar node graph o conservarlo como módulo opcional.
5. Reemplazar `StarterProject` por plantillas 2D mínimas.
6. Build, tests y nueva release.
