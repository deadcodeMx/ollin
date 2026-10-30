# Publicar Ollin en GitHub

Este documento reúne el texto listo para copiar en GitHub y los pasos para publicar el
repositorio.

## Nombre del repositorio

```
ollin
```

## Descripción corta (campo *About*)

> Motor de juego 2D con editor propio en C#/.NET 8: proyectos independientes, escenas
> múltiples, viewport OpenGL, audio, prefabs, autocompletado y scripts en C# con
> recarga en caliente. «Ollin» significa movimiento en náhuatl.

Versión de una línea (para redes o pie de página):

> Motor de juego 2D con editor propio y scripting en C#, escrito desde cero en .NET 8.

## Descripción larga

Ollin es un motor de juego 2D con editor integrado, escrito desde cero en C#/.NET 8.
No envuelve otro motor: el renderizado, el modelo de escena, el bucle de juego, el
historial y el compilador de scripts son código propio.

El editor (Avalonia) incluye proyectos independientes, escenas múltiples en pestañas,
viewport OpenGL ES 3.0, inspector por pestañas, gizmos, deshacer/rehacer, panel de
assets con miniaturas, explorador de archivos, audio 2D, prefabs y autocompletado de C#.
Las escenas se guardan en JSON legible y los scripts se compilan en memoria con Roslyn
y se ejecutan con el ciclo de vida `OnStart`/`OnUpdate`/`OnDestroy`, con recarga en
caliente al guardar.

Motor generalista para cualquier tipo de juego 2D: no impone género ni estilo. El proyecto avanza por etapas documentadas y ya tiene cerrada la etapa 4: animación,
tilemaps con colisión por celda, física, audio, prefabs y herramientas de edición.
La siguiente etapa son las herramientas de juego 2D (luces, triggers, diálogos, inventario, IA).

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
| Social preview | `docs/images/logo.png` (o `docs/images/editor.png`) |

## Releases: cómo se distribuyen las actualizaciones

Cada versión del motor se publica como un **release** con su instalador adjunto. La
página de descargas del README apunta siempre a
[Releases](https://github.com/deadcodeMx/ollin/releases), de modo que el usuario
siempre descarga la última beta.

Título y notas sugeridas para la versión actual (0.9.0):

```markdown
## Ollin 0.9.0 beta — editor completo y etapa 4 cerrada

### Incluye
- Proyectos independientes con plantillas (vacío, plataformas, terror)
- Escenas múltiples en pestañas con estado propio
- Editor de scripts con resaltado, autocompletado y recarga en caliente
- Audio 2D, prefabs, copiar/pegar, buscador y menús contextuales
- Colisión por celda en tilemaps y cuerpos cinemáticos
- Arrastrar scripts y prefabs a entidades

### Instalación
1. Descarga `Ollin-Setup-0.9.0-beta.exe`.
2. Ejecútalo (Windows puede avisar por no estar firmado).
3. Abre Ollin desde el acceso directo.

Requiere .NET 8 Desktop Runtime.
```

Para generar el instalador y (si tienes `gh`) publicar el release:

```powershell
powershell -ExecutionPolicy Bypass -File tools/release.ps1
```

Sin `gh`, el script deja el instalador en `dist/installer/` y muestra los pasos para
subirlo a mano desde *Releases → Draft a new release*.
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
git remote add origin https://github.com/deadcodeMx/ollin.git
git push -u origin main
```

3. En GitHub, pulsa **About** (engranaje) y pega la descripción y los topics.
4. Ve a **Releases → Draft a new release**, etiqueta `v0.3.0` y pega las notas.

## Después de publicar

- [x] Enlaces con el usuario real (`deadcodeMx`) en el README, las plantillas y esta guía.
- [ ] Verificar que la pestaña **Actions** ejecuta la CI en verde.
- [ ] Comprobar que la licencia MIT aparece detectada.
- [ ] Añadir la imagen social (Settings → General → Social preview).
- [ ] Etiquetar el primer release y fijar la descripción del repositorio.

## Notas

- El flujo de CI usa `windows-latest` porque el editor necesita OpenGL/D3D en Windows;
  las pruebas del núcleo son independientes de la plataforma.
- El repositorio no incluye binarios ni `bin/`, `obj/`: se compilan al clonar.
