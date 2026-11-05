# Mejoras futuras del motor

Documentos vivos para planificar el trabajo que no forma parte del cierre actual.
Cada documento describe estado, limitaciones, prioridad, criterios de aceptación y
dependencias. No son promesas de fecha; son una cola de trabajo revisable.

## Reglas

- Una mejora por documento.
- Prioridad `P0` = bloquea uso real; `P1` = importante; `P2` = pulido.
- Cada documento debe poder cerrarse con pruebas y criterio verificable.
- Si una mejora rompe compatibilidad, se indica en el documento.
- No se marca como hecha sin build 0 errores y pruebas verdes.

## Índice

| Documento | Área | Prioridad | Estado |
|---|---|---|---|
| [GRAFO.md](GRAFO.md) | Grafo visual / codegen | P0 | Pendiente |
| [SCRIPTING.md](SCRIPTING.md) | API de scripting | P1 | Pendiente |
| [FISICA.md](FISICA.md) | Física 2D | P1 | Pendiente |
| [UI.md](UI.md) | UI Canvas / editor | P1 | Pendiente |
| [TILEMAP.md](TILEMAP.md) | Tilemap y autotile | P1 | Pendiente |
| [ANIMACION.md](ANIMACION.md) | Animación y esqueletos | P1 | Pendiente |
| [AUDIO.md](AUDIO.md) | Audio 2D y Linux | P1 | Pendiente |
| [EDITOR.md](EDITOR.md) | Editor, undo y flujo | P1 | Pendiente |
| [RENDIMIENTO.md](RENDIMIENTO.md) | Render, batching y pruebas | P1 | Pendiente |
| [RED.md](RED.md) | Multijugador y RPC | P2 | Pendiente |
| [LINUX.md](LINUX.md) | Linux real y empaquetado | P1 | Pendiente |
| [JUEGO-VALIDACION.md](JUEGO-VALIDACION.md) | Juego de validación | P1 | Pendiente |

## Orden recomendado

1. Grafo P0: variables, parámetros y codegen fiable.
2. Pruebas de compilación del código generado.
3. Física y UI: lo que más afecta a juegos reales.
4. Tilemap/animación/audio.
5. Editor y rendimiento.
6. Red y Linux.
7. Juego de validación completo.

## Definición de terminado

Un documento se considera cerrado cuando:

- Build de la solución en 0 errores.
- Pruebas verdes y nuevas pruebas del área.
- Documentación actualizada.
- Sin limitaciones conocidas del documento sin marcar.
- Si aplica, instalador y release publicados.
