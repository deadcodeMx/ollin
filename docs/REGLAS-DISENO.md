# Reglas de diseño de componentes

1. Un componente resuelve una responsabilidad.
2. Nada de componentes específicos de un juego si puede ser dato + componente genérico.
3. Los componentes se combinan: `Health + Hitbox + Hurtbox + Knockback`.
4. Items, diálogos y misiones viven en assets, no en código.
5. Todo componente necesita inspector, undo, serialización y preview.

## Auditoría actual

| Componente | Responsabilidad | Estado |
|---|---|---|
| `Health` | Vida y daño | ✅ |
| `Hitbox` | Detectar y aplicar daño | ✅ |
| `Hurtbox` | Recibir daño por equipo | ✅ |
| `Knockback2D` | Aplicar empuje | ✅ separado de Hitbox |
| `Weapon2D` | Disparar proyectiles | ✅ genérico y por datos |
| `Projectile2D` | Mover y dañar | ✅ |
| `Checkpoint2D` | Guardar punto de reaparición | ✅ |
| `Pickup` | Recoger item o curar | ✅ dato `ItemId` |
| `Door` | Puerta 3D con llave | ⚠️ específica de 3D; no usar en 2D |
| `Weapon` | Arma FPS 3D | ⚠️ específica de 3D; no usar en 2D |
| `WinLoseRules` | Reglas de victoria/derrota | ⚠️ convertir a datos de proyecto |
| `StarterProject` | Plantillas de ejemplo | ✅ assets de plantilla, no runtime |

## Contenido en assets

- Items: `items/*.json`.
- Diálogos: `dialogs/*.json`.
- Misiones: datos de `GameState` y assets de proyecto.
- Enemigos y armas: prefabs + componentes genéricos.

## Pendiente de cumplimiento total

- Preview visual de `Checkpoint2D`, `Hitbox`, `Hurtbox` y `Weapon2D` en el viewport.
- Inspector dedicado para cada variante de `Door`/3D si se reactiva el 3D.
- Convertir `WinLoseRules` en configuración de proyecto.
