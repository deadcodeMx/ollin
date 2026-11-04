## Ollin Beta 6.180.0 — diagnóstico RTT y limpieza

### Añadido

- Editor de puntos de `NavigationPolygon2D`.
- Launcher/instalador corregido y verificado.

### Diagnóstico RTT

- `previousFramebuffer=0`.
- `fbo=2`.
- `restored=0`.

Avalonia usa el framebuffer 0; por tanto no era un problema de restauración de FBO. La composición del SubViewport sigue sin aparecer; el fallo está en la textura/quad/orden.

### Verificación

- **236/236 pruebas**.
- Instalador **6.180.0** instalado correctamente.
- Escena de ejemplo restaurada.

## [6.178.0] — 2026-11-04