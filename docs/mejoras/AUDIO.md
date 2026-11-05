# Audio

**Prioridad:** P1
**Estado:** Activo
**Dueño:** runtime

## Objetivo

Audio estable en Windows y Linux, con mezcla y 2D.

## Estado actual (6.278.0)

- `Audio`, `AudioSource` y `Audio2D`.
- Música, efectos y reproducción puntual.
- HUD/audio host desde scripts.
- Audio 2D con distancia básica.

## Funciones nuevas

- [ ] Validar backend Linux en hardware.
- [ ] Mezclador y buses.
- [ ] Audio 2D con atenuación/panning completos.
- [ ] Pruebas de latencia.
- [ ] Transiciones de música.
- [ ] Volumen por categoría.

## Limitaciones conocidas

- Backend Linux no validado.
- Mezcla/volúmenes básicos.
- Efectos 2D limitados.
- Sin pruebas de latencia.
- Sin buses avanzados.

## Criterios de aceptación

1. Reproducción estable Windows/Linux.
2. Mezcla, música y efectos.
3. Audio 2D con distancia.
4. Pruebas y documentación.

## Dependencias

Runtime, NAudio y Linux.

## Riesgos

El backend de audio es distinto por plataforma; aislar tras `Audio`.
