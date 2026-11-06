# Audio

**Prioridad:** P1
**Estado:** Completado en Windows; Linux empaquetado sin validar en hardware — 7.0.0-beta.14
**Dueño:** runtime

## Objetivo

Audio estable en Windows y Linux, con mezcla y 2D.

## Estado actual (7.0.0-beta.14)

- `Audio`, `AudioSource` y `AudioSource2D`.
- Buses Master, Music, Effects y UI con volumen independiente.
- Música con instancia única por backend.
- Efectos 2D con atenuación y panning.
- `Fade` por handle en Windows.
- Backend Linux con `paplay`, `aplay` o `ffplay`.
- El volumen de `ffplay` se aplica al iniciar.
- **Corregido:** NAudio aplicaba Master dos veces.

## Funciones completadas

- [x] Mezclador y buses.
- [x] Volumen por categoría.
- [x] Audio 2D con atenuación/panning.
- [x] Música con instancia única.
- [x] Backend Linux seleccionable y documentado.
- [x] Pruebas de mezcla y audio 2D.

## Limitaciones conocidas

- `paplay`/`aplay` no permiten cambiar el volumen por proceso.
- No hay validación en hardware Linux.
- No hay pruebas de latencia perceptual.
- `Fade` sigue siendo inmediato en Linux.

## Criterios de aceptación

1. Reproducción estable Windows. ✅
2. Mezcla, música y efectos. ✅
3. Audio 2D con distancia/pan. ✅
4. Pruebas y documentación. ✅
5. Validación Linux en hardware. ⏳

## Dependencias

Runtime, NAudio y Linux.
