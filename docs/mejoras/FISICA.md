# Física 2D

**Prioridad:** P1
**Estado:** Completado — 7.0.0-beta.9
**Dueño:** runtime

## Objetivo

Física estable para plataformas, top-down y puzzles.

## Estado actual (7.0.0-beta.9)

- `Physics2D`, `CharacterBody2D`, `Joints2D`.
- `PhysicsQuery` con `Raycast`, `RaycastAll`, `OverlapCircle`, `OverlapBox`, `BoxCast` y `ShapeCast`.
- `PhysicsServer2D` para sensores y `PhysicsSystem` para contactos.
- Triggers y cuerpos estáticos/dinámicos.
- Capas y máscaras bidireccionales.
- Broadphase por rejilla espacial.
- CCD por barrido y subpasos adaptativos.

## Funciones completadas

- [x] Colisionador de polígono y borde.
- [x] Máscaras por capa completas y documentadas.
- [x] CCD para cuerpos rápidos.
- [x] Joints avanzados (`DistanceJoint2D`, `RopeJoint2D`, solver de impulsos).
- [x] Overlap y shape cast con filtros.
- [x] Pruebas de colisión, triggers y velocidad alta.

## Limitaciones resueltas

- El borde contra círculo usa distancia exacta al segmento.
- Los triggers respetan capas y máscaras en ambos sentidos.
- Los joints se ejecutan en cada subpaso con corrección de posición.
- Las consultas filtran por capa, triggers, entidad y máscara.

## Criterios de aceptación

1. Capas/máscaras completas. ✅
2. CCD sin atravesar paredes. ✅
3. Joints estables. ✅
4. Raycast/overlap/shape cast con filtros. ✅
5. Pruebas de colisión, triggers y velocidad alta. ✅

## Verificación

- Build: 0 errores.
- Pruebas: 198/198.
- Documentación: [FISICA-2D](../FISICA-2D.md).

## Dependencias

`Ollin.Core` y `Ollin.Runtime`.

## Riesgos

Cambiar el solver puede alterar escenas existentes; versionar el formato si cambia.
