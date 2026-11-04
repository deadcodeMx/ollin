## Ollin Beta 6.138.0 — audio Linux

### Añadido

- Backend Linux basado en `paplay`/`aplay`/`ffplay`.
- Fallback silencioso si no hay reproductor.
- Loop básico reiniciando el proceso.

### Limitaciones

- Sin control de volumen por handle en Linux.
- Sin paneo estéreo.
- Depende de un reproductor del sistema.

### Verificación

- **219/219 pruebas**.
- Instalador 6.138.0 instalado y abierto.
- Demo técnica no publicada.