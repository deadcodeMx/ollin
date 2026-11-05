# Red y RPC

**Prioridad:** P2
**Estado:** Activo
**Dueño:** runtime

## Objetivo

Multijugador 2D básico y seguro.

## Estado actual (6.278.0)

- `NetworkManager` con `StartHost`, `Connect` y `Send`.
- `NetworkSnapshot`, `NetworkTransformState` y `NetworkSystem`.
- Interpolación básica de transformaciones.
- Componentes de red.

## Funciones nuevas

- [ ] Transporte UDP/LiteNetLib estable.
- [ ] RPC con IDs y serialización.
- [ ] Autoridad servidor/cliente.
- [ ] Predicción/rollback.
- [ ] Validación y límites de frecuencia.
- [ ] Pruebas con dos procesos.

## Limitaciones conocidas

- Sin RPC.
- Sin autoridad completa.
- Sin predicción/rollback.
- Sin pruebas con dos procesos.
- Sin límites de frecuencia.

## Criterios de aceptación

1. Transporte UDP funcional.
2. RPC con IDs.
3. Autoridad y validación.
4. Predicción/interpolación.
5. Pruebas reales.

## Dependencias

Runtime y scripting.

## Riesgos

La red introduce estado no determinista; aislar y versionar mensajes.
