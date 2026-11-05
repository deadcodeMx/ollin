# Render 2D

**Prioridad:** P1
**Estado:** Activo
**Dueño:** render

## Objetivo

Render 2D estable y medible para sprites, luces y UI.

## Estado actual (6.278.0)

- Render OpenGLES con `SpriteRenderer`.
- Materiales (`CanvasMaterial2D`), luces 2D y partículas.
- SubViewports con render-to-texture.
- Cámara 2D, canvas layers y transiciones.
- Tema visual del editor independiente del juego.

## Funciones nuevas

- [ ] Batching por material/textura medido.
- [ ] Orden de capas y Y-sort nativo.
- [ ] Sombras/luces 2D avanzadas.
- [ ] Perfil de draw calls.
- [ ] Pruebas gráficas de regresión.

## Limitaciones conocidas

- Batching mejorable.
- Sin métricas de draw calls.
- Sin pruebas visuales automáticas.

## Criterios de aceptación

1. Métricas de draw calls.
2. Batching estable.
3. Pruebas visuales.
4. Documento de rendimiento.

## Dependencias

Rendimiento y tilemap.
