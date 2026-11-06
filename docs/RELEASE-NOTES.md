# Ollin 7.0.0-beta.9 — cierre de física 2D

## Broadphase espacial

- Rejilla espacial en lugar de comparar todos los pares.
- Los colliders de gran tamaño se comparan contra todos para no perder colisiones.

## EdgeCollider2D exacto

- Colisión círculo-borde por distancia real al segmento.
- Ya no depende de la caja envolvente del contorno.

## Joints con impulsos

- Solver iterativo de 8 iteraciones.
- Corrección de posición y velocidad.
- `DistanceJoint2D`, `RopeJoint2D`, `SpringJoint2D` y `PinJoint2D`.

## Verificación

- Build: 0 errores.
- Pruebas: 198/198.
- Instalador: `Ollin-Setup-7.0.0-beta.9.exe`.

## Nota

Con esto se cierran los límites documentados de la etapa de física 2D.