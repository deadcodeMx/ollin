## Ollin 0.20.0 beta — arreglo de scripts y audio al exportar

### Corregido

- Los juegos exportados vuelven a ejecutar sus scripts (menús, botones, IA...).
- El reproductor inicializa el audio y encuentra los sonidos dentro del paquete.

### Incluye

- Exportación a un único `.exe` + `game.pak` cifrado con scripts compilados.
- Pantalla de inicio con el logo y marca de agua opcionales.

### Instalación

1. Descarga `Ollin-Setup-0.20.0-beta.exe`.
2. Vuelve a exportar tu juego (los paquetes antiguos no cambian).
3. Ejecuta el nuevo `<Juego>.exe`; ya deben sonar los menús y responder los botones.