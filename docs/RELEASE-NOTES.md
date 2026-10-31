## Ollin 0.48.0 beta — cierre de limitaciones de 0.44

### Incluye

- **Separar H/V** conservando huecos existentes.
- Conexiones desactivadas cortas sin etiqueta solapada (tooltip y `(off)`).
- Historial limitado por **20 MB** además de archivos/edad.
- **203 pruebas automatizadas**.

### Cómo probarlo

1. Instala 0.48.0 y abre el grafo.
2. Usa **Separar H/V** en **Más**: conserva huecos y corrige solapes.
3. Deshabilita una conexión corta: verás `(off)` en el puerto y tooltip en la línea.
4. Revisa que el historial no crezca por encima de 20 MB.

### Limitaciones

- Separar solo impone un mínimo; no iguala huecos.
- Las conexiones cortas no muestran la etiqueta textual.
- El historial elimina las entradas más antiguas al superar el tope.