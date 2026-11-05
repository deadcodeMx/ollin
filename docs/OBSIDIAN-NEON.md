# Obsidiana Neón — UI del editor

## Paleta

- Fondo: `#0A0A0C`.
- Superficie: `#0F0F14`.
- Superficie elevada: `#15151D`.
- Texto: `#F2EEF7`.
- Texto secundario: `#A79FB5`.
- Degradado: violeta `#7B2CFF` → magenta `#FF2FA8` → naranja `#FF6A3D`.

## Componentes

### `NeonPalette`

Tokens y pinceles cacheados. El degradado del borde se crea una sola vez y se comparte.

### `GlassPanel`

`Decorator` que dibuja:

1. Relleno semitransparente.
2. Contenido.
3. Borde con degradado.
4. Brillo exterior opcional.

No aplica blur por panel. Para blur real de ventana se puede usar un acrylic de ventana; hacerlo por panel multiplica el coste de composición.

## Uso XAML

```xml
xmlns:oc="using:Ollin.Editor.Controls"

<oc:GlassPanel Padding="16" CornerRadius="14" Glow="True">
  <StackPanel Spacing="8">
    <TextBlock Text="JERARQUÍA" Classes="section" />
    <!-- contenido -->
  </StackPanel>
</oc:GlassPanel>
```

## Rendimiento

- Pinceles y lápices cacheados; no se reconstruyen en cada frame.
- Un solo `Render` por panel.
- Sin efectos de desenfoque por panel.
- `AffectsRender` solo en propiedades visuales.
- El contenido se mide y organiza con `Padding`, sin capas adicionales.
- Para listas grandes, virtualizar el contenido dentro del panel, no crear un panel por fila.

## Estructura recomendada

```text
Controls/
  NeonPalette.cs     tokens y pinceles
  GlassPanel.cs      contenedor glass + borde degradado
Views/
  MainWindow.axaml   layout por paneles
  ...
```

Mantener el layout clásico:

- Jerarquía izquierda.
- Viewport central.
- Inspector derecha.
- Consola/Archivos abajo.

Los paneles deben ser contenedores, no dueños de lógica de negocio.
