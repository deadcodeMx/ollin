## Ollin Beta 6.170.0 — SubViewport y render-to-texture

### Añadido

- `RenderTarget2D` con framebuffer, color y depth.
- `SubViewport2D` con `UseTexture`.
- Composición de la textura del subviewport en pantalla.
- Checkbox "Render a textura".

### Corregido

- `SubViewport2D` no estaba registrado en el discriminador JSON, por lo que no se podía cargar desde escena.

### Verificación

- **236/236 pruebas**.
- Instalador 6.170.0 instalado.
- Se probó una escena con SubViewport; no se observó un error de carga.
- La composición visual del overlay no quedó claramente diferenciada porque usaba la misma cámara/escena; falta una prueba con cámara distinta.

## [6.169.0] — 2026-11-04