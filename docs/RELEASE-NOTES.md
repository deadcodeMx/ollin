## Ollin 0.45.0 beta — modo paso y grupos explícitos

### Incluye

- **Modo paso** sin breakpoint.
- **Agrupar seleccionados / Desagrupar** con paternidad explícita.
- Arrastrar un grupo plegado ya no mueve miembros ocultos.
- **202 pruebas automatizadas**.

### Cómo probarlo

1. Instala 0.45.0 y abre el grafo.
2. En **Más ▾** activa **Modo paso** y reproduce: debe pausar en cada nodo.
3. Pulsa **Paso** repetidamente.
4. Crea un grupo, selecciona nodos y usa **Agrupar seleccionados**.
5. Marca **Plegado** y comprueba que se ocultan.
6. Arrastra el grupo plegado: los ocultos no deben moverse.

### Limitaciones

- No hay paso atrás.
- Los grupos creados antes de 0.45 siguen usando pertenencia geométrica.
- El modo paso no tiene “ejecutar hasta el nodo seleccionado”.