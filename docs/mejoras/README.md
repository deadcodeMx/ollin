# Documentos de desarrollo

Estos documentos son la guía de trabajo del motor. No son una lista de deseos:
cada área tiene estado actual, funciones nuevas, límites conocidos y criterios
verificables.

## Cómo se usan

1. Elegir un documento por prioridad.
2. Tomar una función nueva o una limitación.
3. Implementarla con pruebas.
4. Actualizar el documento y `CHANGELOG.md`.
5. Publicar solo cuando el bloque esté cerrado.

## Reglas

- Una tarea debe ser verificable.
- `P0` bloquea uso real; `P1` importante; `P2` pulido.
- No se marca como hecha sin build en 0 errores y pruebas verdes.
- Si cambia el C# generado o un formato, se anota compatibilidad.
- Las releases siguen [VERSIONADO.md](../VERSIONADO.md).

## Índice

| Documento | Área | Prioridad | Estado |
|---|---|---|---|
| [GRAFO.md](GRAFO.md) | Grafo visual / codegen | P0 | Completado 6.278 |
| [FEEDBACK-UI.md](FEEDBACK-UI.md) | Feedback de usuarios de la interfaz | P0 | Completado |
| [SCRIPTING.md](SCRIPTING.md) | API de scripting | P1 | Activo |
| [FISICA.md](FISICA.md) | Física 2D | P1 | Completado beta9 |
| [UI.md](UI.md) | UI Canvas / editor | P1 | Completado beta11 |
| [TILEMAP.md](TILEMAP.md) | Tilemap y autotile | P1 | Activo |
| [PREFABS.md](PREFABS.md) | Prefabs y blueprints | P1 | Activo |
| [GUARDADO.md](GUARDADO.md) | Guardado y estado | P1 | Activo |
| [DIALOGOS.md](DIALOGOS.md) | Diálogos | P1 | Activo |
| [RENDER.md](RENDER.md) | Render 2D | P1 | Activo |
| [ANIMACION.md](ANIMACION.md) | Animación y esqueletos | P1 | Activo |
| [AUDIO.md](AUDIO.md) | Audio 2D y Linux | P1 | Activo |
| [EDITOR.md](EDITOR.md) | Editor, undo y flujo | P1 | Activo |
| [RENDIMIENTO.md](RENDIMIENTO.md) | Render, batching y pruebas | P1 | Activo |
| [RED.md](RED.md) | Multijugador y RPC | P2 | Activo |
| [LINUX.md](LINUX.md) | Linux real y empaquetado | P1 | Activo |
| [JUEGO-VALIDACION.md](JUEGO-VALIDACION.md) | Juego de validación | P1 | Cerrado beta10 |
| [VALIDACION-2026-11-06.md](VALIDACION-2026-11-06.md) | Informe de la primera pasada | P1 | En curso |

## Orden recomendado

1. **Juego de validación**: revela qué falta de verdad. Es el siguiente bloque.
2. **UI**: lo que más bloquea jugabilidad tras física.
3. **Tilemap, animación y audio**.
4. **Editor y rendimiento**.
5. **Red y Linux**.
6. **Cierre de la beta**.

El grafo ya está cerrado como bloque; sus límites viven en `GRAFO.md` y no se
reabren sin una función nueva concreta.

## Plantilla

Usar [_PLANTILLA.md](_PLANTILLA.md) para documentos nuevos.

## Definición de terminado

- Build de la solución en 0 errores.
- Pruebas verdes y nuevas pruebas del área.
- Documento actualizado.
- `CHANGELOG.md` actualizado.
- Sin límites del documento sin marcar.
- Si aplica, instalador, release y notas publicadas.
