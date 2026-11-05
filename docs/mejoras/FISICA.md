# Mejoras de física 2D

**Prioridad:** P1
**Estado:** Pendiente

## Objetivo

Física estable para plataformas, top-down y puzzles.

## Limitaciones

- Falta pulido de colisionadores de polígono/borde.
- Capas y máscaras no cubren todos los casos.
- Joints avanzados incompletos.
- CCD no probado en casos límite.
- Triggers/sensores pueden fallar con cuerpos rápidos.
- Consultas básicas; faltan shape casts y filtros.
- Sin pruebas de estrés.

## Criterios de aceptación

- Capas/máscaras completas y documentadas.
- CCD sin atravesar paredes.
- Joints estables.
- Raycast/overlap/shape cast con filtros.
- Pruebas de colisión, triggers y cuerpos rápidos.

## Tareas

- [ ] Colisionador de polígono y borde.
- [ ] Máscaras por capa.
- [ ] CCD.
- [ ] Joints avanzados.
- [ ] Shape cast.
- [ ] Pruebas de estrés.

## Dependencias

`Ollin.Core` y `Ollin.Runtime`.
