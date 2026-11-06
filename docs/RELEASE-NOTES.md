# Ollin 7.0.0-beta.10 — juego de validación

## Juego de validación

- Proyecto jugable en `samples/ValidacionOllin`.
- Nivel, cámara, física, enemigo, monedas, meta, HUD, pausa y audio.
- Exportación y ejecución del reproductor verificadas.

## Correcciones de motor

- `System` disponible globalmente en scripts (`MathF`, `Random`, etc.).
- Orden de render entre tilemaps y sprites por `SortingOrder`.
- HUD de scripts visible también en el Game View del editor.
- Meta del nivel ajustada al alcance del jugador.
- `rootEntities` ya no se serializa duplicando entidades.
- HUD del Game View y orden de render verificados en beta10.

## Verificación

- Build: 0 errores.
- Pruebas: 200/200.
- Informe: `docs/mejoras/VALIDACION-2026-11-06.md`.
- Instalador: `Ollin-Setup-7.0.0-beta.10.exe`.

## Nota

Con esta beta se cierra la primera pasada del juego de validación.