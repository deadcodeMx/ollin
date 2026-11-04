## Ollin Beta 6.173.0 — framebuffer de Avalonia

### Corregido

- `RenderTarget2D` restaura el framebuffer anterior de Avalonia en lugar de asumir el framebuffer 0.
- Esto explica por qué la composición del SubViewport no aparecía.

### Añadido

- `RenderTarget2D` con color/depth.
- SubViewport con render-to-texture y borde.

### Verificación

- **236/236 pruebas**.
- Instalador 6.173.0 instalado.
- **La composición visual del SubViewport sigue sin verificarse en pantalla**; el fallo de framebuffer está corregido, pero no tuve una captura concluyente.

## [6.172.0] — 2026-11-04