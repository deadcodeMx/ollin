# Scripting

**Prioridad:** P1
**Estado:** Activo
**Dueño:** runtime + editor

## Objetivo

Que un juego 2D completo se pueda escribir sin tocar el núcleo.

## Estado actual (6.278.0)

- `OllinScript` con `OnStart`, `OnUpdate`, `OnDestroy`, `OnReload`,
  colisiones, triggers e interacción.
- Acceso a entidad, componentes, escena, input, audio, HUD, diálogo, cámara,
  tween, shake y corrutinas.
- Señales globales (`SignalBus`) y eventos de grafo.
- Carga de escena desde el grafo mediante `SceneLoadQueue`.
- Guardado por ranuras existe en `SaveSlotService`.
- El grafo genera C# compilado y validado antes de guardar.

## Funciones nuevas

- [ ] `Scene.Load`, `Scene.Reload`, `Scene.Transition` como API pública de script.
- [ ] Eventos de UI (`OnClick`, `OnFocus`) sin acoplar al editor.
- [ ] Navegación: `Patrol`, `Wait`, `Arrived`.
- [ ] Fachada de guardado por ranura desde script.
- [ ] Debugger de scripts con breakpoints y variables.
- [ ] Acceso tipado generado a componentes.
- [ ] RPC de red.

## Limitaciones conocidas

- No hay debugger visual de scripts.
- La navegación no cubre patrullas, esperas ni llegada.
- No hay RPC.
- La UI no expone eventos de click/foco a scripts.
- Los errores dependen de consola y del diálogo de compilación.

## Criterios de aceptación

1. Cambio de escena desde script con estado.
2. Eventos de UI utilizables desde C# y grafo.
3. Navegación con llegada y patrulla.
4. Guardado/carga por ranura.
5. Pruebas de API y documentación.

## Dependencias

Runtime, escenas, UI y guardado.

## Riesgos

Cambiar la API de escena puede romper scripts existentes; mantener sobrecargas.
