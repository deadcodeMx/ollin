# Editor

**Prioridad:** P1
**Estado:** Completado — 7.0.0-beta.15
**Dueño:** editor

## Objetivo

Un editor cómodo y seguro para trabajo diario.

## Estado actual (7.0.0-beta.15)

- Historial de escena por instantáneas con límite configurable.
- Deshacer/rehacer con etiqueta visible.
- Las ediciones del inspector se agrupan por propiedad.
- Las operaciones por lotes (mover, rotar, escalar, ordenar, alinear y distribuir) generan **una sola entrada** de historial.
- Edición multi-entidad con selección múltiple.
- Duplicado simple y en serie.
- Buscador de entidades en la jerarquía.
- **Buscador de propiedades en el inspector**.
- Atajos de deshacer/rehacer y guardado.
- Reordenar, copiar, pegar y eliminar.

## Funciones completadas

- [x] Historial consistente en inspector.
- [x] Agrupación de edición multi-entidad.
- [x] Duplicado y orden.
- [x] Buscador de entidades y propiedades.
- [x] Atajos.
- [x] Pruebas de historial.

## Verificación

- Build: 0 errores.
- Pruebas: 209/209.
- Historial: deshacer, rehacer, coalescencia y límite probados.

## Criterios de aceptación

1. Undo/redo consistente. ✅
2. Edición multi-entidad. ✅
3. Duplicado y orden. ✅
4. Buscador. ✅
5. Pruebas de historial. ✅

## Dependencias

Editor, historial y serialización.
