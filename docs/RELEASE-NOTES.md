## Ollin Beta 6.52.0 — cierre de limitaciones de 0.49

### Incluye

- La **migración de grupos** entra en el historial de deshacer.
- **Variables por nodo** con el estado posterior a la acción.
- **Cancelar ejecutar hasta** desde **Más ▾**.
- **Mover hijos ocultos** se conserva en el grafo.
- **205 pruebas automatizadas**.

### Cómo probarlo

1. Instala 6.52.0 y abre el grafo.
2. Abre un grupo antiguo y usa **Deshacer**.
3. Reproduce una acción que cambie `GameState` y revisa variables por nodo.
4. Lanza **Ejecutar hasta** y cancélalo desde **Más**.
5. Activa **Mover hijos ocultos**, cierra y reabre el grafo.

### Limitaciones

- **No hay paso atrás**.
- Las variables muestran el estado posterior, no un diff antes/después.
- La demo técnica no se publica.