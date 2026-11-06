# Física 2D

Ollin incluye física 2D determinista por pasos y consultas independientes del render.

## Colliders

| Componente | Uso |
|---|---|
| `BoxCollider` | Caja alineada a los ejes |
| `CircleCollider2D` | Círculo exacto |
| `CapsuleCollider2D` | Cápsula convexa |
| `CylinderCollider2D` | Cilindro 2D aproximado |
| `ConeCollider2D` | Cono / visión |
| `PolygonCollider2D` | Polígono convexo definido por puntos |
| `EdgeCollider2D` | Cadena de segmentos con grosor |
| `Area2D` | Sensor que solo notifica solapamientos |

Todos heredan de `Collider2D`:

```csharp
var collider = entity.AddComponent(new PolygonCollider2D
{
    Points = new List<Vector2>
    {
        new(-1f, 0f), new(0f, 1f), new(1f, 0f), new(0f, -1f),
    },
    Layer = 1,
    Mask = 1 << 2,
    IsTrigger = false,
    OneWay = false,
});
```

## Capas y máscaras

- `Layer`: 0–31.
- `Mask`: bits de capas con las que puede colisionar.
- La comprobación es **bidireccional**: ambos colliders deben aceptar la capa del otro.

```csharp
player.Layer = 1; player.Mask = 1 << 2; // jugador -> enemigos
enemy.Layer = 2;  enemy.Mask = 1 << 1; // enemigos -> jugador
```

`CanCollideWith(other)` centraliza la regla y se usa en la simulación y en las consultas.

## Joints

| Joint | Comportamiento |
|---|---|
| `DistanceJoint2D` | Mantiene una distancia fija |
| `RopeJoint2D` | Limita la distancia máxima sin empujar si están cerca |
| `SpringJoint2D` | Muelle con rigidez y amortiguación |
| `PinJoint2D` | Une a una distancia máxima |

Los joints se resuelven en cada subpaso de física, después de los contactos.

## CCD

`Rigidbody2D.ContinuousCollision = true` hace un barrido entre la posición anterior y la nueva. Si detecta un collider compatible antes del destino, detiene el cuerpo en el punto de impacto y anula su velocidad.

Además, la simulación divide el frame en subpasos según la velocidad máxima.

## Consultas

```csharp
var hit = PhysicsQuery.Raycast(scene, origin, direction, distance, layerMask);
var all = PhysicsQuery.RaycastAll(scene, origin, direction, distance);
var bodies = PhysicsQuery.OverlapCircle(scene, center, radius, layerMask);
var boxes = PhysicsQuery.OverlapBox(scene, center, size, layerMask);
var cast = PhysicsQuery.ShapeCast(scene, center, size, direction, distance);
```

- `layerMask` filtra por `Collider2D.Layer`.
- `includeTriggers` incluye sensores.
- `ignore` excluye una entidad.
- `RaycastAll` devuelve los impactos ordenados por distancia.

## Sensores y triggers

- `Area2D` mantiene un conjunto estable de solapamientos.
- `BodyEntered` se emite una sola vez al entrar.
- `BodyExited` se emite al salir.
- Los triggers no bloquean el movimiento.
- Las capas y máscaras se respetan en ambos sentidos.

## Fase amplia y estabilidad

- Broadphase por **rejilla espacial**; los colliders enormes caen a comparación completa.
- Fase estrecha SAT para polígonos y cápsulas.
- `EdgeCollider2D` contra círculo usa la distancia exacta al segmento, no su caja envolvente.
- Joints con **solver de impulsos iterativo** (8 iteraciones) y corrección de posición.
- CCD por barrido y subpasos adaptativos.
