# Linux

**Prioridad:** P1
**Estado:** Paquete generado; validación en hardware pendiente — 7.0.0-beta.14
**Dueño:** release + runtime

## Objetivo

Que Linux sea una plataforma soportada, no experimental.

## Estado actual (7.0.0-beta.14)

- Código multiplataforma .NET 8 y Avalonia.
- `tools/release.ps1` genera instalador Windows.
- `tools/linux-package.ps1` genera `Ollin-<versión>-linux-x64.tar.gz`.
- Launcher `Ollin` y `LEEME-LINUX.txt` incluidos.
- Audio Linux con `paplay`, `aplay` o `ffplay`.
- El paquete se ha generado en Windows y no se ha ejecutado en una distro real en este entorno.

## Funciones completadas

- [x] Paquete Linux por release.
- [x] Documento de instalación dentro del paquete.
- [x] Selección de backend de audio Linux.
- [x] Empaquetado reproducible.
- [ ] Probar instalación y arranque en distro real.
- [ ] Validar audio Linux en hardware.
- [ ] Validar gamepads Linux.
- [ ] CI Linux.

## Cómo generar

```powershell
powershell -ExecutionPolicy Bypass -File tools/linux-package.ps1
```

Salida:

```text
dist/linux/Ollin-<versión>-linux-x64.tar.gz
```

## Criterios de aceptación

1. Instalación y arranque en distro real. ⏳
2. Editor y player funcionando. ⏳
3. Audio y gamepad. ⏳
4. Paquete de cada release. ✅
5. CI Linux. ⏳

## Dependencias

Runtime, audio y release.
