# Publicar Ollin en GitHub

Este documento reúne el texto listo para copiar en GitHub y los pasos para publicar el
repositorio.

## Nombre del repositorio

```
ollin
```

## Descripción corta (campo *About*)

> Motor de juego 2D con editor propio en C#/.NET 8: viewport OpenGL, gizmos, historial,
> escenas JSON y scripts en C# compilados con Roslyn. «Ollin» significa movimiento en
> náhuatl.

Versión de una línea (para redes o pie de página):

> Motor de juego 2D con editor propio y scripting en C#, escrito desde cero en .NET 8.

## Descripción larga

Ollin es un motor de juego 2D con editor integrado, escrito desde cero en C#/.NET 8.
No envuelve otro motor: el renderizado, el modelo de escena, el bucle de juego, el
historial y el compilador de scripts son código propio.

El editor (Avalonia) incluye viewport OpenGL ES 3.0, jerarquía de escena, inspector en
vivo, gizmos de mover/rotar/escalar, deshacer/rehacer, panel de assets con miniaturas y
consola. Las escenas se guardan en JSON legible y los scripts se escriben en C#, se
compilan en memoria con Roslyn y se ejecutan con el ciclo de vida
`OnStart`/`OnUpdate`/`OnDestroy`.

El proyecto avanza por etapas documentadas: editor y render (1), runtime, assets y
gizmos (2), scripting (3); después animación, tilemaps, física y audio (4), build del
jugador (5) y multiplataforma (6).

## Topics sugeridos

```
game-engine, game-development, 2d, csharp, dotnet, avalonia, opengl, roslyn,
editor, engine, silk-net, sprite-batch, mexico, nahuatl, gamedev
```

## Configuración recomendada del repositorio

| Ajuste | Valor |
| --- | --- |
| Descripción | La del apartado *About* |
| Website | Opcional (devlog o página del proyecto) |
| Topics | Los de arriba |
| Licencia | MIT (detectada por el archivo `LICENSE`) |
| Issues | Activados (con plantillas incluidas) |
| Discussions | Activadas si quieres ideas abiertas |
| Wiki / Projects | Desactivados al principio |
| Actions | Activadas (la CI compila y prueba en Windows) |
| Social preview | `docs/images/editor.png` (ideal 1280×640) |

## Primer *release*: v0.3.0

Título: **Ollin v0.3.0 — scripting en C#**

```markdown
Primer corte público: editor 2D funcional con scripting en C#.

### Incluye
- Editor Avalonia con jerarquía, inspector, consola y viewport OpenGL ES 3.0
- Escenas JSON, assets con miniaturas, gizmos de mover/rotar/escalar y deshacer/rehacer
- Modo Reproducir/Pausar/Detener con restauración exacta de la escena
- Scripts C# compilados en memoria con Roslyn (`OnStart` / `OnUpdate` / `OnDestroy`)
- 41 pruebas automatizadas

### Cómo probarlo
    git clone https://github.com/TU_USUARIO/ollin.git
    cd ollin
    dotnet run --project src/Ollin.Editor

### Requisitos
Windows 10/11, .NET 8 SDK y una GPU con OpenGL ES 3.0.
```

## Cómo publicarlo

### Opción A — GitHub CLI

```powershell
gh auth login
gh repo create ollin --public --source . --remote origin --push `
  --description "Motor de juego 2D con editor propio en C#/.NET 8: viewport OpenGL, gizmos, historial, escenas JSON y scripts en C# compilados con Roslyn."
```

### Opción B — manual

1. Crea un repositorio **vacío** llamado `ollin` en GitHub (sin README ni licencia).
2. En la carpeta del proyecto:

```powershell
git branch -M main
git remote add origin https://github.com/TU_USUARIO/ollin.git
git push -u origin main
```

3. En GitHub, pulsa **About** (engranaje) y pega la descripción y los topics.
4. Ve a **Releases → Draft a new release**, etiqueta `v0.3.0` y pega las notas.

## Después de publicar

- [ ] Verificar que la pestaña **Actions** ejecuta la CI en verde.
- [ ] Comprobar que la licencia MIT aparece detectada.
- [ ] Reemplazar `TU_USUARIO` en `README.md`, `.github/ISSUE_TEMPLATE/config.yml` y en
      este documento por tu usuario real.
- [ ] Añadir la imagen social (Settings → General → Social preview).
- [ ] Etiquetar el primer release y fijar la descripción del repositorio.

## Notas

- El flujo de CI usa `windows-latest` porque el editor necesita OpenGL/D3D en Windows;
  las pruebas del núcleo son independientes de la plataforma.
- El repositorio no incluye binarios ni `bin/`, `obj/`: se compilan al clonar.
