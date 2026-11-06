# Validación — primera pasada

**Proyecto:** `samples/ValidacionOllin`
**Fecha:** 2026-11-06
**Versión:** 7.0.0-beta.9
**Estado:** primera pasada completada; quedan huecos de motor.

## Alcance probado

- Crear el proyecto, abrirlo, reproducirlo y exportarlo.
- Nivel con tilemap, plataformas, cámara con seguimiento, jugador y enemigo.
- Movimiento, salto, daño por enemigo y HUD de vida/monedas.
- Meta de nivel y pantalla de fin.
- Menú de pausa del reproductor: continuar, guardar, cargar, opciones y salir.
- Ejecutable exportado con `game.pak` y scripts compilados.

## Resultado

| Prueba | Resultado |
|---|---|
| Arranque del editor y reproductor | ✅ |
| Cámara de juego y seguimiento | ✅ |
| Movimiento y salto | ✅ |
| Enemigo y daño | ✅ |
| HUD | ✅ |
| Meta y fin de nivel | ✅ |
| Pausa | ✅ |
| Exportación y ejecución | ✅ |
| Guardado/carga | ⚠️ botones presentes; falta verificar persistencia completa |
| Monedas | ⚠️ no se recogió una en la pasada automática |
| Fondo detrás del tilemap | ❌ hueco de orden de render |

## Huecos encontrados

### 1. `System` no era global en scripts
Los scripts de validación usaban `MathF` y fallaban con `CS0103` al exportar.

- **Corrección:** `ScriptCompiler` ahora añade `global using System;`.
- **Prueba:** `SystemEstaDisponibleSinUsingExplicito`.

### 2. El fondo tapa el tilemap
`Renderer2D` dibuja primero los tilemaps y después los sprites. Un sprite de fondo grande, aunque tenga `SortingOrder` menor, cubre el tilemap.

- **Workaround en el juego:** se eliminó el sprite de fondo.
- **Pendiente de motor:** ordenar tilemaps y sprites por `SortingOrder` en la misma pasada, o separar capas de fondo.

### 3. Meta fuera del alcance
La meta estaba a una altura que el jugador no tocaba caminando.

- **Corrección:** se bajó la meta al nivel del suelo.

### 4. HUD solo en el reproductor
El HUD de scripts aparece en el ejecutable exportado, pero no en la ventana `Ollin — Juego` del editor.

- **Pendiente:** reutilizar el HUD del reproductor en el Game View.

### 5. Sin menú inicial
El juego arranca directamente en el nivel; el menú disponible es el de pausa.

- **Pendiente:** decidir si el juego de validación necesita un menú de inicio o si la pantalla de pausa es suficiente.

## Próximos pasos

1. Verificar guardado/carga completa y recogida de monedas.
2. Corregir el orden de render de tilemaps y sprites.
3. Mostrar el HUD también en el Game View del editor.
4. Añadir un menú de inicio si la validación lo exige.
5. Repetir la matriz completa y actualizar este informe.
