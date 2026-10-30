## Ollin 0.20.2 beta — registro limpio del reproductor

### Corregido

- El juego exportado ya no carga el último proyecto del editor ni intenta compilar sus
  scripts: `player.log` deja de mostrar errores falsos de `Ollin`/`OllinScript`.
- Se mantienen las mejoras de 0.20.1: entrada de teclado/ratón, activación de ventana y
  audio inicializado.

### Cómo comprobar que funciona

Abre `player.log` junto al `.exe`; debe mostrar:

```
Backend de audio: activo
Scripts precargados: 4 (...)
Script iniciado: Director ...
Audio 'assets/audio/...' → id 1
```

Si aparece un id de audio y no se oye, revisa el volumen de Windows y la mezcladora
para el proceso del juego.

### Instalación

Descarga `Ollin-Setup-0.20.2-beta.exe`, reinstala y **vuelve a exportar** el juego.