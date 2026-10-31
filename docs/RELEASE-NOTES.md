## Ollin 0.41.0 beta — grupos, distribución y conexiones deshabilitadas

### Incluye

- El **marquee de un grupo** selecciona sus miembros.
- **Distribuir H / Distribuir V** con separación igual.
- **Deshabilitar conexión** desde el menú contextual.
- **Breakpoints persistentes**.
- **Historial versionado** con limpieza a 30 días.
- **Borde rojo en vivo** en expresiones.
- **198 pruebas automatizadas**.

### Cómo probarlo

1. Instala 0.41.0 y abre el grafo de **Guía**.
2. Enmarca un grupo: también se seleccionan sus nodos.
3. Selecciona tres nodos y usa **Distribuir H/V**.
4. Clic derecho en una conexión → **Deshabilitar conexión**; la línea se vuelve gris.
5. Pon un breakpoint, cierra y reabre el grafo: debe conservarse.
6. Escribe una expresión inválida: el borde se pone rojo al instante.

### Limitaciones

- La distribución usa el orden actual de los nodos, no su separación previa.
- No hay paso a paso real.
- Los grupos no se pliegan ni anidan.
- La barra del grafo ocupa dos líneas en ventanas estrechas.