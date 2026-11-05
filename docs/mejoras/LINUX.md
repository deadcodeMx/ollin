# Linux

**Prioridad:** P1
**Estado:** Activo
**Dueño:** release + runtime

## Objetivo

Que Linux sea una plataforma soportada, no experimental.

## Estado actual (6.278.0)

- Código multiplataforma .NET 8 y Avalonia.
- `tools/release.ps1` genera instalador Windows.
- No hay paquete Linux automático en cada release.
- Audio/gamepad Linux sin validación de hardware.

## Funciones nuevas

- [ ] Paquete Linux por release.
- [ ] Probar instalación y arranque en distro real.
- [ ] Validar audio Linux.
- [ ] Validar gamepads Linux.
- [ ] CI Linux.
- [ ] Documento de instalación.

## Limitaciones conocidas

- Paquete Linux no validado en hardware real.
- Audio Linux sin probar.
- Gamepads Linux con campos no usados.
- Sin CI Linux.
- El paquete puede quedar atrás respecto a Windows.

## Criterios de aceptación

1. Instalación y arranque en distro real.
2. Editor y player funcionando.
3. Audio y gamepad.
4. Paquete de cada release.
5. CI Linux.

## Dependencias

Runtime, audio y release.

## Riesgos

Empaquetado distinto por distro; empezar por `.tar.gz` portable.
