## Ollin Beta 6.172.0 — SubViewport/RTT

### Añadido

- `RenderTarget2D` con framebuffer, color y depth.
- `SubViewport2D` con `UseTexture`.
- Composición de la textura del subviewport.
- Borde de depuración.

### Corregido

- `SubViewport2D` no estaba registrado en el discriminador JSON.
- `RenderSubViewports` no se invocaba desde el viewport.

### Verificación

- **236/236 pruebas**.
- Instalador 6.172.0 instalado.
- Se cargó una escena con SubViewport sin errores.
- **No se observó claramente el overlay** en la captura; falta validar la composición con una cámara más distinta y revisar el orden de dibujo.

## [6.171.0] — 2026-11-04