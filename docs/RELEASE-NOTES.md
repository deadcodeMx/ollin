## Ollin 0.20.1 beta — entrada y diagnóstico del juego exportado

### Corregido

- El juego exportado captura el teclado y el ratón a nivel de ventana (aunque el foco
  esté en otro control) y se activa al quitar la pantalla de inicio.
- La exportación verifica el paquete y avisa si falta la escena, el manifiesto o los
  scripts compilados.
- El reproductor genera `player.log` junto al ejecutable con la versión, los scripts
  cargados y cada sonido reproducido.

### Importante

Hay que **volver a exportar** el juego con esta versión; los `.exe` exportados antes
llevan dentro el reproductor antiguo.

### Instalación

1. Descarga `Ollin-Setup-0.20.1-beta.exe`.
2. Reexporta tu juego y ejecútalo.
3. Si algo falla, envía el `player.log` que aparece junto al `.exe`.