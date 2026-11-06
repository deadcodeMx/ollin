# Ollin 7.0.0-beta.17 — multijugador y RPC

## Red

- RPC con método, payload, peer y tick.
- Host reenvía RPC a los demás peers.
- Autoridad básica por propietario.
- Snapshots e interpolación existentes.
- Pruebas de serialización y ciclo de host.

## Verificación

- Build: 0 errores.
- Pruebas: 215/215.
- Instalador: `Ollin-Setup-7.0.0-beta.17.exe`.

## Límites

Sin predicción/rollback, canales fiables, lobby, reconexión ni prueba de dos procesos.