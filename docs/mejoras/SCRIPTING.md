# Mejoras de scripting

**Prioridad:** P1
**Estado:** Pendiente

## Objetivo

Cubrir las APIs que un juego 2D real necesita sin obligar a tocar el núcleo.

## Limitaciones

- No hay cambio de escena desde script.
- No hay RPC de red.
- Faltan eventos de UI enlazados a scripts.
- `MoveTo`/agente no cubren patrullas, esperas ni llegada.
- Faltan utilidades de cámara (límites, shake ya existe, follow avanzado).
- No hay API de guardado por ranuras desde script.
- El tipado de componentes se resuelve con `Get<T>`; no hay acceso generado.
- Errores de script dependen de la consola; no hay debugger.

## Criterios de aceptación

- Cambio de escena con carga asíncrona y estado.
- API de UI: mostrar, ocultar, texto, foco y eventos.
- Navegación con llegada y patrulla.
- Guardado/carga por ranura.
- Pruebas de API y documentación.

## Tareas

- [ ] `Scene.Load`, `Scene.Reload`, `Scene.Transition`.
- [ ] Eventos UI (`OnClick`, `OnFocus`) sin acoplar al editor.
- [ ] Navegación: `Patrol`, `Wait`, `Arrived`.
- [ ] Guardado por ranura.
- [ ] Debugger de scripts.

## Dependencias

Runtime, escenas, UI y guardado.
