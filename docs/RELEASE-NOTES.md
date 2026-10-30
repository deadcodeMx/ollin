## Ollin 0.20.3 beta — el instalador actualiza de verdad

### Corregido

- El instalador **cierra Ollin.Editor y Ollin.Player** antes de copiar los archivos.
  Antes podían quedar bloqueados y la instalación seguía usando la versión vieja.
- Esto explica que tras instalar 0.20.x se siguieran exportando juegos con el
  reproductor antiguo.

### Importante

1. Instala 0.20.3 (el instalador cerrará el editor solo).
2. Abre tu proyecto y **vuelve a exportar** el juego.
3. Ejecuta el nuevo `<Juego>.exe`.

### Diagnóstico

Junto al `.exe` se genera `player.log`. Debe contener:

```
Backend de audio: activo
Scripts precargados: N (...)
Script iniciado: ...
Audio 'assets/audio/...' → id 1
```