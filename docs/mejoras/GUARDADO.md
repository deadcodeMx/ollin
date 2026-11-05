# Guardado y estado

**Prioridad:** P1
**Estado:** Activo
**Dueño:** runtime

## Objetivo

Guardar/cargar partidas y mantener estado global entre escenas.

## Estado actual (6.278.0)

- `SaveService` y `SaveSlotService`.
- `GameState` y `GameStateHost`.
- Guardado con respaldo `.bak`.
- El grafo escribe variables en `GameState`.
- Carga de escena desde runtime.

## Funciones nuevas

- [ ] Fachada de guardado desde scripts.
- [ ] Guardado automático configurable.
- [ ] Migración de versiones de partida.
- [ ] Guardado de UI/audio y estado de escena.
- [ ] Pruebas de corrupción y recuperación.

## Limitaciones conocidas

- El estado de listas/structs no se sincroniza con `GameState`.
- La carga de escena no conserva UI/audio.
- Sin migración de saves antiguos.

## Criterios de aceptación

1. Guardar/cargar por ranura desde script.
2. Migración de formato.
3. Recuperación ante archivo corrupto.
4. Pruebas.

## Dependencias

Serialización, escenas y runtime.
