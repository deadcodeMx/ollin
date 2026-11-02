## Ollin Beta 6.84.0 — scripts editables

### Corregido

- Quitar un script lo elimina del inspector.
- Los campos **y propiedades públicas** aparecen tras recargar scripts.
- Los valores públicos se aplican al reproducir.

### Cómo probarlo

1. Instala 6.84.0.
2. Añade un script desde Archivos.
3. Añade un `public float velocidad = 5f;` al script y guarda.
4. Pulsa **Scripts → Recargar**.
5. El campo aparece en **SCRIPT · Nombre** y puedes editarlo.
6. Pulsa **Quitar componente** y desaparece del inspector.

### Verificación

- **205/205 pruebas**.
- Instalador 6.84.0 instalado.
- Demo técnica no publicada.