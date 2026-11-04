# Jardín de Ollin

Ejemplo 2D del núcleo actual de Ollin. No usa 3D ni sistemas RPG.

## Abrir

Desde la raíz del repositorio:

```powershell
dotnet run --project src/Ollin.Editor/Ollin.Editor.csproj -- --project samples/JardinOllin
```

O abre `samples/JardinOllin/project.ollin` desde el editor.

## Controles

- WASD o flechas: mover a Lina.
- Recoge Semilla, Flor y Lucero.
- El HUD muestra el progreso.

## Qué demuestra

- Escena 2D con cámara, tilemap, sprites, luces y partículas.
- Física 2D y CharacterBody2D.
- Script C# (`scripts/Jugador.cs`) con Input, navegación de entidades y HUD.
- UI Canvas con etiquetas.
- Señales y API de scripting.

## Estructura

- `scenes/inicio.ollin.json`: escena principal.
- `scripts/Jugador.cs`: movimiento y recogida.
- `project.ollin`: proyecto.
