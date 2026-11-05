# Tilemap

**Prioridad:** P1
**Estado:** Activo
**Dueño:** render + editor

## Objetivo

Tilemaps rápidos y cómodos para niveles 2D.

## Estado actual (6.278.0)

- `TileSet`, `TileDefinition` y `TileMapLayer`/`Tilemap`.
- Autotile por variantes (`ResolveAutotile`).
- Serialización de tilesets (`TileSetSerializer`).
- Pintado y edición básica en el editor.
- Capas mediante varias entidades.

## Funciones nuevas

- [ ] Autotile por vecindad/terreno.
- [ ] Colisión por tile.
- [ ] Capas y Y-sort nativos.
- [ ] Editor de tileset usable.
- [ ] Pintado por lotes optimizado.
- [ ] Pruebas de mapas grandes.

## Limitaciones conocidas

- Autotile limitado a variantes.
- Colisión por tile incompleta.
- Capas/Y-sort básicos.
- Sin pruebas de mapas grandes.

## Criterios de aceptación

1. Autotile por vecindad.
2. Colisión por tile correcta.
3. Capas y Y-sort.
4. Editor de tileset usable.
5. Pruebas de rendimiento.

## Dependencias

Render, física y editor.

## Riesgos

Cambios en el formato de tileset requieren migración.
