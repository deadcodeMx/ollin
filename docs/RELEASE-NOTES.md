## Ollin Beta 6.187.0 — SubViewport validado

### Corregido

- `RenderTarget2D` desligaba el framebuffer de Avalonia al construirse.
- Ahora guarda y restaura el framebuffer y la textura anteriores.
- `SubViewport2D` render-to-texture se compone correctamente.

### Validado

- Dos cámaras.
- Render target con contenido.
- Overlay visible.
- Borde de depuración visible.
- Textura directa sin readback de CPU.

### Verificación

- **236/236 pruebas**.
- Instalador 6.187.0 instalado correctamente.

## [6.186.0] — 2026-11-04