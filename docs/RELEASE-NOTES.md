# Ollin 7.0.0-beta.14 — audio y Linux

## Audio

- Buses Master, música, efectos y UI.
- Volumen por categoría.
- Música con instancia única.
- Audio 2D con atenuación y panning.
- Corregido el doble volumen de Master en NAudio.
- Backend Linux con paplay/aplay/ffplay.
- `ffplay` aplica volumen inicial.

## Linux

- Nuevo `tools/linux-package.ps1`.
- Paquete `Ollin-<versión>-linux-x64.tar.gz`.
- Launcher y LEEME-LINUX incluidos.
- Audio Linux empaquetado; validación en hardware pendiente.

## Verificación

- Build: 0 errores.
- Pruebas: 209/209.
- Instalador Windows y paquete Linux generados.

## Nota

La etapa de audio queda cerrada en Windows. Linux queda empaquetado y documentado,
pero sin validación real de hardware en este entorno.