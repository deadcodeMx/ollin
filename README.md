# Ollin

Motor de juego **2D** con editor gráfico propio y scripting en **C#**, construido sobre
[.NET 8](https://dotnet.microsoft.com/), [Avalonia](https://avaloniaui.net/) y **OpenGL**
(vía [Silk.NET](https://github.com/dotnet/Silk.NET)).

> *Ollin* significa **movimiento** en náhuatl. El *Nahui Ollin* es el símbolo mexica del
> movimiento y el cambio.

![Editor de Ollin](docs/images/editor.png)

## Estado actual — Etapa 1 completada · Etapa 2 en curso

| Área | Estado |
| --- | --- |
| Shell del editor (menú, barras, paneles, tema oscuro) | ✅ |
| Jerarquía de escena con selección | ✅ |
| Inspector de propiedades con edición en vivo | ✅ |
| Viewport 2D con OpenGL ES 3.0 (cuadrícula, ejes, sprites) | ✅ |
| Cámara ortográfica con zoom y paneo | ✅ |
| Selección por clic en el viewport (hit-test rotado) | ✅ |
| Guardar / abrir escena en JSON | ✅ |
| Consola de mensajes del motor | ✅ |
| Modo Play: reproducir, pausar y detener con restauración | ✅ |
| Panel de assets: miniaturas, importación y asignación de texturas | ✅ |
| Gizmos en el viewport: mover, rotar y escalar con el ratón | ✅ |
| Deshacer / rehacer con historial de estados | ✅ |
| Scripting en C# con Roslyn (compilación en memoria) y ciclo de vida | ✅ |
| Pruebas automatizadas | ✅ 41/41 |
| Recarga en caliente sin parar, físicas, audio, export | ⏳ etapas 3–5 |

Consulta el [roadmap por etapas](docs/ROADMAP.md) y la
[arquitectura](docs/ARQUITECTURA.md).

## Requisitos

- **Windows 10/11** (el motor se diseña para poder portarse a Linux/macOS más adelante).
- **.NET 8 SDK** ([descarga](https://dotnet.microsoft.com/download/dotnet/8.0)).
- GPU con soporte de OpenGL ES 3.0 (cualquier tarjeta de los últimos 10 años).

## Ejecutar el editor

```powershell
dotnet run --project src/Ollin.Editor
```

La primera vez se abre una escena de ejemplo con cámara, jugador, cajas y una moneda.

## Atajos

| Atajo | Acción |
| --- | --- |
| `Ctrl + N` | Nueva escena |
| `Ctrl + O` | Abrir escena |
| `Ctrl + S` | Guardar |
| `Supr` | Eliminar entidad seleccionada |
| `F` | Enfocar la entidad seleccionada |
| Inicio | Centrar la cámara |
| F5 | Reproducir / detener la escena |
| F6 | Pausar / reanudar |
| Clic | Seleccionar sprite |
| Rueda | Zoom anclado al cursor |
| `Alt` + arrastrar, o botón central | Mover la cámara |

## Assets

Las texturas viven en `assets/` junto al proyecto y se referencian con rutas relativas
(`assets/sprites/hero.png`), así el proyecto se puede mover de carpeta sin romper nada.

- **Importar:** pestaña *Assets* → `Importar…` (copia el archivo a `assets/` y evita sobrescribir).
- **Asignar:** selecciona una entidad y una textura, y pulsa `Asignar` (o doble clic / `Enter`).
  El ancho del sprite se ajusta solo según la proporción de la imagen.
- **Formatos:** PNG, JPG y BMP.

La escena de ejemplo ya usa las texturas de `assets/sprites/`, generadas para el proyecto.

## Scripts en C#

Los scripts viven en `scripts/` y se compilan **en memoria con Roslyn** al abrir el
proyecto y al pulsar Reproducir (o el botón `⟳ Scripts`).

```csharp
using System;
using System.Numerics;
using Ollin.Scripting;

public sealed class PlayerBob : OllinScript
{
    private Vector2 _origin;
    private float _time;

    public override void OnStart() => _origin = Entity.Transform.Position;

    public override void OnUpdate(float deltaTime)
    {
        _time += deltaTime;
        Entity.Transform.Position = _origin + new Vector2(0f, MathF.Sin(_time * 2.2f) * 0.28f);
    }

    public override void OnDestroy() => Entity.Transform.Position = _origin;
}
```

- **Ciclo de vida:** `OnStart`, `OnUpdate(deltaTime)` y `OnDestroy`.
- **API:** `Entity` (con `Transform`), `Scene` y `Debug("mensaje")` hacia la consola.
- **Enlazar:** en el inspector, sección *SCRIPT* (archivo + clase) o botón
  *Crear script para esta entidad*, que genera la plantilla.
- **Errores:** se muestran en la consola con archivo, línea y código (`CS0029`…).

## Estructura del repositorio

```
src/
  Ollin.Core/        Modelo de escena, componentes, cámara, serialización, log
  Ollin.Rendering/   Renderer 2D sobre OpenGL ES (sprite batch, cuadrícula, hit-test)
  Ollin.Runtime/     Bucle de juego: reproducción, pausa, delta time y restauración
  Ollin.Scripting/   Compilación de scripts con Roslyn y API pública de scripting
  Ollin.Editor/      Aplicación Avalonia: GUI, viewport, inspector, consola
tests/
  Ollin.Tests/       Pruebas del núcleo y del runtime (xUnit)
assets/
  sprites/           Texturas de ejemplo (héroe, caja, moneda, suelo)
scripts/             Scripts de usuario en C# (se compilan en memoria)
docs/
  ARQUITECTURA.md    Cómo está construido y por qué
  ROADMAP.md         Etapas de desarrollo
```

## Pruebas

```powershell
dotnet test tests/Ollin.Tests
```

Si el host de pruebas no arranca en un entorno restringido, el mismo proyecto funciona
como ejecutor en proceso:

```powershell
dotnet run --project tests/Ollin.Tests
```

## Formato de escena

Las escenas se guardan como JSON legible (`.ollin.json`) con discriminador de tipo por
componente, pensado para poder migrarse:

```json
{
  "formatVersion": 1,
  "engine": "Ollin",
  "scene": {
    "name": "Escena de ejemplo",
    "entities": [
      {
        "name": "Jugador",
        "transform": { "position": [0, -1.6], "rotation": 0, "scale": [1, 1] },
        "components": [
          { "$type": "sprite", "size": [1.1, 1.1], "tint": [0.91, 0.64, 0.23, 1], "sortingOrder": 10 }
        ]
      }
    ]
  }
}
```

## Licencia

Pendiente de definir.
