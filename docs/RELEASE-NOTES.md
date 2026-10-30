## Ollin 0.19.0 beta — juegos empaquetados y protegidos

### Incluye

- Exportación en **un solo ejecutable** con el nombre del juego.
- `game.pak` **cifrado** con escenas, assets y **scripts compilados** (sin `.cs`).
- Diálogo para activar/desactivar pantalla de inicio y marca de agua.
- Exportación por consola para automatizar: `--export`.
- 120 pruebas automatizadas.

### Nota

El `.exe` del juego requiere .NET 8 Desktop Runtime. La protección evita que el código
fuente quede a la vista, pero no es DRM infalible.

### Instalación

1. Descarga `Ollin-Setup-0.19.0-beta.exe`.
2. Abre tu proyecto y pulsa **⬆ Exportar**.
3. Ejecuta el `<Juego>.exe` generado.