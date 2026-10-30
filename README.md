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
| Pruebas automatizadas | ✅ 24/24 |
| Gizmos, assets, scripting C#, físicas, audio | ⏳ etapas 2–4 |

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

## Estructura del repositorio

```
src/
  Ollin.Core/        Modelo de escena, componentes, cámara, serialización, log
  Ollin.Rendering/   Renderer 2D sobre OpenGL ES (sprite batch, cuadrícula, hit-test)
  Ollin.Runtime/     Bucle de juego: reproducción, pausa, delta time y restauración
  Ollin.Editor/      Aplicación Avalonia: GUI, viewport, inspector, consola
tests/
  Ollin.Tests/       Pruebas del núcleo y del runtime (xUnit)
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
