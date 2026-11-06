# Red y RPC

**Prioridad:** P2
**Estado:** Completado el bloque básico — 7.0.0-beta.17
**Dueño:** runtime

## Objetivo

Multijugador 2D básico y seguro.

## Estado actual (7.0.0-beta.17)

- `NetworkManager` con `StartHost`, `Connect`, `Send` y `Rpc`.
- `NetworkSnapshot`, `NetworkTransformState` y `NetworkSystem`.
- Interpolación de transformaciones.
- Autoridad: solo se aceptan estados cuyo `Owner` coincide con `NetworkIdentity.OwnerPeer`.
- RPC con método, payload, peer y tick.
- Host reenvía RPC a los demás peers.
- Componentes de red.
- Pruebas de serialización de snapshots y RPC, host y puerto.

## Funciones completadas

- [x] Transporte UDP.
- [x] RPC con método/payload.
- [x] Autoridad básica por propietario.
- [x] Interpolación.
- [x] Pruebas de mensajes y ciclo de host.

## Limitaciones conocidas

- Sin predicción/rollback.
- Sin canales fiables ni orden garantizado.
- Sin lobby, emparejamiento ni reconexión automática.
- Sin validación de frecuencia ni anti-cheat.
- Sin prueba con dos procesos en esta suite.

## Criterios de aceptación

1. Transporte UDP funcional. ✅
2. RPC con IDs. ✅
3. Autoridad y validación básica. ✅
4. Interpolación. ✅
5. Pruebas reales de dos procesos. ⏳

## Dependencias

Runtime y scripting.
