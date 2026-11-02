## Ollin Beta 6.98.0 — Area2D y formas de colisión

### Añadido

- **Area2D** como zona de detección/trigger.
- **CircleCollider2D**.
- **CapsuleCollider2D**.
- **PolygonCollider2D**.
- Base común `Collider2D` para física y render.
- Radio editable para círculos.
- Ancho/alto para caja, cápsula y área.

### Importante

- La física usa la **caja envolvente** de círculos, cápsulas y polígonos.
- Es funcional y estable, pero la colisión circular/poligonal exacta queda para una etapa posterior.

### Verificación

- **205/205 pruebas**.
- Instalador 6.98.0 instalado y abierto sin crash.
- Demo técnica no publicada.