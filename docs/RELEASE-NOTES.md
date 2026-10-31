## Ollin 0.25.0 beta — IA 3D

### Incluye

- Enemigos 3D con patrulla y persecución por etiqueta.
- Visión con radio y **línea de vista** (las paredes bloquean).
- Memoria de la última posición conocida.
- Colisión contra paredes de caja.
- Enemigo de ejemplo en la plantilla **2.5D (pasillo)**.
- 132 pruebas automatizadas.

### Cómo probarlo

1. Instala 0.25.0 y crea un proyecto **2.5D (pasillo)**.
2. Reproduce y acércate a la Sombra: te verá si hay línea de vista y te perseguirá.
3. Aléjate u ocúltate tras una pared: la IA seguirá un rato la última posición y luego
   patrullará.

### Limitaciones

- Sin navegación A* alrededor de esquinas; usa deslizamiento por paredes.
- Sin cilindros de colisión.