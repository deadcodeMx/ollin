# UI Canvas

**Prioridad:** P1
**Estado:** Completado — 7.0.0-beta.11
**Dueño:** editor + runtime

## Objetivo

UI 2D cómoda para menús, HUD y diálogos.

## Estado actual (7.0.0-beta.11)

- `UiCanvas`, `UiElement` y `UiTheme`.
- Layout libre, vertical, horizontal y **grid**.
- Anclas con márgenes izquierda/derecha/arriba/abajo.
- Contenedores anidados con padding y gap.
- Botones, etiquetas, barras, paneles, listas y contenedores.
- Foco visible y navegación con **Tab / Shift+Tab**.
- Activación con **Enter**.
- Tooltips por elemento.
- Listas con scroll.
- Acciones de UI emitidas como señal `ui.<acción>` a scripts.
- Editor visual de elementos con jerarquía, layout, márgenes, foco y tooltip.

## Funciones completadas

- [x] Contenedores y layout (horizontal, vertical, grid).
- [x] Anchors y márgenes completos.
- [x] Foco, teclado y navegación entre controles.
- [x] Listas, scroll y tooltips.
- [x] Editor visual de UI con jerarquía.
- [x] Eventos de UI hacia scripts.
- [x] Pruebas de layout y eventos.

## Criterios de aceptación

1. Layout con anchors, márgenes y contenedores. ✅
2. Navegación por teclado con foco visible. ✅
3. Temas reutilizables. ✅
4. Listas y scroll. ✅
5. Pruebas de layout y eventos. ✅

## Verificación

- Build: 0 errores.
- Pruebas: 202/202.
- Pruebas nuevas: contenedores anidados, tooltip, layout grid y márgenes.

## Dependencias

UI Canvas, editor y scripting.
