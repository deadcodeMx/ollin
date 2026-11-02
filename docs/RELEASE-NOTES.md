## Ollin Beta 6.102.0 — Input Map y gamepad

### Añadido

- **Input Map** por proyecto, guardado en `project.ollin`.
- **Editar → Mapa de entrada…**
- Acciones por defecto:
  - move_left, move_right, move_up, move_down.
  - jump, interact, attack, pause.
- Bindings de teclado y gamepad.
- Editor para añadir, quitar y restablecer acciones.
- Gamepad vía XInput.
- API de scripts:
  - `Input.GetAction("jump")`
  - `Input.GetActionDown("jump")`
  - `Input.GetActionUp("jump")`
- Personaje 2D usa move_left/move_right/jump.

### Verificación

- **205/205 pruebas**.
- Instalador 6.102.0 instalado y abierto sin crash.
- Demo técnica no publicada.