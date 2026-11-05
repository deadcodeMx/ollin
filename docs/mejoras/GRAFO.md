# Mejoras del grafo visual

**Prioridad:** P0
**Estado:** Pendiente
**Riesgo:** alto; toca modelo, editor y codegen.

## Objetivo

Que el grafo sea una forma real de programar: variables, parámetros, control de
flujo, datos tipados y traducción fiable a C#.

## Limitaciones actuales

### P0 — codegen incorrecto

- `SetVariable` y `AddVariable` ignoran `Expression`; usan `ValueOf` y, sin enlace,
  `Amount`.
- Los parámetros de funciones/eventos no están disponibles dentro de
  `ExpressionEvaluator`; una condición `x > 0` no ve `x`.
- Los eventos personalizados generan el cuerpo como método sin parámetros; si el
  cuerpo usa un parámetro, el C# no compila.
- `GraphVariable` es solo metadato: el script usa `_vars` como
  `Dictionary<string, float>` e ignora tipo y valor inicial.
- El runtime `NodeGraph` usa `GameState` y el script generado usa `_vars`; no
  comparten estado.

### P1 — control de flujo

- El cuerpo de bucles usa una ruta antigua que solo inlinea acciones simples.
- Condiciones o bucles anidados dentro de un bucle pueden romper
  `break/continue`.
- `while` generado no tiene límite de iteraciones.
- No hay `return` tipado validado contra el tipo de la función.
- No hay variables locales por función.

### P1 — datos y tipos

- Los pines de datos solo alimentan `amount` o una propiedad concreta.
- No hay operadores matemáticos/lógicos como nodos; solo expresiones de texto.
- No hay arrays, listas ni structs.
- El auto-cast es básico y no cubre todas las combinaciones.
- La validación de tipos solo cubre argumentos de llamadas.

### P2 — editor

- Subgrafos no se traducen al script generado.
- No hay breakpoints en el código generado.
- No hay vista previa del diff del script antes de sobrescribir.
- No hay validación de nombres duplicados de variables/eventos/funciones.
- El parser de argumentos no entiende lambdas.

## Criterios de aceptación

1. Un grafo con variables y parámetros genera C# que compila.
2. Una función con `if`, `for` y `return` genera código ejecutable.
3. Un evento personalizado puede usar sus parámetros en el cuerpo.
4. `SetVariable` usa la expresión escrita.
5. El código generado se compila antes de guardarse.
6. `while` tiene un guard de iteraciones configurable.
7. Las variables respetan tipo y valor inicial.
8. Pruebas de codegen compilan el resultado, no solo comparan strings.

## Tareas

### Fase A — codegen fiable
- [ ] Usar `Expression` en `SetVariable`/`AddVariable`.
- [ ] Contexto de expresiones con parámetros y variables.
- [ ] Eventos personalizados inline con parámetros.
- [ ] Variables tipadas e inicializadas.
- [ ] Compilar el script generado antes de guardar.

### Fase B — control de flujo
- [ ] Unificar todo el cuerpo en `EmitInlineChain`.
- [ ] Condiciones y bucles anidados.
- [ ] Guard de iteraciones en `while`.
- [ ] Validar `return` contra tipo de retorno.
- [ ] Variables locales.

### Fase C — datos
- [ ] Pines por cualquier entrada de nodo.
- [ ] Nodos de operadores.
- [ ] Arrays/listas/structs.
- [ ] Auto-cast completo.

### Fase D — editor
- [ ] Subgrafos en codegen.
- [ ] Preview/diff del script.
- [ ] Nombres duplicados.
- [ ] Breakpoints de código.

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
