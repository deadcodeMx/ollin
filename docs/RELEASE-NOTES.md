## Ollin 0.36.0 beta — selección múltiple y expresiones

### Incluye

- **Selección múltiple** de nodos con Ctrl/Shift; Ctrl+A selecciona todo.
- **Mover varios nodos** arrastrando cualquiera de los seleccionados.
- **Copiar/pegar grupos** con sus conexiones internas.
- **Clic derecho** en una conexión o salida para borrarla.
- **Validación de sintaxis** de expresiones.
- **187 pruebas automatizadas**.

### Cómo probarlo

1. Instala 0.36.0 y abre el demo.
2. Selecciona **Guía** y pulsa **Grafo**.
3. Ctrl+clic en varios nodos y arrastra uno: se mueven todos.
4. Ctrl+C / Ctrl+V pega el grupo.
5. Clic derecho sobre una línea para borrar la conexión.
6. Escribe una expresión inválida como `2 +` y revisa **VALIDACIÓN**.

### Limitaciones

- La selección múltiple es por Ctrl/Shift; no hay lazo de selección.
- El historial restaura la selección principal, no todo el grupo.
- Copiar/pegar conserva solo conexiones internas del grupo.
- El borrado de conexiones no tiene menú contextual; es clic derecho directo.
- La validación detecta sintaxis, no variables no definidas ni errores semánticos.