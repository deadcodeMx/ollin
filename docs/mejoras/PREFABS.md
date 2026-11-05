# Prefabs y blueprints

**Prioridad:** P1
**Estado:** Activo
**Dueño:** editor

## Objetivo

Crear, guardar e instanciar entidades reutilizables sin duplicar escenas.

## Estado actual (6.278.0)

- `PrefabSerializer` guarda/carga una entidad.
- `BlueprintSerializer` captura una jerarquía y la instancia.
- El grafo puede clonar una entidad plantilla (`Crear entidad`).
- El editor puede crear prefabs desde una entidad.
- No hay biblioteca visual de prefabs.

## Funciones nuevas

- [ ] Biblioteca de prefabs con vista previa.
- [ ] Instanciar por id de prefab, no solo por nombre de entidad.
- [ ] Prefabs anidados.
- [ ] Sustituir prefab en escena.
- [ ] Pruebas de round-trip y herencia.

## Limitaciones conocidas

- Depende de nombres de entidad.
- Sin previsualización.
- Herencia de prefab limitada.

## Criterios de aceptación

1. Guardar/cargar una jerarquía completa.
2. Instanciar desde biblioteca.
3. Prefabs anidados.
4. Pruebas de serialización.

## Dependencias

Serialización, editor y grafo.
