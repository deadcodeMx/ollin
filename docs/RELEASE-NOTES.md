## Ollin Beta 6.55.0 — cierre de 6.53

### Incluye

- **Ventana de atajos accionable**: cada fila ejecuta la acción y cierra.
- **Smoke UIA** que valida nombres de accesibilidad del buscador y Más.
- **205 pruebas automatizadas**.

### Cómo probarlo

1. Instala 6.55.0 y abre el grafo.
2. Pulsa **F1**.
3. Haz clic en **Ordenar** o **Distribuir H**: debe ejecutar y cerrar.
4. Ejecuta `tools/ui-smoke.ps1` para validar UIA.

### Limitaciones

- No se ha probado con lector de pantalla real (Narrator/NVDA).
- La demo técnica no se publica.