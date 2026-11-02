## Ollin Beta 6.56.0 — accesibilidad del grafo

### Incluye

- La ventana **F1** enfoca la primera acción y se cierra con `Esc`.
- El estado del grafo se anuncia como región viva.
- El buscador tiene nombre y ayuda accesible.
- Guía manual para Narrator/NVDA.
- **205 pruebas automatizadas**.

### Cómo probarlo

1. Instala 6.56.0 y abre el grafo.
2. Pulsa **F1**; la primera acción debe tener el foco.
3. Pulsa **Esc** para cerrar.
4. Ejecuta `tools/ui-smoke.ps1` para validar nombres UIA.
5. Sigue `docs/ACCESIBILIDAD.md` para Narrator/NVDA.

### Limitaciones

- La prueba con lector de pantalla real queda pendiente de ejecución manual.
- La demo técnica no se publica.