# Física 2D

**Prioridad:** P1
**Estado:** Activo
**Dueño:** runtime

## Objetivo

Física estable para plataformas, top-down y puzzles.

## Estado actual (6.278.0)

- `Physics2D`, `CharacterBody2D`, `Joints2D`.
- `PhysicsQuery` con `Raycast` y `BoxCast`.
- `PhysicsServer2D` y `PhysicsSystem`.
- Triggers y cuerpos estáticos/dinámicos.
- Capas y máscaras básicas.

## Funciones nuevas

- [ ] Colisionador de polígono y borde pulido.
- [ ] Máscaras por capa completas y documentadas.
- [ ] CCD para cuerpos rápidos.
- [ ] Joints avanzados.
- [ ] Overlap y shape cast con filtros.
- [ ] Pruebas de estrés y cuerpos rápidos.

## Limitaciones conocidas

- CCD no probado en casos límite.
- Triggers pueden fallar con cuerpos muy rápidos.
- Joints avanzados incompletos.
- Faltan filtros completos en consultas.
- Sin pruebas de estrés.

## Criterios de aceptación

1. Capas/máscaras completas.
2. CCD sin atravesar paredes.
3. Joints estables.
4. Raycast/overlap/shape cast con filtros.
5. Pruebas de colisión, triggers y velocidad alta.

## Dependencias

`Ollin.Core` y `Ollin.Runtime`.

## Riesgos

Cambiar el solver puede alterar escenas existentes; versionar el formato si cambia.
