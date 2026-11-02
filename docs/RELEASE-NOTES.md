## Ollin Beta 6.87.0 — quitar componentes y valores reales

### Corregido

- Cada componente tiene un botón **✕** visible para eliminarlo.
- Al quitar un script se invalida la caché y desaparece del inspector.
- Los campos públicos muestran el **valor inicial real** del script.
  - `public float vida = 100f;` aparece como **100**, no 0.
- Los campos y propiedades se refrescan al recargar scripts.

### Cómo probarlo

1. Instala 6.87.0.
2. Añade un script con `public float vida = 100f;`.
3. Pulsa Recargar scripts.
4. En el inspector debe aparecer **vida = 100**.
5. Pulsa **✕** en cualquier componente para eliminarlo.

### Verificación

- **205/205 pruebas**.
- Instalador 6.87.0 instalado.
- Demo técnica no publicada.