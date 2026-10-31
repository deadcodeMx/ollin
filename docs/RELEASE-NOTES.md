## Ollin 0.22.1 beta — profundidad y pulido del 2.5D

### Incluye

- **Búfer de profundidad** para la geometría 3D: paredes, suelos, techos y sprites se
  ocluyen correctamente.
- **Altura base** en las cajas 3D para hacer techos y plataformas.
- Corredor de ejemplo con suelo, techo y dos luces.
- Cámara más estable al entrar al viewport.

### Cómo probarlo

1. Instala 0.22.1.
2. Crea un proyecto **2.5D (pasillo)** o abre el existente.
3. Reproduce: WASD + ratón, E para abrir la puerta.
4. Exporta y comprueba que el juego empaquetado mantiene el 2.5D.

### Limitaciones

- Paredes de color liso; sin cilindros, gizmos 3D ni IA 3D todavía.
- Estética retro deliberadamente sobria.