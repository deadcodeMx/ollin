## Ollin 0.49.0 beta — cierre de limitaciones de 0.45

### Incluye

- **Ejecutar hasta seleccionado**.
- **Migración** de grupos antiguos a paternidad explícita.
- **Mover hijos ocultos** opcional al arrastrar grupos plegados.
- **Variables por nodo** en la depuración.
- **Modo paso persistente** al reabrir el grafo.
- **204 pruebas automatizadas**.

### Cómo probarlo

1. Instala 0.49.0 y abre el grafo.
2. Selecciona un nodo y usa **Más ▾ → Ejecutar hasta seleccionado**.
3. Abre un grupo antiguo: sus nodos pasan a pertenencia explícita.
4. Activa **Mover hijos ocultos** y arrastra un grupo plegado.
5. Reproduce con modo paso y revisa **Variables por nodo**.

### Limitaciones

- No hay paso atrás.
- La migración de grupos no entra en el historial de deshacer.
- Las variables por nodo son una foto antes de ejecutar la acción.