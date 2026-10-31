## Ollin 0.40.0 beta — marquee por área y rejilla configurable

### Incluye

- **Marquee por área completa** de tarjetas y grupos.
- **Rejilla configurable** de 2 a 50 px.
- **Guías de borde y centro** al mover nodos.
- **“Ir al destino”** en el menú contextual de conexiones.
- **Saneo de enlaces externos** al pegar.
- **Borde rojo** en expresiones inválidas.
- **196 pruebas automatizadas**.

### Cómo probarlo

1. Instala 0.40.0 y abre el grafo de **Guía**.
2. Arrastra un rectángulo sobre varias tarjetas completas.
3. Cambia el tamaño de rejilla y mueve: el snap usa ese valor.
4. Alinea nodos y observa guías de borde/centro.
5. Clic derecho en una conexión → **Ir al destino** o eliminar.
6. Copia un grupo con enlaces externos y pega.
7. Escribe una expresión inválida: borde rojo y texto.

### Limitaciones

- El marquee selecciona grupos completos, pero no sus miembros.
- Las guías no distribuyen espacios automáticamente.
- No hay “deshabilitar conexión”, solo eliminar.
- El borde rojo se actualiza al perder el foco, no en cada tecla.
- Sigue sin paso a paso real.