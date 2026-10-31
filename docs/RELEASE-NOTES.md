## Ollin 0.35.0 beta — transformaciones de mundo en 2.5D

### Incluye

- Entidades hijas 2.5D renderizadas, iluminadas y colisionadas en **posición de mundo**.
- Cámara, IA, seguidor, navegación A* y recogibles usan mundo.
- Gizmo de mover para hijos: centro de mundo y conversión a local.
- `Scene.SetWorldPosition` y `Scene.WorldDistance`.
- **182 pruebas automatizadas**.

### Cómo probarlo

1. Instala 0.35.0 y abre el demo.
2. Crea una entidad padre y arrastra otra entidad dentro de ella.
3. Mueve al padre: el hijo debe renderizarse y colisionar en la posición correcta.
4. Selecciona al hijo y muévelo con el gizmo: debe moverse respecto al padre.
5. Coloca un recogible hijo y comprueba que se recoge al pasar cerca.

### Limitaciones

- La rotación/escala del padre no se combina aún con `Wall3D.Yaw` ni con el tamaño de
  la caja; solo se compone la posición.
- `Height` de cámara y `BaseY` de pared siguen siendo valores propios, no heredados.
- Los scripts que escriben `Transform.Position` trabajan en local.