# Arquitectura de Ollin

Documento vivo: describe cómo está construido el motor y por qué se tomaron las
decisiones. Se actualiza al cerrar cada etapa.

## 1. Visión general

Ollin es un **motor 2D con editor integrado**: el editor y el runtime comparten el mismo
proceso y el mismo modelo de escena. La GUI es Avalonia; el viewport del juego se dibuja
con OpenGL embebido en la ventana del editor.

```
┌──────────────────────────────────────────────────────────────┐
│ Ollin.Editor (Avalonia)                                      │
│                                                              │
│  MainWindow ── MainViewModel ── Scene (Ollin.Core)           │
│      │              │                  │                     │
│      │              │                  ▼                     │
│      │              │            SceneSerializer (JSON)      │
│      │              ▼                                        │
│      │        ViewportSurface ── ViewportControl             │
│      │                              │                        │
│      └──── paneles / inspector ─────┘                        │
│                                     ▼                        │
│                            Ollin.Rendering.Renderer2D        │
│                                     │                        │
│                                     ▼                        │
│                          OpenGL ES 3.0 (ANGLE)               │
└──────────────────────────────────────────────────────────────┘
```

Dependencias entre proyectos (sin ciclos):

```
Ollin.Editor ──▶ Ollin.Rendering ──▶ Ollin.Core
     └──────────────────────────────────┘
Ollin.Tests ──▶ Ollin.Rendering, Ollin.Core
```

## 2. Por qué este stack

| Decisión | Alternativa descartada | Motivo |
| --- | --- | --- |
| **Avalonia 11.3** | WPF | Multiplataforma desde el mismo código, XAML moderno, temas intercambiables, y permitía empezar sin atarse a Windows. |
| **OpenGL ES 3.0 vía Silk.NET + ANGLE** | Render por software, MonoGame | GPU real, shaders y sprite batching; ANGLE viene incluido con Avalonia en Windows, sin instalar nada. |
| **Modelo de escena con componentes** | ECS puro | Un ECS completo complica el inspector y la serialización en esta etapa; la lista de componentes es suficiente y migrable a ECS después. |
| **System.Text.Json con discriminador `$type`** | Serializador binario | Escenas legibles, revisables en git y migrables con `formatVersion`. |
| **.NET 8 + Roslyn en memoria (etapa 3)** | Compilar DLLs aparte | Recarga en caliente real y errores con línea exacta dentro del editor. |

### Contexto gráfico

`OpenGlControlBase` de Avalonia crea un contexto **OpenGL ES 3.0** (ANGLE sobre D3D11).
El renderer obtiene las funciones con `GL.GetApi(gl.GetProcAddress)` y usa GLSL ES 3.00
con VAO y buffers dinámicos.

> Verificado en el equipo de desarrollo: `OpenGL ES 3.0.0 (ANGLE 2.1.25606, NVIDIA GeForce RTX 3060)`.

## 3. Capas

### Ollin.Core — modelo y datos

- `Scene`: contenedor de entidades, jerarquía por `ParentId`, matrices de mundo y cámara principal.
- `Entity`: identidad estable (`Guid`), nombre, `Transform2D` y componentes.
- `Component`: base abstracta con `Enabled` y `Owner`; los derivados se registran en
  `[JsonDerivedType]` para poder serializarse.
- `Transform2D`: posición, rotación (radianes) y escala; matriz local con convención de
  vectores fila de `System.Numerics`.
- `CameraMath`: única fuente de verdad para convertir pantalla ↔ mundo.
- `Log`: registro central con eventos; la consola del editor se suscribe.
- `SceneSerializer` + convertidores de `Vector2`/`Vector4` a arrays legibles.

### Ollin.Rendering — dibujo

- `Renderer2D`: compila el programa de shaders, limpia, dibuja cuadrícula, sprites y
  selección; cachea texturas por ruta y usa un tablero de ajedrez como "textura faltante".
- `SpriteBatch`: lote de hasta 8192 cuadros, 8 floats por vértice (posición, UV, color),
  un *draw call* por textura.
- `Texture2D`: decodifica PNG/JPG con StbImageSharp y sube la textura a GPU.
- `Renderer2D.HitTest`: prueba punto-en-cuadrilátero respetando rotación, escala y origen.

**Convenciones de coordenadas**

- Mundo: **Y hacia arriba**, unidades de mundo (1 unidad ≈ 1 metro conceptual).
- Pantalla: píxeles, Y hacia abajo.
- Cámara: `OrthographicSize` = mitad de la altura visible en unidades; `Zoom` multiplica.
- Píxeles por unidad: `altura_px / (2 · OrthographicSize) · Zoom`.

### Ollin.Editor — aplicación

- `Program` / `App`: arranque de Avalonia, tema Fluent oscuro y estilos propios.
- `MainWindow`: layout (menú, barra, jerarquía, viewport, consola, inspector, estado) y
  atajos; las operaciones de archivo usan `StorageProvider`.
- `MainViewModel`: estado del editor (escena, selección, cámara, consola, estado sucio),
  comandos y notificación de redibujado.
- `EntityNode`: adaptador observable entre `Entity` y el inspector (conversiones a
  grados, hexadecimal, etc.).
- `ViewportSurface`: borde transparente que captura la entrada (un `Control` sin fondo
  no participa en el hit-test de Avalonia) y la traduce a paneo, zoom y selección.
- `ViewportControl`: control OpenGL; inicializa el renderer, dibuja la escena y mide FPS.

## 4. Flujo de un cuadro

1. Un `DispatcherTimer` (16 ms) pide `RequestNextFrameRendering()`.
2. Avalonia invoca `OnOpenGlRender` en su hilo de render con el contexto activo.
3. `ViewportControl` calcula el tamaño en píxeles (DPI incluido) y llama a `Renderer2D`.
4. `Renderer2D` fija viewport, limpia con el color de la cámara, calcula la matriz
   vista-proyección y llena el lote: cuadrícula → sprites ordenados → selección.
5. `SpriteBatch.End()` sube los vértices y dibuja.
6. Cada 500 ms se publican FPS y draw calls al estado (vía `Dispatcher.UIThread.Post`).

## 5. Cómo extender

### Añadir un componente

1. Crear la clase en `Ollin.Core/Scene/Components/`, heredando de `Component`.
2. Implementar `CloneFor`.
3. Registrarla en `Component.cs`:
   ```csharp
   [JsonDerivedType(typeof(Components.MiComponente), "mi-componente")]
   ```
4. (Editor) Exponer sus campos en `EntityNode` y dibujar la sección en el inspector.

### Añadir un tipo de panel

Los paneles son `Border` con clase `card` y un `TextBlock` con clase `section` como
cabecera; los estilos viven en `Styles/Theme.axaml`.

## 6. Decisiones y límites conocidos

- **Un solo hilo de render**: el dibujo ocurre en el hilo de render de Avalonia; los
  cambios de estado de UI se publican al hilo de UI. La compilación de scripts (etapa 3)
  irá en un hilo aparte.
- **Una cámara principal**: `GetMainCamera()` devuelve la primera con `IsMain`. Las
  cámaras múltiples llegarán con el modo juego.
- **Sin rotación de cámara**: el paneo/zoom la asumen alineada a los ejes; la matriz ya
  está preparada para añadirla.
- **Texturas**: las rutas son relativas a la carpeta de la escena (`ProjectRoot`).
- **Rendimiento**: el editor redibuja a ~60 Hz aunque no haya cambios; en la etapa 5 se
  medirá y se pasará a redibujado bajo demanda en reposo.
- **Sin deshacer**: la pila de comandos es parte de la etapa 2.

## 7. Convenciones

- **Código en inglés** (clases, métodos, campos); **UI y mensajes en español**.
- Namespaces = carpeta: `Ollin.Core.Model`, `Ollin.Rendering`, `Ollin.Editor.ViewModels`.
- Un archivo por tipo público, salvo agrupaciones pequeñas y coherentes.
- Todo mensaje relevante pasa por `Log` y aparece en la consola del editor.
