# Feedback de interfaz

**Prioridad:** P0
**Estado:** Completado — 13/13 puntos
**Origen:** usuarios beta, 2026-11-05

## Objetivo

Convertir el feedback real en mejoras verificables del editor, sin romper el flujo
de trabajo existente.

## Estado por punto

| # | Feedback | Estado |
|---|---|---|
| 1 | BoxCollider se veía circular y faltaban formas | **Hecho**: caja real de 4 esquinas, cilindro y cono |
| 2 | CharacterBody no genera código | **Hecho**: script C# y grafo con eventos/acciones de personaje |
| 3 | Segundo viewport con la cámara | **Hecho**: selector, ocultar/mostrar, ventana independiente y placeholder |
| 4 | Clic derecho en jerarquía | **Hecho**: espacio vacío y entidad, con componentes por categoría |
| 5 | Añadir componente desde inspector | **Verificado**: el botón ya abre la lista por categorías |
| 6 | Gizmos de mover/rotar/escalar | **Hecho**: modos con botones y atajos W/E/R |
| 7 | Panel de archivos estilo Unity | **Hecho**: árbol + cuadrícula, breadcrumb y búsqueda |
| 8 | Traducción al inglés | **Hecho**: español oficial, inglés opcional y persistente |
| 9 | Emojis en jerarquía | **Hecho**: símbolos y emojis |
| 10 | Tema claro pierde contraste | **Hecho**: revisión visual, paneles claros y texto oscuro |
| 11 | Formas en SpriteRenderer | **Hecho**: cuadrado, cilindro y cono |
| 12 | Botón Scripts con lista del proyecto | **Hecho**: menú con scripts y recompilar |
| 13 | Arrastrar archivos al inspector | **Hecho**: texturas y scripts |

## Limitaciones conocidas

- Cilindro/cono usan contorno aproximado; la física trabaja con caja envolvente.
- La traducción cubre textos conocidos; ventanas nuevas pueden quedar en español.
- El grafo de personaje expone salto, velocidad y gravedad, no todos los métodos internos.

## Siguientes mejoras

- [ ] Cobertura de traducción para todas las ventanas.
- [ ] Más acciones de personaje expuestas al grafo (dash, pared, techo).
- [ ] Colisión exacta para cilindro/cono.
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
12. Tema claro legible en jerarquía, inspector y consola.
13. Grafo de personaje con saltar, velocidad y gravedad.

## Pruebas

- ColliderShapeTests: esquinas, cilindro, cono y serialización.
- Serialización de SpriteRenderer.Shape.
- Build del editor y 187 pruebas.

## Dependencias

Editor, render, física y scripting.
