## Ollin 0.37.0 beta — pulido del editor de grafo

### Incluye

- **Zoom centrado en el cursor** y **paneo con botón central**.
- **Validación semántica** de variables y funciones.
- **Historial de ejecución** de los últimos 24 nodos.
- **Búsqueda** de nodos y **auto-layout**.
- **Pointer lock suspendido** al activar el grafo durante la partida.
- **191 pruebas automatizadas**.

### Cómo probarlo

1. Instala 0.37.0 y abre el demo.
2. Abre **Guía → Grafo**.
3. Usa Ctrl+rueda sobre un nodo: el zoom debe acercarse a ese punto.
4. Arrastra con el botón central para paneo.
5. Prueba **Buscar nodo** y **Ordenar**.
6. Escribe `foo(1)` o una variable no definida y revisa VALIDACIÓN.
7. Reproduce y activa la ventana del grafo: el cursor debe quedar libre.

### Limitaciones

- La validación semántica no conoce variables escritas por componentes/scripts.
- El historial de ejecución es en memoria (24 nodos) y no tiene breakpoints ni paso a paso.
- No hay grupos/comentarios de nodos.
- El auto-layout es BFS simple y puede solapar ramas.
- La búsqueda resalta, no filtra ni salta al nodo.