# Rendimiento y pruebas

**Prioridad:** P1
**Estado:** Activo
**Dueño:** render + QA

## Objetivo

Medir y mejorar el coste real de render, física y UI.

## Estado actual (6.278.0)

- Render OpenGLES con sprites, materiales, luces 2D y subviewports.
- `Profiler` y diagnóstico en runtime.
- Suite de 182 pruebas unitarias.
- Escena de ejemplo `JardinOllin`.
- Sin pruebas de estrés ni objetivos de FPS.

## Funciones nuevas

- [ ] Escena de estrés con métricas.
- [ ] Perfil CPU/GPU continuo.
- [ ] Medir batching y draw calls.
- [ ] Pruebas gráficas de integración.
- [ ] Objetivos de FPS documentados.
- [ ] Pruebas de memoria y GC.

## Limitaciones conocidas

- Sin pruebas de estrés.
- Sin perfilado continuo.
- Batching mejorable.
- Sin pruebas gráficas de integración.
- Sin objetivos de FPS.

## Criterios de aceptación

1. Escena de estrés con métricas.
2. Perfil CPU/GPU.
3. Batching y draw calls medidos.
4. Pruebas de integración.
5. Documento de resultados.

## Dependencias

Render, física y tests.

## Riesgos

Optimizar sin métricas puede degradar otros casos; medir antes y después.
