# Mejoras del grafo visual

**Prioridad:** P0
**Estado:** Completado en 6.278.0 — límites conocidos al final.
**Riesgo:** alto; toca modelo, editor y codegen.

## Objetivo

Que el grafo sea una forma real de programar: variables, parámetros, control de
flujo, datos tipados y traducción fiable a C#.

## Estado a 6.278.0

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

## Principio de diseño

El grafo no debe incluir acciones cerradas para “puerta”, “spawner” o “checkpoint”.
El usuario debe poder construir esos comportamientos combinando primitivas.

Una acción cerrada obliga a adivinar el juego del usuario. Una primitiva genérica
sirve para cualquier juego.

## Primitivas genéricas objetivo

- **Crear entidad**: instanciar una entidad o prefab por nombre.
- **Destruir entidad**.
- **Fijar propiedad**: ya existe.
- **Emitir señal/evento**: comunicación sin acoplar componentes.
- **Consultar área**: entidades dentro de un radio, con filtro.
- **Temporizador**: esperar o repetir cada intervalo.
- **Cargar escena**.
- **Variables y expresiones**: ya existen.

Estas primitivas permiten construir puerta, spawner y checkpoint sin que el motor
los defina.
## Límites conocidos

- No existe un pin universal para cualquier campo de cualquier nodo; hay pin de expresión universal y campos semánticos por acción.
- Las esperas dentro de funciones con parámetros locales no heredan esos parámetros.
- Listas y structs no se sincronizan con `GameState`.
- La carga de escena reinicia scripts, pero no conserva estado de UI/audio de la escena anterior.
- El parser de argumentos no entiende lambdas.
- No hay pruebas de integración de UI.
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
- Listas y structs con valores iniciales.
- Pin universal de expresión.
- Pruebas de compilación por caso.
- Primitiva Crear entidad.
- Destruir entidad.
- Emitir y recibir señales.
- Consultar área.
- Temporizador de espera.
- Cargar escena integrada con `SceneRuntime`.
- Listas de structs y structs anidados.
- Auto-cast y conversión explícita.

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

- **1. Sí:** hay compilación real de variables, parámetros, funciones, subgrafos y eventos.
- **2. Sí:** funciones con condición, bucle y return generan código.
- **3. Sí:** eventos personalizados inline con parámetros.
- **4. Sí:** `SetVariable`/`AddVariable` usan la expresión.
- **5. Sí:** se compila antes de guardar y se bloquea si falla.
- **6. Sí:** guard configurable en `while`.
- **7. Sí:** variables con tipo y valor inicial.
- **8. Sí:** cada caso tiene prueba de compilación real con Roslyn.

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
- [x] Listas y structs.
- [x] Auto-cast completo.

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
