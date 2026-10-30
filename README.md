# Ollin

**Motor de juego 2D con editor propio y scripting en C#.**

> *Ollin* significa **movimiento** en náhuatl. El *Nahui Ollin* es el símbolo mexica del
> movimiento y el cambio.

[![CI](https://github.com/deadcodeMx/ollin/actions/workflows/ci.yml/badge.svg)](https://github.com/deadcodeMx/ollin/actions/workflows/ci.yml)
![.NET](https://img.shields.io/badge/.NET-8.0-512BD4)
![Avalonia](https://img.shields.io/badge/Avalonia-11.3-8B44AC)
![OpenGL](https://img.shields.io/badge/OpenGL-ES%203.0-5586A4)
![Roslyn](https://img.shields.io/badge/Roslyn-4.11-2E7D32)
![Pruebas](https://img.shields.io/badge/pruebas-41%2F41-47B8A8)
![Etapa](https://img.shields.io/badge/etapa-3%20de%206-E9A23B)

![Editor de Ollin](docs/images/editor.png)

## ¿Qué es Ollin?

Ollin es un motor de juego 2D escrito desde cero en **C#/.NET 8**, con su propio editor
gráfico. No envuelve otro motor: el renderizado, la escena, el bucle de juego, el
historial y el compilador de scripts son código propio del proyecto.

La idea es sencilla: **abrir el editor, montar una escena, escribir un script en C# y
darle a Reproducir**, sin salir de la misma ventana.

- Editor con tema oscuro propio (jade + ámbar) construido con **Avalonia**.
- Viewport 2D con **OpenGL ES 3.0** real (sprite batching, cuadrícula, gizmos).
- Escenas guardadas en **JSON legible** y versionables.
- Assets con rutas relativas: el proyecto se puede mover de carpeta.
- Deshacer/rehacer con historial de estados.
- Scripts en **C# compilados con Roslyn** en memoria, con recarga desde el editor.

## Estado del proyecto

| Etapa | Contenido | Estado |
| --- | --- | --- |
| 1 | Editor: jerarquía, inspector, viewport, cámara, escenas JSON | ✅ |
| 2 | Modo Play, assets, gizmos de transformación, deshacer/rehacer | ✅ |
| 3 | Scripting en C# con Roslyn y errores en consola | ✅ (sin recarga en vivo) |
| 4 | Animación, tilemaps, física 2D, audio, UI de juego | ⏳ |
| 5 | Prefabs, build del jugador, perfilador | ⏳ |
| 6 | Multiplataforma y ecosistema | ⏳ |

Detalle completo en [docs/ROADMAP.md](docs/ROADMAP.md).

## Características actuales

| Área | Qué hace |
| --- | --- |
| Escena | Entidades con jerarquía, transformación 2D y componentes serializables |
| Render | Sprite batch OpenGL con tinte, rotación, escala, orden de dibujo y texturas |
| Cámara | Ortográfica con paneo, zoom anclado al cursor y encuadre |
| Edición | Inspector en vivo, selección por árbol y por clic, gizmos mover/rotar/escalar |
| Historial | Deshacer/rehacer con agrupación de ediciones continuas (60 pasos) |
| Assets | Lista con miniaturas, importar PNG/JPG/BMP y asignar al sprite |
| Juego | Reproducir, pausar y detener restaurando la escena exactamente |
| Scripts | Compilación en memoria, ciclo de vida y errores con archivo y línea |
| Consola | Mensajes del motor con niveles (info, aviso, error) |

## Stack

| Pieza | Tecnología |
| --- | --- |
| Lenguaje | C# 12 / .NET 8 |
| GUI | [Avalonia](https://avaloniaui.net/) 11.3 |
| Render | OpenGL ES 3.0 vía [Silk.NET](https://github.com/dotnet/Silk.NET) (ANGLE en Windows) |
| Scripts | [Roslyn](https://github.com/dotnet/roslyn) (`Microsoft.CodeAnalysis.CSharp`) |
| Serialización | `System.Text.Json` con discriminador `$type` |
| Pruebas | xUnit (con ejecutor en proceso incluido) |

## Empezar

### Requisitos

- **Windows 10/11** (el código está preparado para portarse a Linux/macOS).
- **.NET 8 SDK** ([descarga](https://dotnet.microsoft.com/download/dotnet/8.0)).
- GPU con soporte de OpenGL ES 3.0.

### Ejecutar el editor

```powershell
git clone https://github.com/deadcodeMx/ollin.git
cd ollin
dotnet run --project src/Ollin.Editor
```

O abre `Ollin.sln` en Visual Studio 2022 / Rider y ejecuta **Ollin.Editor**.

### Ejecutar las pruebas

```powershell
dotnet test tests/Ollin.Tests
```

Si el host de VSTest no arranca en tu entorno, el mismo proyecto funciona como
ejecutor en proceso:

```powershell
dotnet run --project tests/Ollin.Tests
```

## Controles del editor

| Entrada | Acción |
| --- | --- |
| Clic | Seleccionar sprite en el viewport |
| Arrastrar el sprite | Mover (con `Ctrl`, pasos de 0.25) |
| Arrastrar un mango de esquina | Escalar (con `Ctrl`, pasos de 0.1) |
| Arrastrar el rombo superior | Rotar (con `Ctrl`, pasos de 15°) |
| Rueda | Zoom anclado al cursor |
| `Alt` + arrastrar, o botón central | Mover la cámara |
| `F5` / `F6` | Reproducir-detener / pausar |
| `Ctrl + Z` / `Ctrl + Y` | Deshacer / rehacer |
| `Ctrl + N` / `Ctrl + O` / `Ctrl + S` | Nueva / abrir / guardar escena |
| `Supr` | Eliminar entidad |
| `F` / `Inicio` | Enfocar selección / centrar cámara |

## Estructura del repositorio

```
src/
  Ollin.Core/        Modelo de escena, componentes, cámara, historial, serialización
  Ollin.Rendering/   Renderer 2D sobre OpenGL ES (sprite batch, cuadrícula, hit-test)
  Ollin.Runtime/     Bucle de juego: reproducción, pausa, delta time y restauración
  Ollin.Scripting/   Compilación de scripts con Roslyn y API pública de scripting
  Ollin.Editor/      Aplicación Avalonia: GUI, viewport, inspector, consola
tests/
  Ollin.Tests/       Pruebas del núcleo, del runtime y del compilador de scripts
assets/sprites/      Texturas de ejemplo (héroe, caja, moneda, suelo)
scripts/             Scripts de usuario en C# (se compilan en memoria)
docs/                Arquitectura, roadmap y guía de publicación en GitHub
```

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
- **Enlazar:** en el inspector, sección *SCRIPT* (archivo + clase) o el botón
  *Crear script para esta entidad*, que genera la plantilla.
- **Errores:** aparecen en la consola con archivo, línea y código (`CS0029`…).

## Formato de escena

Las escenas se guardan como JSON legible (`.ollin.json`) con versión de formato y
discriminador de tipo por componente:

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
          { "$type": "sprite", "texturePath": "assets/sprites/hero.png", "size": [1.3, 1.3] },
          { "$type": "script", "scriptPath": "scripts/PlayerBob.cs", "className": "PlayerBob" }
        ]
      }
    ]
  }
}
```

## Arquitectura

El editor y el runtime comparten proceso y modelo de escena. La GUI es Avalonia y el
viewport se dibuja con OpenGL embebido en la ventana.

```
Ollin.Editor ──▶ Ollin.Rendering ──▶ Ollin.Core
     │                                  ▲
     └────────▶ Ollin.Runtime ──────────┤
                Ollin.Scripting ────────┘
```

Más detalle (capas, coordenadas, flujo de un cuadro, cómo añadir componentes) en
[docs/ARQUITECTURA.md](docs/ARQUITECTURA.md).

## Contribuir

Lee [CONTRIBUTING.md](CONTRIBUTING.md). Las ideas, reportes de errores y *pull requests*
son bienvenidos.

## Licencia

[MIT](LICENSE) © 2026 DeadCodeMx.

---

## English summary

**Ollin** is a from-scratch 2D game engine and editor written in C#/.NET 8, with an
Avalonia UI, an OpenGL sprite renderer, JSON scenes, transform gizmos, undo/redo and
runtime C# scripting through Roslyn. The editor compiles the scripts in memory, binds
them to entities and reports errors with file and line. Build with `dotnet run --project
src/Ollin.Editor`.
