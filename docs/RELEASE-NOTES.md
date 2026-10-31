## Ollin 0.22.0 beta — primera entrega de la etapa 6 (2.5D)

### Incluye

- Cámara en primera persona con perspectiva, mirada y movimiento.
- Paredes/cajas 3D con colisión y puertas.
- Billboards de sprites ordenados por profundidad.
- Niebla de distancia, iluminación por objeto y audio posicional básico.
- Plantilla **2.5D (pasillo)** con puerta interactiva y enemigo sprite.
- Inspector para Cámara 3D, Pared 3D y Controlador FPS.
- 126 pruebas automatizadas.

### Cómo probarlo

1. Instala 0.22.0.
2. Crea un proyecto con la plantilla **2.5D (pasillo)** (o abre el editor y añade los
   componentes a una escena).
3. Reproduce: WASD para moverte, ratón para mirar, E para abrir la puerta.

### Limitaciones

- Orden de dibujado por profundidad (pintor), sin búfer de profundidad.
- Paredes de color liso, sin cilindros ni gizmos 3D todavía.
- Primera entrega del puente 2.5D; falta pulido visual.