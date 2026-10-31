## Ollin 0.44.0 beta — cierre de limitaciones de 0.41

### Incluye

- **Distribución uniforme** entre tarjetas.
- Etiqueta **DESACTIVADA** en conexiones deshabilitadas.
- Historial limitado a **500 archivos** y 30 días.
- **200 pruebas automatizadas**.

### Cómo probarlo

1. Instala 0.44.0 y abre el grafo.
2. Selecciona tres nodos de distinto ancho y usa **Distribuir H**.
3. Deshabilita una conexión: debe verse gris y con `DESACTIVADA`.
4. Abre/cierra el grafo varias veces y revisa `graph-history`.

### Limitaciones

- La distribución usa el orden visual actual.
- La etiqueta puede solaparse en conexiones muy cortas.
- El límite de historial es por número de archivos, no por tamaño en MB.