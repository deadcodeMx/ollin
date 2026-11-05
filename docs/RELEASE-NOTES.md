# Ollin 7.0.0-beta.1 — feedback de interfaz

Primera beta del ciclo 7. Incluye el primer bloque de feedback de usuarios.

## Colliders

- `BoxCollider` ahora es una caja real de cuatro esquinas; ya no se dibuja circular.
- Nuevo `CylinderCollider2D`.
- Nuevo `ConeCollider2D`.
- Menú de colisión: caja, círculo, cilindro, cono, cápsula y disparador.
- Serialización de los nuevos colliders.
- Pruebas de forma y round-trip.

## SpriteRenderer

- Nueva propiedad `Shape`: cuadrado, cilindro o cono.
- Render de las tres formas.
- Selector en el inspector.
- Prueba de serialización.

## Jerarquía y componentes

- Clic derecho en espacio vacío de jerarquía abre menú de entidad y componentes.
- Componentes agrupados por categoría.
- Emojis además de símbolos.
- El botón “Añadir componente” del inspector ya abre la lista categorizada.

## Scripts

- El botón **Scripts** abre la lista de scripts del proyecto.
- Acción **Recompilar scripts** dentro del menú.
- Botón **Crear script de personaje** en la pestaña de `CharacterBody2D`.

## Tema claro

- Colores duros reemplazados por recursos de tema en toolbar, inspector, jerarquía,
  tarjetas y textos.
- Pendiente revisión visual completa.

## Verificación

- Build: 0 errores.
- Pruebas: 186/186.
- Instalador: `Ollin-Setup-7.0.0-beta.1.exe`.

## Pendiente del feedback

- Segundo viewport de cámara.
- Gizmos mover/rotar/escalar.
- Panel de archivos estilo Unity.
- Traducción al inglés.
- Drag-and-drop de archivos al inspector.
- Revisión visual del tema claro.
