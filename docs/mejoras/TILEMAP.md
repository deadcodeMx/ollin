# Tilemap

**Prioridad:** P1
**Estado:** Completado — 7.0.0-beta.12
**Dueño:** render + editor

## Objetivo

Tilemaps rápidos y cómodos para niveles 2D.

## Estado actual (7.0.0-beta.12)

- `TileSet`, `TileDefinition` y `TileMapLayer`/`Tilemap`.
- Autotile por variantes y por **terreno** con máscara N/E/S/O.
- Colisión por tile, one-way y pendientes.
- Varias capas por entidad y Y-sort.
- Serialización de tilesets (`TileSetSerializer`).
- Editor de tileset con colisión, terreno, variantes, navegación, sombras y pendientes.
- Pintado por lotes con `PaintRect`, `Fill` y `CopyTilesFrom`.

## Funciones completadas

- [x] Autotile por vecindad/terreno.
- [x] Colisión por tile.
- [x] Capas y Y-sort.
- [x] Editor de tileset usable.
- [x] Pintado por lotes.
- [x] Pruebas de mapas grandes.

## Verificación

- Build: 0 errores.
- Pruebas: 205/205.
- Nuevas pruebas: máscara de autotile, terreno, pintado por lotes y mapa 256×256.

## Criterios de aceptación

1. Autotile por vecindad. ✅
2. Colisión por tile correcta. ✅
3. Capas y Y-sort. ✅
4. Editor de tileset usable. ✅
5. Pruebas de rendimiento. ✅

## Dependencias

Render, física y editor.
