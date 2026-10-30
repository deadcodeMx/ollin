## Ollin 0.21.0 beta — HUD y diálogos en el juego exportado

### Añadido

- El reproductor muestra el **HUD** (barras, etiquetas y mensajes), los **diálogos** y la
  pantalla de **fin de partida**, igual que el editor.

### Corregido

- El diálogo inicial de un juego se ejecutaba pero no se veía; como muchos scripts
  bloquean el movimiento durante el diálogo, parecía que los controles no funcionaban.
  Ahora se puede leer y avanzar con **Enter / E / Espacio**, y después moverse.

### Cómo probarlo

1. Instala 0.21.0 y **vuelve a exportar**.
2. Al empezar la partida verás el diálogo abajo; avánzalo con Enter.
3. Después podrás moverte con A/D.

### Instalación

Descarga `Ollin-Setup-0.21.0-beta.exe`.