# Juego de validación

**Prioridad:** P1
**Estado:** Siguiente bloque — pendiente
**Dueño:** QA + diseño

## Objetivo

Validar el motor con un juego 2D completo y pequeño.

## Estado actual (7.0.0-beta.9)

- El proyecto de ejemplo `samples/JardinOllin` se eliminó; el usuario creará su propio proyecto.
- El editor puede crear, guardar, reproducir y exportar.
- Hay grafo, física, UI, audio, guardado y scripts.
- Física 2D cerrada en `7.0.0-beta.9` con 198 pruebas.
- Todavía no hay un juego terminado de principio a fin.

## Alcance del juego

- Menú.
- Un nivel completo.
- Cámara.
- Física.
- Enemigos.
- UI.
- Audio.
- Guardado.
- Exportación.

## Funciones nuevas necesarias

- [ ] Diseño y flujo del juego.
- [ ] Nivel con tilemap.
- [ ] Enemigos con FSM.
- [ ] Menú y HUD.
- [ ] Guardado por ranura.
- [ ] Exportación a ejecutable.
- [ ] Informe de huecos del motor.

## Limitaciones que debe revelar

- API de scripting insuficiente.
- Física inestable.
- UI incómoda.
- Tilemap limitado.
- Exportación incompleta.
- Falta de documentación.

## Criterios de aceptación

1. Jugable de principio a fin.
2. Sin errores en consola.
3. Exportado y probado.
4. Documento de huecos encontrados.

## Dependencias

Todas las áreas anteriores.

## Riesgos

Es el mejor detector de problemas reales; no posponerlo por pulir sistemas.
