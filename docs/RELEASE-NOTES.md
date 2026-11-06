# Ollin 7.0.0-beta.8 — física 2D avanzada

## Colliders

- Polígonos convexos y cadenas de bordes con grosor.
- Círculo, cápsula, cilindro y cono.
- Sensores `Area2D` con entrada/salida estable.

## Capas y máscaras

- Filtrado bidireccional en simulación y consultas.
- `CanCollideWith` centraliza la regla.

## Joints

- `DistanceJoint2D` y `RopeJoint2D`.
- Resolución de joints en cada subpaso de física.
- Corregida la integración: los joints ya se ejecutan en el runtime.

## CCD

- Barrido de cuerpos rápidos contra colliders compatibles.
- Subpasos adaptativos según la velocidad máxima.

## Consultas

- `Raycast` y `RaycastAll`.
- `OverlapCircle` y `OverlapBox`.
- `ShapeCast` / `BoxCast`.
- Filtros por capa, triggers, entidad ignorada y etiquetas.

## Verificación

- Build: 0 errores.
- Pruebas: 197/197.
- Instalador: `Ollin-Setup-7.0.0-beta.8.exe`.

## Nota

Release incremental del bloque de física. La base sigue siendo el ciclo 7.0.0.