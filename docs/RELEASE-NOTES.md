## Ollin Beta 6.166.0 — materiales de canvas

### Añadido

- `CanvasMaterial2D` con shader GLSL ES 3.0.
- Compilación y cache por fuente.
- `uTint` por material.
- Inspector de shader.

### Limitaciones

- El shader debe respetar el contrato `uTexture`, `vTexCoord`, `vColor`.
- Sin hot-reload de shaders.
- Sin uniforms arbitrarios desde el inspector.

### Verificación

- **236/236 pruebas**.
- Instalador 6.166.0 instalado y abierto.

## [6.165.0] — 2026-11-04