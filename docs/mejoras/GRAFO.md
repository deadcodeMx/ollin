# Mejoras del grafo visual

**Prioridad:** P0
**Estado:** Parcial — codegen y control de flujo cerrados; datos avanzados pendientes.
**Riesgo:** alto; toca modelo, editor y codegen.

## Objetivo

Que el grafo sea una forma real de programar: variables, parámetros, control de
flujo, datos tipados y traducción fiable a C#.

## Estado a 6.263.0

### Cumplido

- `SetVariable`/`AddVariable` usan la expresión.
- Parámetros y variables en `ExpressionEvaluator` mediante `Locals`.
- Eventos personalizados inline con parámetros.
- Variables tipadas con valor inicial.
- Compilación del script antes de guardar.
- `while` con límite configurable.
- `Return` validado contra el tipo de función.
- Variables locales por función.
- Bucles y condiciones anidados con codegen inline.
- Subgrafos como métodos.
- Diff antes de sobrescribir.
- Nombres duplicados de variables/funciones/eventos.
- Breakpoints insertados como `Debugger.Break()`.
- Operadores matemáticos/lógicos con tipo de resultado.

### Parcial

- Pines por entrada: hay pines semánticos para `amount`, `message`, `component`, `property`, `radius` e `interval`, pero no para cualquier entrada de cualquier nodo.
- Auto-cast: cubre número/entero/booleano/texto; no toda la matriz.
- Pruebas de codegen compilado: hay prueba de compilación, pero no de todos los grafos posibles.
- Sincronización de estado: el script restaura variables al `OnStart` y escribe cambios; los cambios externos posteriores no se reflejan hasta reiniciar.

## Limitaciones vigentes

### Datos y tipos

- Los pines son semánticos por acción; no existe un pin universal para cualquier entrada de cualquier nodo.
- No hay arrays, listas ni structs.
- El auto-cast es básico y no cubre toda la matriz.
- Las expresiones compuestas muy anidadas pueden quedar como tipo desconocido.
- El parser de argumentos no entiende lambdas.

### Runtime

- `NodeGraph` usa `GameState` y el script generado usa campos `_var_*`/`_vars`; no comparten estado.
- Las acciones Checkpoint/OpenDoor/Spawn dependen de componentes de gameplay eliminados; radio e intervalo se ejecutan, pero no tienen efecto visible.

### Pruebas

- Hay una prueba de compilación real, pero no cubre todos los grafos posibles.
- Faltan pruebas de integración de UI y de ejecución en juego.

## Limitaciones resueltas

- `SetVariable`/`AddVariable` usan `Expression`.
- Parámetros y variables visibles en `ExpressionEvaluator`.
- Eventos personalizados inline con parámetros.
- Variables tipadas con valor inicial.
- Compilación antes de guardar.
- `while` con límite configurable.
- `Return` validado contra tipo de función.
- Variables locales por función.
- Bucles y condiciones anidados.
- Subgrafos en codegen.
- Diff antes de sobrescribir.
- Nombres duplicados de variables/funciones/eventos.
- Breakpoints en el código generado.
- Operadores con tipo de resultado.
- Sincronización inicial de variables con `GameState`.

## Criterios de aceptación

1. Un grafo con variables y parámetros genera C# que compila.
2. Una función con `if`, `for` y `return` genera código ejecutable.
3. Un evento personalizado puede usar sus parámetros en el cuerpo.
4. `SetVariable` usa la expresión escrita.
5. El código generado se compila antes de guardarse.
6. `while` tiene un guard de iteraciones configurable.
7. Las variables respetan tipo y valor inicial.
8. Pruebas de codegen compilan el resultado, no solo comparan strings.

## Cumplimiento

- **1. Parcial:** hay compilación real de un grafo simple; faltan casos con variables y parámetros.
- **2. Sí:** funciones con condición, bucle y return generan código.
- **3. Sí:** eventos personalizados inline con parámetros.
- **4. Sí:** `SetVariable`/`AddVariable` usan la expresión.
- **5. Sí:** se compila antes de guardar y se bloquea si falla.
- **6. Sí:** guard configurable en `while`.
- **7. Sí:** variables con tipo y valor inicial.
- **8. Parcial:** hay una prueba de compilación real, no una por cada grafo.

## Tareas

### Fase A — codegen fiable
- [x] Usar `Expression` en `SetVariable`/`AddVariable`.
- [x] Contexto de expresiones con parámetros y variables.
- [x] Eventos personalizados inline con parámetros.
- [x] Variables tipadas e inicializadas.
- [x] Compilar el script generado antes de guardar.

### Fase B — control de flujo
- [x] Unificar todo el cuerpo en `EmitInlineChain`.
- [x] Condiciones y bucles anidados.
- [x] Guard de iteraciones en `while`.
- [x] Validar `return` contra tipo de retorno.
- [x] Variables locales.

### Fase C — datos
- [~] Pines por cualquier entrada de nodo (semánticos por acción, no universales).
- [x] Nodos de operadores.
- [ ] Arrays/listas/structs.
- [ ] Auto-cast completo.

### Fase D — editor
- [x] Subgrafos en codegen.
- [x] Preview/diff del script.
- [x] Nombres duplicados.
- [x] Breakpoints de código.

## Pruebas mínimas

- Codegen de función con condición y bucle.
- Codegen de evento con parámetros usados en cuerpo.
- Compilación real del script generado.
- Variables de texto/bool/Vector2.
- `while` con límite.

## Dependencias

- `Ollin.Core.Scripting` (codegen y parser).
- `Ollin.Scripting` (compilador en runtime).
- `Ollin.Editor` (inspector y validación).

## Nota de compatibilidad

La corrección de variables y parámetros puede cambiar el C# generado. Los scripts
generados antes de la mejora deben regenerarse.
