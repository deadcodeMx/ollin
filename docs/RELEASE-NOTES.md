## Ollin Beta 6.108.0 — reglas de diseño

### Cambiado

- `Knockback2D` es un componente independiente.
- `Hitbox` solo gestiona daño y enfriamiento.
- El empuje se combina: `Hitbox + Knockback2D + Rigidbody2D`.
- Auditoría en `docs/REGLAS-DISENO.md`.

### Verificación

- **205/205 pruebas**.
- Instalador 6.108.0 instalado y abierto sin crash.
- Demo técnica no publicada.