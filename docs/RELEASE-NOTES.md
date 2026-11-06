# Ollin 7.0.0-beta.7 — cierre de feedback UI

## Scene View y Game View separados

- El Scene View conserva la cámara interna del editor.
- La cámara del editor no aparece en la jerarquía.
- El Game View se abre en una ventana independiente (`Ollin — Juego`).
- El Game View renderiza solo la cámara de la escena: sin rejilla, gizmos, selección ni ayudas de edición.
- La ventana de juego recibe foco, teclado y ratón automáticamente.
- El Scene View permite seleccionar y editar durante la reproducción.
- El input del juego no se envía desde el viewport del editor.

## Scripts

- `Dictionary<,>` disponible globalmente en scripts antiguos.
- `GraphScript.cs` de ejemplo corregido.
- El grafo de personaje expone saltar, aterrizar, velocidad y gravedad.

## Interfaz
- Forma del sprite (cuadrado, cilindro, cono) actualizada en el render.
- Paneo del Scene View con Alt+arrastrar o botón central.
- Drag-and-drop de archivos hacia el inspector y el viewport.
- Exportación de todas las escenas del proyecto, no solo la inicial.

- Menús de entidad por categorías.
- Tema claro revisado.
- Panel de archivos con árbol/cuadrícula.
- Drag-and-drop al inspector.
- Vista de cámara con selector y ventana.
- Cámara y juego separados como en Unity.

## Verificación

- Build: 0 errores.
- Pruebas: 188/188.
- Input del juego verificado en la ventana `Ollin — Juego`.
- Instalador: `Ollin-Setup-7.0.0-beta.7.exe`.

## Nota

Esta release consolida el bloque de feedback. No habrá releases intermedias hasta el siguiente bloque.