# Animación y esqueletos

**Prioridad:** P1
**Estado:** Activo
**Dueño:** editor + render

## Objetivo

Animación 2D suficiente para personajes y efectos.

## Estado actual (6.278.0)

- `SpriteAnimator`, `AnimationPlayer2D`.
- `AnimationBlendTree2D` y `AnimationStateMachine`.
- `Skeleton2D` y `SkinnedMesh2D`.
- Clips, estados y transiciones básicas.
- Inspector de animación en el editor.

## Funciones nuevas

- [ ] Timeline editable con curvas.
- [ ] Eventos de frame.
- [ ] Pesos de skinning completos.
- [ ] Mezcla por capas.
- [ ] FSM visual.
- [ ] Editor de curvas.

## Limitaciones conocidas

- Curvas y eventos de frame limitados.
- Skinning con pesos no completo.
- Mezcla por capas limitada.
- FSM visual básica.
- Sin editor de curvas avanzado.

## Criterios de aceptación

1. Timeline con curvas.
2. Eventos de frame.
3. Clips skeletal con pesos.
4. Mezcla por capas.
5. FSM visual.
6. Pruebas.

## Dependencias

Render y editor.

## Riesgos

El formato de clip puede cambiar; prever migración.
