# UI Canvas

**Prioridad:** P1
**Estado:** Activo
**Dueño:** editor + runtime

## Objetivo

UI 2D cómoda para menús, HUD y diálogos.

## Estado actual (6.278.0)

- `UiCanvas`, `UiElement` y `UiTheme`.
- `CanvasLayer`, `CanvasItem2D`, `ScreenTransition2D` y `SubViewport2D`.
- `VariableHud` y HUD de mensajes/barras.
- Inspector de UI en el editor.
- Temas y estilos básicos.

## Funciones nuevas

- [ ] Contenedores y layout (horizontal, vertical, grid).
- [ ] Anchors y márgenes completos.
- [ ] Foco, teclado y navegación entre controles.
- [ ] Listas, scroll y tooltips.
- [ ] Editor visual de UI con jerarquía.
- [ ] Eventos de UI hacia scripts y grafo.
- [ ] Pruebas de layout y eventos.

## Limitaciones conocidas

- Layout avanzado incompleto.
- Foco/teclado básico.
- Sin listas/scroll completos.
- Editor visual básico.
- Eventos de UI no enlazados a scripts.
- Sin tests de UI.

## Criterios de aceptación

1. Layout con anchors, márgenes y contenedores.
2. Navegación por teclado con foco visible.
3. Temas reutilizables.
4. Listas y scroll.
5. Pruebas de layout y eventos.

## Dependencias

UI Canvas, editor y scripting.

## Riesgos

Cambios de layout pueden mover menús existentes; incluir migración.
