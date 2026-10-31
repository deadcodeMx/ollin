## Ollin 0.38.0 beta — historial persistente, grupos y breakpoints

### Incluye

- **Historial persistente** del grafo entre cierres.
- **Sincronización** con cambios externos de la escena.
- **Comentarios** y **grupos visuales** arrastrables.
- **Breakpoints** que pausan la partida al alcanzar un nodo.
- **194 pruebas automatizadas**.

### Cómo probarlo

1. Instala 0.38.0 y abre el demo.
2. Abre **Guía → Grafo**, mueve/edita nodos y cierra la ventana.
3. Vuelve a abrir el grafo: el historial debe conservarse.
4. Añade un **Comentario** o un **Grupo** y arrástralo.
5. Pon un **breakpoint** en una acción, reproduce y comprueba que la partida se pausa.

### Limitaciones

- El historial se guarda por escena/entidad; si cambia la ruta de la escena, se pierde.
- El historial no se limpia automáticamente y no está versionado.
- Los grupos son visuales: no hay plegado, anidación ni pertenencia persistente.
- No hay **paso a paso** ni inspección de variables por nodo; solo breakpoints.
- La sincronización externa se sondea cada 120 ms.