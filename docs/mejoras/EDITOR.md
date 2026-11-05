# Editor

**Prioridad:** P1
**Estado:** Activo
**Dueño:** editor

## Objetivo

Un editor cómodo y seguro para trabajo diario.

## Estado actual (6.278.0)

- Launcher, inspector, jerarquía, files panel y viewport.
- Editor de grafo con paleta, inspector, validación, diff y breakpoints.
- Undo/redo con `SceneHistory`.
- Prefabs, scripts, reproducción y recarga en caliente.
- Diálogo de errores de compilación.

## Funciones nuevas

- [ ] Undo/redo en todo el inspector.
- [ ] Edición multi-entidad.
- [ ] Buscador global y atajos completos.
- [ ] Inspector genérico sin campos truncados.
- [ ] Tests de UI.
- [ ] Editor visual de UI.
- [ ] Editor de tileset.
- [ ] Timeline de animación.

## Limitaciones conocidas

- Undo/redo no cubre todo el inspector.
- Multi-edición limitada.
- Buscador/atajos básicos.
- Inspector con campos limitados.
- Sin tests de UI.

## Criterios de aceptación

1. Undo/redo consistente.
2. Multi-edición.
3. Buscador y atajos.
4. Inspector sin desbordes.
5. Pruebas de historial.

## Dependencias

Editor y ViewModels.

## Riesgos

Cambios de UI pueden romper atajos; mantener tests de humo.
