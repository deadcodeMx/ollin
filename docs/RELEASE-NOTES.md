# Ollin 7.0.0-beta.7 — cierre de feedback UI

## Juego y editor separados

- El viewport del editor se congela durante la reproducción y no recibe input del juego.
- Se muestra el overlay **MODO JUEGO**.
- El juego se ejecuta en la ventana de cámara.
- La vista de cámara no dibuja rejilla, selección, gizmos ni ayudas de depuración.
- La ventana de juego se abre al reproducir y se cierra al detener.

## Scripts

- `Dictionary<,>` disponible globalmente en scripts antiguos.
- Corregido `GraphScript.cs` de ejemplo.
- El grafo de personaje expone saltar, aterrizar, velocidad y gravedad.

## Interfaz

- Menús de entidad por categorías.
- Tema claro revisado.
- Panel de archivos con árbol/cuadrícula.
- Drag-and-drop al inspector.
- Vista de cámara con selector y ventana.

## Verificación

- Build: 0 errores.
- Pruebas: 188/188.
- Instalador: `Ollin-Setup-7.0.0-beta.7.exe`.

## Nota

Esta release consolida el bloque de feedback. No habrá releases intermedias hasta el siguiente bloque.