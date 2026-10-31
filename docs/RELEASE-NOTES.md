## Ollin 0.39.0 beta — marquee, guías y enlaces del grafo

### Incluye

- **Marquee** para seleccionar varios nodos.
- El historial restaura la **selección múltiple**.
- Copiar/pegar conserva **enlaces externos**.
- **Menú contextual** para eliminar conexiones.
- **Snap de 10 px** y **guías de alineación**.
- Errores de expresión **junto al campo**.
- **196 pruebas automatizadas**.

### Cómo probarlo

1. Instala 0.39.0 y abre el demo.
2. Abre el grafo de **Guía**.
3. Arrastra un rectángulo sobre el fondo para seleccionar varios nodos.
4. Mueve el grupo; observa el snap y las guías ámbar.
5. Ctrl+C / Ctrl+V conserva enlaces internos y externos.
6. Clic derecho sobre una línea → **Eliminar conexión**.
7. Escribe una expresión inválida y mira el error bajo el campo.

### Limitaciones

- El marquee selecciona por la posición del nodo, no por el área completa del grupo.
- Las guías alinean bordes, no distribuyen espacios.
- El snap es fijo a 10 px.
- El menú contextual solo elimina la conexión.