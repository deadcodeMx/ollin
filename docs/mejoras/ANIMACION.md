# Animación y esqueletos

**Prioridad:** P1
**Estado:** Completado — 7.0.0-beta.13
**Dueño:** editor + render

## Objetivo

Animación 2D suficiente para personajes y efectos.

## Estado actual (7.0.0-beta.13)

- `AnimationPlayer2D` con clips, tracks y keyframes.
- Curvas con easing, tangentes de entrada/salida y Hermite.
- Eventos de frame y eventos de audio.
- `AnimationBlendTree2D` para mezcla lineal de clips.
- `Skeleton2D` con huesos jerárquicos e IK de dos huesos.
- `SkeletonAnimator2D` con clips de ángulos, eventos y blend.
- `SkinnedMesh2D` con cuatro huesos y pesos.
- `AnimationStateMachine` con estados, transiciones por parámetro y FSM visual.
- Editor de timeline con previsualización de curvas, tracks y keyframes.
- Editor visual de FSM con estados y transiciones.

## Funciones completadas

- [x] Timeline editable con curvas.
- [x] Eventos de frame.
- [x] Pesos de skinning.
- [x] Mezcla por capas/clips.
- [x] FSM visual.
- [x] Editor de curvas/timeline.
- [x] Pruebas de animación y esqueletos.

## Verificación

- Build: 0 errores.
- Pruebas: 207/207.
- Nuevas pruebas: animación esquelética con eventos y transiciones FSM.

## Criterios de aceptación

1. Timeline con curvas. ✅
2. Eventos de frame. ✅
3. Clips skeletal con pesos. ✅
4. Mezcla por capas/clips. ✅
5. FSM visual. ✅
6. Pruebas. ✅

## Dependencias

Render y editor.
