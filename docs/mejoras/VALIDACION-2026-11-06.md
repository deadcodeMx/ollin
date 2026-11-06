# Validación — primera pasada

**Proyecto:** `samples/ValidacionOllin`
**Fecha:** 2026-11-06
**Versión:** 7.0.0-beta.9
**Estado:** cerrada — 7.0.0-beta.10.

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
| Fondo detrás del tilemap | ✅ corregido por orden de render |

## Huecos encontrados

### 1. `System` no era global en scripts
Los scripts de validación usaban `MathF` y fallaban con `CS0103` al exportar.

- **Corrección:** `ScriptCompiler` ahora añade `global using System;`.
- **Prueba:** `SystemEstaDisponibleSinUsingExplicito`.

### 2. El fondo tapa el tilemap
`Renderer2D` dibuja primero los tilemaps y después los sprites. Un sprite de fondo grande, aunque tenga `SortingOrder` menor, cubre el tilemap.

- **Corregido:** los sprites con orden menor al tilemap se dibujan antes; el resto, después.
- El nivel conserva el fondo y el suelo se ve correctamente.

### 3. Meta fuera del alcance
La meta estaba a una altura que el jugador no tocaba caminando.

- **Corrección:** se bajó la meta al nivel del suelo.

### 4. HUD en el Game View
El HUD de scripts solo aparecía en el ejecutable exportado.

- **Corregido:** el Game View del editor muestra barras, etiquetas, mensajes y fin de partida.

### 5. `rootEntities` duplicaba las raíces
El serializador guardaba una segunda lista de entidades raíz dentro de `scene`.

- **Corregido:** `RootEntities` es calculada y lleva `[JsonIgnore]`.
- **Prueba:** `NoSerializaLaListaCalculadaDeRaices`.

### 6. Sin menú inicial
El juego arranca directamente en el nivel; el menú disponible es el de pausa.

- **Pendiente:** decidir si el juego de validación necesita un menú de inicio o si la pantalla de pausa es suficiente.

## Cierre

La primera pasada deja el motor y el juego en estado jugable y exportable. Quedan como mejoras posteriores:

1. Verificación manual completa de guardado/carga en varias ranuras.
2. Recogida de las tres monedas y balance del nivel.
3. Menú de inicio si se considera necesario.
4. Repetir la matriz en la siguiente beta.
