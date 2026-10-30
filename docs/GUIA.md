# Guía de uso de Ollin

Esta guía explica qué es cada parte del editor y cómo usarla para construir un
prototipo 2D de principio a fin. Está pensada para leerse en orden la primera vez y
como referencia después.

---

## 1. Qué es Ollin

Ollin es un motor de juego 2D con editor propio, scripts en C# y renderizado con
OpenGL. Un proyecto de Ollin es una **carpeta independiente** con sus escenas,
scripts y assets; puedes tener tantos proyectos como quieras y no comparten nada.

Requisitos:

- Windows 10/11 de 64 bits.
- **.NET 8 Desktop Runtime** (el instalador no lo incluye).
- Una GPU con OpenGL 3.0 o superior (cualquier equipo de los últimos años).

---

## 2. Instalar y abrir

1. Ejecuta `Ollin-Setup-<versión>-beta.exe`.
2. El instalador copia el motor en `%LOCALAPPDATA%\Ollin` y crea un acceso directo
   en el escritorio y en el menú de inicio.
3. Abre **Ollin** desde el acceso directo.

La primera vez verás la pantalla de bienvenida con tres opciones: **crear proyecto**,
**abrir proyecto** o **abrir carpeta de ejemplo**. Las siguientes veces se abre
automáticamente el último proyecto.

> Windows puede mostrar un aviso de SmartScreen porque el instalador no está firmado.
> Es normal en una beta: elige «Más información → Ejecutar de todas formas».

---

## 3. Crear un proyecto

`Archivo → Nuevo proyecto…` (o el botón del escritorio de bienvenida).

- **Plantilla**: Vacío, Plataformas o Terror. Cada una crea una escena y un script
  inicial que ya funciona.
- **Nombre**: se usa también como nombre de la carpeta (los espacios pasan a `_`).
- **Carpeta**: dónde se creará. El diálogo tiene desplazamiento si la pantalla es
  pequeña y valida que la carpeta no esté ocupada.

El proyecto se crea con esta estructura:

```
MiJuego/
  project.ollin          Metadatos del proyecto (nombre y escena inicial)
  scenes/                Escenas (*.ollin.json)
  scripts/               Scripts C# (*.cs)
  assets/                Texturas, audio y demás recursos
  prefabs/               Entidades plantilla (*.prefab.json)
```

`Archivo → Cerrar proyecto` vuelve a la bienvenida y `Archivo → Abrir proyecto…`
carga cualquier carpeta con `project.ollin` (o una carpeta suelta, que se trata como
proyecto).

---

## 4. La interfaz

| Zona | Para qué sirve |
| --- | --- |
| **Menú superior** | Archivo (proyecto y escenas), Edición (deshacer, copiar/pegar, prefabs), Ver (cámara, cuadrícula), Escena (reproducir/pausar/detener) y Ayuda. |
| **Barra de herramientas** | Acciones frecuentes: proyecto, nueva escena, abrir, guardar, deshacer/rehacer, añadir entidad, duplicar, compilar scripts, centrar cámara, reproducir. |
| **Jerarquía** (izquierda) | Árbol de entidades de la escena activa. Tiene buscador, menú contextual y doble clic para enfocar. |
| **Assets** (izquierda) | Texturas del proyecto con miniatura. Doble clic o «Asignar» aplica la textura a la entidad seleccionada. |
| **Documentos** (centro) | Una pestaña por escena y por script abierto. La marca `●` indica cambios sin guardar. |
| **Viewport** (centro) | La escena en vivo. Aquí se selecciona, se mueven entidades y se pinta el tilemap. |
| **Inspector** (derecha) | Propiedades de la entidad en pestañas: Entidad, Visual, Audio, Mundo, Tilemap y Cámara. |
| **Consola / Archivos** (abajo) | Consola con mensajes y errores; explorador del proyecto para abrir, arrastrar y eliminar archivos. |
| **Barra de estado** | Mensajes, estado de scripts, reproducción, selección, zoom, FPS y llamadas de dibujo. |

### Escenas múltiples

Cada pestaña de escena guarda su propia escena, cámara, selección e historial de
deshacer. Cambiar de pestaña no pierde nada. Al cerrar una escena con cambios se
pide confirmación; no se puede cerrar la última ni cambiar de escena mientras se
reproduce.

---

## 5. Entidades, componentes y jerarquía

Una **entidad** es un objeto de la escena: tiene nombre, transformación (posición,
rotación, escala) y **componentes**. Al seleccionarla, el inspector muestra solo las
pestañas de los componentes que tiene.

Componentes disponibles (se añaden con **+ Añadir componente** o con el clic derecho
sobre la entidad → *Añadir componente*):

| Componente | Para qué sirve |
| --- | --- |
| **SpriteRenderer** | Dibuja una textura o un rectángulo de color. |
| **SpriteAnimator** | Anima una hoja de cuadros (ancho/alto de cuadro, FPS, bucle). |
| **Camera2D** | Cámara ortográfica de la escena. |
| **BoxCollider** | Caja de colisión. Marca *disparador* para que no bloquee. |
| **Rigidbody2D** | Cuerpo con velocidad y gravedad. Puede ser estático o cinemático. |
| **Tilemap** | Rejilla de mosaicos con tileset; puede colisionar por celda. |
| **AudioSource** | Sonido de la entidad (clip, volumen, bucle, música, fundido). |
| **ScriptComponent** | Enlaza un script C# a la entidad. |
| **Rotator / Oscillator** | Giran u oscilan la entidad sin escribir código. |

Acciones de jerarquía:

- **Crear hijo** (clic derecho): crea una entidad con `ParentId` apuntando a la
  seleccionada.
- **Duplicar**, **Eliminar**, **Guardar como prefab…**, **Enfocar en la cámara**.
- **Arrastrar** un `.cs` desde *Archivos* a una entidad: le enlaza el script.
- **Arrastrar** un `*.prefab.json` a una entidad o al viewport: lo instancia.

---

## 6. El viewport

| Acción | Cómo |
| --- | --- |
| Seleccionar | Clic sobre un sprite (o en la jerarquía). |
| Mover / rotar / escalar | Arrastra los tiradores del *gizmo* de la entidad seleccionada. |
| Mover la cámara | Alt + arrastrar, o botón central. |
| Zoom | Rueda del ratón. |
| Centrar selección | Doble clic en la jerarquía o tecla `F`. |
| Volver a la vista inicial | `Inicio` / botón *Centrar*. |
| Pintar mosaicos | Selecciona la entidad con Tilemap, activa *Pintar en el viewport* en la pestaña Tilemap y haz clic o arrastra. |

El panel superior del viewport muestra el nombre de la escena; el inferior, la ayuda
de controles.

---

## 7. Scripts en C#

`Edición → Abrir script de la selección`, doble clic en un `.cs` del explorador o
arrástralo a una entidad.

El editor tiene resaltado de sintaxis, numeración de líneas y **autocompletado**:

- `Ctrl+Espacio`: palabras clave, tipos, API de Ollin, entidades de la escena y
  clases de script del proyecto.
- Al escribir `.`: miembros (`Position`, `Velocity`, `Size`, `Transform`…).

`Ctrl+S` guarda y recompila. Si estás reproduciendo, la recarga es **en caliente**:
se conserva el estado de los scripts. Si el archivo cambia por fuera y no tienes
cambios sin guardar, la pestaña se actualiza sola.

Un script mínimo:

```csharp
using System.Numerics;
using Ollin.Core.Input;
using Ollin.Scripting;

public sealed class MiScript : OllinScript
{
    public override void OnStart()
    {
        Debug("¡Hola desde " + Entity.Name + "!");
    }

    public override void OnUpdate(float deltaTime)
    {
        var speed = 4f;
        if (Input.GetKey(KeyCode.A)) Entity.Transform.Position += new Vector2(-speed * Time.DeltaTime, 0f);
        if (Input.GetKey(KeyCode.D)) Entity.Transform.Position += new Vector2(speed * Time.DeltaTime, 0f);

        if (Input.GetKeyDown(KeyCode.Space))
        {
            Audio.Play("assets/audio/jump.wav", 0.7f);
            StartCoroutine(WaitForSeconds(0.5f));
        }
    }

    public override void OnCollisionEnter(Ollin.Core.Model.Entity other)
    {
        Debug("Choque con " + other.Name);
    }
}
```

API disponible:

| API | Uso |
| --- | --- |
| `Input.GetKey(KeyCode.X)` | Tecla mantenida. |
| `Input.GetKeyDown(KeyCode.X)` | Tecla pulsada este frame. |
| `Time.DeltaTime` / `Time.Elapsed` | Tiempo del frame / desde el inicio. |
| `Entity.Transform` | Posición, rotación y escala. |
| `Entity.GetComponent<T>()` | Acceso a componentes. |
| `Scene.Find("Nombre")` | Buscar otra entidad. |
| `Debug(...)`, `Warn(...)`, `Error(...)` | Consola. |
| `Audio.Play(...)`, `Audio.Music(...)`, `Audio.StopAll()` | Sonido. |
| `StartCoroutine(WaitForSeconds(n))` | Esperas sin bloquear. |

Callbacks: `OnStart`, `OnUpdate`, `OnReload`, `OnDestroy`, `OnCollisionEnter`,
`OnTriggerEnter`, `OnTriggerExit`.

---

## 8. Diálogos e interacción

Los diálogos son archivos JSON en `dialogs/`:

```json
{
  "id": "nota",
  "speaker": "Nota",
  "lines": [
    { "text": "Primera línea." },
    { "speaker": "Otro", "text": "Segunda línea con otro hablante." }
  ]
}
```

Para usarlos:

1. Añade el componente **Interactable** a una entidad (inspector → *Añadir componente*).
2. Configura **Aviso** («E: leer la nota»), **Radio**, **Diálogo** (`nota`) y *Solo una vez*.
3. La entidad del jugador debe tener la etiqueta **`player`** (inspector → *Etiquetas*).
4. Pulsa **Reproducir**: al acercarte aparece el aviso; con **E** se abre el diálogo y
   con **E**, **Espacio** o **Intro** avanzas.

Desde un script:

```csharp
Dialogue.Play("nota");
var activo = Dialogue.IsActive;
```

También puedes implementar `OnInteract(Entity other)` en el script de la entidad
interactuable para hacer algo además del diálogo.

## 9. Luces y atmósfera

- **Añadir una luz**: inspector → *+ Añadir componente* → **Luz 2D**.
- Propiedades: **Color**, **Radio**, **Intensidad**, tipo **cono** (linterna),
  **Ángulo** del cono y **Parpadeo** (0 = fija).
- La **luz ambiente** se configura en la cámara (pestaña *Cámara* → *Ambiente*).
  Blanco = sin oscurecer; valores bajos oscurecen la escena.
- **Viñeta**: se aplica automáticamente cuando hay ambiente o luces.
- **Paralaje**: en el sprite, *Paralaje* 1 = mundo normal, 0 = fijo a la cámara,
  0.5 = se mueve a mitad de velocidad (fondos).
- **Cámara con seguimiento**: en la cámara, *Seguir* = etiqueta del objetivo
  (`player`) y *Suavizado*. Funciona durante la reproducción.

> **Sombras (experimental):** la casilla *Proyectar sombras* de una luz puntual proyecta la
> silueta de los colliders. Está desactivada por defecto y aún es tosca; el post-proceso
> (niebla, grano) tampoco está.

## 10. HUD y menús

Desde cualquier script:

```csharp
Hud.SetBar("Vida", 75f, 100f);          // barra con nombre, valor y máximo
Hud.SetLabel("Monedas", "3");           // etiqueta de texto
Hud.Message("¡Bienvenido!", 3f);        // mensaje temporal
Hud.GameOver("Has caído");              // pantalla de fin de partida
Hud.Clear();
```

- Las barras y etiquetas se dibujan arriba a la izquierda; los mensajes, arriba al centro.
- **Escape** durante la reproducción abre el **menú de pausa**: Continuar, Reiniciar la
  escena, Detener y **volumen maestro**.
- La pantalla de **fin de partida** aparece cuando un script llama a `Hud.GameOver` y
  ofrece Reintentar o Detener.
- Guardar y cargar partida llegarán con el sistema de guardado.

## 11. Audio

Añade un **AudioSource** a una entidad:

- **Clip**: ruta relativa, por ejemplo `assets/audio/ambient.wav`.
- **Volumen**, **bucle**, **entrada (fade)**.
- **Reproducir al iniciar**: suena al pulsar Reproducir.
- **Canal de música**: corta la música anterior.

Puedes generar o copiar tus `.wav`/`.mp3` en `assets/audio/`. El proyecto de ejemplo
incluye `jump`, `coin`, `hit` y `ambient`.

---

## 12. Prefabs, copiar y pegar

- **Guardar como prefab**: clic derecho sobre la entidad → *Guardar como prefab…*.
  Se guarda en `prefabs/nombre.prefab.json`.
- **Instanciar**: doble clic en el explorador o arrástralo al viewport/jerarquía.
- **Copiar / pegar**: `Ctrl+C` y `Ctrl+V` duplican la entidad con nombre único y un
  pequeño desplazamiento.
- **Duplicar**: `Ctrl+D` disponible en el menú de edición y en el clic derecho.

---

## 13. Reproducir y depurar

| Acción | Cómo |
| --- | --- |
| Reproducir | Botón ▶ o `F5`. |
| Pausar / reanudar | `F6`. |
| Detener y restaurar | Botón ⏹ (la escena vuelve al estado anterior). |

Durante la reproducción el teclado y el ratón pertenecen al juego. La consola muestra
los mensajes de `Debug`, errores de scripts y avisos de audio.

---

## 14. Archivos del proyecto

Desde la pestaña **Archivos** del dock inferior:

- **Doble clic** abre scripts, escenas, asigna texturas e instancia prefabs.
- **Clic derecho → Eliminar…** borra un archivo (con confirmación). Si estaba abierto,
  se cierra su pestaña; si era un script, se recompila.
- **Arrastrar** un archivo a la jerarquía o al viewport.

Formato de escena: JSON legible con versión de formato, entidades y componentes con
discriminador `$type`. Puedes versionarlo con git sin problemas.

---

## 15. Atajos de teclado

| Atajo | Acción |
| --- | --- |
| `Ctrl+N` | Nueva escena |
| `Ctrl+O` | Abrir escena |
| `Ctrl+S` | Guardar escena (o script si está activo) |
| `Ctrl+Z` / `Ctrl+Y` | Deshacer / rehacer |
| `Ctrl+C` / `Ctrl+V` | Copiar / pegar entidad |
| `Ctrl+Espacio` | Autocompletar en el editor de scripts |
| `Ctrl+F5` (menú) | Reproducir |
| `F5` / `F6` | Reproducir / pausar |
| `F` | Enfocar selección |
| `Inicio` | Centrar cámara |
| `Supr` | Eliminar entidad |
| `Escape` | Deseleccionar |
| Rueda | Zoom del viewport |
| Alt + arrastrar | Mover cámara |

---

## 16. Problemas comunes

| Síntoma | Causa y solución |
| --- | --- |
| «No se pudo iniciar el viewport» | GPU sin OpenGL 3.0 o controladores antiguos. Actualiza los controladores. |
| No suena el audio | No hay dispositivo de salida o el clip no existe. Revisa la consola. |
| El instalador no arranca | Falta .NET 8 Desktop Runtime. Instálalo desde Microsoft. |
| SmartScreen bloquea el instalador | Beta sin firmar: «Más información → Ejecutar de todas formas». |
| Un script no aparece | Debe ser `public sealed class` con el mismo nombre que el archivo. |
| Cambios de escena no guardados | La pestaña muestra `●`; usa `Ctrl+S` o *Guardar*. |

---

## 17. Flujo recomendado para un prototipo

1. Crea un proyecto con la plantilla **Plataformas**.
2. Abre `scenes/inicio.ollin.json` (pestaña **Escena**).
3. Ajusta el **tilemap** del suelo y marca *Colisiona*.
4. Añade entidades y asígnales sprites desde **Assets**.
5. Escribe el movimiento en `scripts/Plataformas.cs` (autocompletado con `Ctrl+Espacio`).
6. Añade **AudioSource** para saltos y monedas.
7. Guarda la escena, pulsa **Reproducir** y ajusta.
8. Convierte enemigos u obstáculos en **prefabs** para reutilizarlos.

Con eso tienes el ciclo completo: escena → componentes → scripts → sonido → prueba.
