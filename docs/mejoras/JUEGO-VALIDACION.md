# Juego de validación

**Prioridad:** P1
**Estado:** Siguiente bloque — pendiente
**Dueño:** QA + diseño

## Objetivo

Validar el motor con un juego 2D completo y pequeño.

## Estado actual (7.0.0-beta.9)

- Proyecto de validación: `samples/ValidacionOllin`.
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

## Preparación de la validación

- [x] Física 2D cerrada en `7.0.0-beta.9`.
- [x] Documentación de áreas actualizada.
- [x] Proyecto de ejemplo eliminado; se usará el proyecto del usuario.
- [x] Proyecto creado en `samples/ValidacionOllin`.
- [x] Primera pasada jugable.
- [x] Exportación y ejecución del reproductor.
- [x] Informe inicial: [VALIDACION-2026-11-06.md](VALIDACION-2026-11-06.md).
- [ ] Verificación completa de guardado/carga y monedas.

## Matriz de validación

| Área | Prueba | Criterio |
|---|---|---|
| Arranque | Abrir proyecto, escena y reproducir | Sin errores en consola |
| Menú | Inicio, pausa, continuar y salir | Navegable con teclado/ratón |
| Nivel | Tilemap, colisiones y límites | Jugable sin atravesar geometría |
| Cámara | Seguimiento y encuadre | Estable, sin saltos |
| Física | Saltos, rampas, one-way y triggers | Sin bloqueos ni túneles |
| Enemigos | Patrulla, detección y daño | Comportamiento repetible |
| UI | HUD, salud, mensajes y pausa | Legible y sin solapamientos |
| Audio | Música y efectos | Sin cortes ni saturación |
| Guardado | Guardar, cerrar y cargar | Estado consistente |
| Exportación | Exportar y ejecutar fuera del editor | Cámara, input y escenas correctos |
| Rendimiento | Nivel completo | FPS estable y sin fugas |

## Informe de huecos

Cada hueco se anotará con:

1. Área y pasos para reproducir.
2. Resultado esperado y resultado real.
3. Impacto (bloquea / molesto / pulido).
4. Archivo o API afectada.
5. Prueba o corrección propuesta.

## Dependencias

Todas las áreas anteriores.

## Riesgos

Es el mejor detector de problemas reales; no posponerlo por pulir sistemas.
