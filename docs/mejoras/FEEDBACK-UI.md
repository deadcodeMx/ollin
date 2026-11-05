# Feedback de interfaz

**Prioridad:** P0
**Estado:** En curso
**Origen:** usuarios beta, 2026-11-05

## Objetivo

Convertir el feedback real en mejoras verificables del editor, sin romper el flujo
de trabajo existente.

## Estado por punto

| # | Feedback | Estado |
|---|---|---|
| 1 | BoxCollider se veía circular y faltaban formas | **Hecho**: caja real de 4 esquinas, cilindro y cono |
| 2 | CharacterBody no genera código | **Parcial**: botón “Crear script de personaje” con esqueleto C# |
| 3 | Segundo viewport con la cámara | **Hecho**: selector, ocultar/mostrar, ventana independiente y placeholder |
| 4 | Clic derecho en jerarquía vacía | **Hecho**: menú contextual con entidades y componentes por categoría |
| 5 | Añadir componente desde inspector | **Verificado**: el botón ya abre la lista por categorías |
| 6 | Gizmos de mover/rotar/escalar | **Hecho**: modos con botones y atajos W/E/R |
| 7 | Panel de archivos estilo Unity | **Hecho**: árbol + cuadrícula, breadcrumb y búsqueda |
| 8 | Traducción al inglés | **Hecho**: español oficial, inglés opcional y persistente |
| 9 | Emojis en jerarquía | **Hecho**: símbolos y emojis |
| 10 | Tema claro pierde contraste | **Parcial**: colores duros reemplazados por recursos; falta QA visual |
| 11 | Formas en SpriteRenderer | **Hecho**: cuadrado, cilindro y cono |
| 12 | Botón Scripts con lista del proyecto | **Hecho**: menú con scripts y recompilar |
| 13 | Arrastrar archivos al inspector | **Hecho**: texturas y scripts |

## Funciones nuevas

- [ ] Viewport de cámara secundario.
- [ ] Modos de gizmo: mover, rotar, escalar.
- [ ] Panel de archivos con árbol, breadcrumbs y drag-and-drop.
- [ ] Localización español/inglés.
- [ ] Drag-and-drop de texturas/scripts al inspector.
- [ ] Script de personaje también editable desde el grafo.

## Limitaciones conocidas

- Cilindro/cono usan contorno aproximado; la física trabaja con caja envolvente.
- El script de personaje es una plantilla editable, no un generador automático.
- El tema claro necesita revisión visual completa.
- El menú de jerarquía de una entidad sigue siendo más plano que el de espacio vacío.

## Criterios de aceptación

1. Los cuatro colliders se dibujan y serializan.
2. CharacterBody puede generar y enlazar un script.
3. Jerarquía vacía y entidad tienen menús por categorías.
4. Scripts abre la lista del proyecto.
5. Tema claro legible en inspector, jerarquía y toolbar.
6. SpriteRenderer dibuja las tres formas.
7. Vista de cámara con selector, ocultar/mostrar y ventana independiente.
8. Gizmos separados de mover, rotar y escalar.
9. Panel de archivos con cuadrícula, breadcrumb y búsqueda.
10. Drag-and-drop de texturas/scripts al inspector.
11. Idioma español oficial e inglés opcional.

## Pruebas

- ColliderShapeTests: esquinas, cilindro, cono y serialización.
- Serialización de SpriteRenderer.Shape.
- Build del editor y 186 pruebas.

## Dependencias

Editor, render, física y scripting.
