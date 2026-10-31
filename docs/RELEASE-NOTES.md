## Ollin 0.33.0 beta — guardado y pruebas de interfaz

### Incluye

- Servicio de **tres ranuras** y **autoguardado** independiente.
- `AutosaveScheduler` y `PlayerSettings` compartidos por el jugador.
- Historial y validación del grafo extraídos a clases probadas.
- **Smoke test de interfaz**: pausa/reanudación real y apertura del grafo.
- **171 pruebas automatizadas**.

### Cómo probarlo

1. Instala 0.33.0.
2. Abre el demo y guarda/carga en las tres ranuras.
3. Espera 45 s para ver el autoguardado.
4. En el editor, abre el grafo y usa deshacer/zoom/validación.
5. Opcional: ejecuta `powershell -ExecutionPolicy Bypass -File tools/ui-smoke.ps1`.

### Limitaciones

- El smoke test requiere Windows con sesión de escritorio; no corre en CI headless.
- No se pulsa cada botón del menú; se prueba la lógica y el flujo pausa/reanudación.
- El canvas del grafo se prueba por lógica (historial/validación), no píxel a píxel.
- El pointer lock solo está implementado para Windows.