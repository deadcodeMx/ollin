## Ollin 0.42.0 beta — paso a paso y grupos plegables

### Incluye

- **Paso a paso real** desde un breakpoint.
- **Grupos plegables** que ocultan sus nodos y conexiones.
- **Anidación visual** de grupos.
- **199 pruebas automatizadas**.

### Cómo probarlo

1. Instala 0.42.0 y abre el grafo de **Guía**.
2. Pon un breakpoint en una acción y reproduce: la partida se pausa.
3. Pulsa **Paso**: se ejecuta solo ese nodo.
4. Crea un grupo, mete nodos y marca **Plegado** en el inspector.
5. Comprueba que los nodos y líneas contenidos desaparecen.

### Limitaciones

- El paso solo avanza un nodo desde un breakpoint; no hay “paso atrás”.
- El plegado es geométrico: no guarda una relación padre-hijo explícita.
- La barra del grafo puede ocupar dos líneas.