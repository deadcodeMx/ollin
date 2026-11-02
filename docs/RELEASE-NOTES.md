## Ollin Beta 6.78.0 — scripts como componentes

### Cambiado

- Cada script es un **componente independiente**.
- Se pueden añadir **varios scripts** a una misma entidad.
- Los scripts se añaden desde **Archivos**, no desde el inspector.
- Clic derecho en un `.cs` → **Añadir a la entidad seleccionada**.
- Clic derecho en `scripts` → **Crear script aquí…**.
- Cada script conserva su ruta, clase y campos públicos.
- **205 pruebas automatizadas**.

### Cómo probarlo

1. Instala 6.78.0.
2. Selecciona una entidad.
3. En Archivos, clic derecho en un script → **Añadir a la entidad seleccionada**.
4. Repite con otro script.
5. En el inspector aparecen como componentes separados.

### Limitación

- La demo técnica no se publica.