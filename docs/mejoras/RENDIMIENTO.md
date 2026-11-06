# Rendimiento y pruebas

**Prioridad:** P1
**Estado:** Completado — 7.0.0-beta.16
**Dueño:** render + QA

## Objetivo

Medir y mejorar el coste real de render, física y UI.

## Estado actual (7.0.0-beta.16)

- Pruebas de estrés automatizadas en `PerformanceStressTests`.
- RenderServer con 20.000 sprites.
- Física con 500 cuerpos durante 30 ticks.
- UI Canvas con 2.000 elementos y layout grid.
- Tilemap con 256×256 celdas.
- Profiler de FPS, frame ms, draws y entidades.
- Suite serializada para evitar interferencias de estado estático.

## Resultados de la última pasada

Entorno: Windows x64, build Debug de pruebas, sin GPU real.

| Prueba | Resultado |
|---|---|
| 20.000 sprites en RenderServer | ~35–48 ms |
| 500 cuerpos × 30 ticks | ~4–5 s |
| 2.000 elementos UI | ~100–160 ms |
| Tilemap 256×256 | correcto |
| Suite completa | 213/213 |

La física con 500 cuerpos es el coste más alto; el broadphase espacial ya evita
comparar todos los pares, pero el solver y las consultas de bounds siguen siendo
el cuello de botella en escenas densas.

## Funciones completadas

- [x] Pruebas de estrés de render, física, UI y tilemap.
- [x] Medición de tiempos.
- [x] Serialización de pruebas para medidas estables.
- [x] Documentación de resultados.

## Límites conocidos

- Las medidas son de build Debug, no de Release con GPU.
- No hay benchmark de luces/partículas a gran escala.
- La física densa sigue siendo el mayor coste.
- No hay CI de rendimiento con umbrales estrictos.

## Criterios de aceptación

1. Pruebas de estrés. ✅
2. Medición reproducible. ✅
3. Cuellos de botella documentados. ✅
4. Pruebas verdes. ✅

## Dependencias

Render, física, UI y runtime.
