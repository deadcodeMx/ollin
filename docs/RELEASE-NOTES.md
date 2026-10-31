## Ollin 0.32.0 beta — editor de grafo completo

### Incluye

- **Deshacer/rehacer** en el grafo, con Ctrl+Z / Ctrl+Y.
- **Copiar/pegar** nodos con Ctrl+C / Ctrl+V.
- **Zoom** de 35% a 200% y Ctrl+rueda.
- **Validación automática** con panel de errores/avisos y nodos resaltados.
- **Depuración visual**: último nodo ejecutado en cian y variables en vivo.
- Ventana del grafo **no modal** para observar la partida.
- 163 pruebas automatizadas.

### Cómo probarlo

1. Instala 0.32.0 y abre el **Demo técnico 2.5D**.
2. Selecciona **Guía** y pulsa **Grafo**.
3. Crea nodos, conéctalos, mueve y usa **Ctrl+Z / Ctrl+Y**.
4. Usa **Ctrl+C / Ctrl+V** y el zoom con **Ctrl+rueda**.
5. Revisa el panel **VALIDACIÓN**.
6. Pulsa **Reproducir** en el editor y observa el nodo activo y las variables.

### Limitaciones

- El historial vive mientras la ventana del grafo está abierta; al cerrarla se pierde.
- Copiar/pegar trabaja con **un nodo**, no con subgrafos.
- El zoom no está centrado en el cursor.
- La validación es heurística; no compila expresiones.
- La depuración muestra el último nodo, sin breakpoints ni paso a paso.