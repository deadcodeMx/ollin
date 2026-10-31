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

Al abrir Ollin aparece el **lanzador de proyectos**: una lista con los proyectos
recientes y botones para **crear** uno nuevo o **abrir** una carpeta existente. El
editor se abre después de elegir un proyecto.

> Windows puede mostrar un aviso de SmartScreen porque el instalador no está firmado.
> Es normal en una beta: elige «Más información → Ejecutar de todas formas».

---

## 3. Crear un proyecto

`Archivo → Nuevo proyecto…` (o el botón del escritorio de bienvenida).

- **Plantilla**: Vacío (solo la cámara), Plataformas, Terror o Tutorial. La de
  Tutorial crea un recorrido guiado con HUD, inventario, diálogo, IA y guardado.
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

- **Temas**: `Ver → Tema` ofrece **Oscuro**, **Claro** y **Ollin** (colores del logo).
  Se recuerda entre sesiones.
- **Ventana de juego**: `Ver → Ventana de juego` (Ctrl+G) abre una ventana aparte con el
  viewport del juego. Puedes moverla a otro monitor y pulsar **F11** para pantalla
  completa; **Esc** la devuelve a ventana.

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

## 11. Inventario, estadísticas y día/noche

**Inventario** (`Inventory`): capacidad y pilas de objetos.

```csharp
var bag = Entity.GetComponent<Inventory>();
bag?.Add("pocion", 2);            // hasta el máximo de la pila
bag?.Remove("moneda", 1);
if (bag?.Has("pocion", 1) == true) { /* usar poción */ }
```

Los objetos se definen en `items/*.json` (`id`, `name`, `maxStack`). Desde el inspector,
pestaña **Inventario**, puedes ajustar la capacidad y añadir o quitar objetos con
`pocion x3`.

**Estadísticas** (`Stats`): valores con máximo y desgaste por segundo.

```csharp
var stats = Entity.GetComponent<Stats>();
stats?.Add("Vida", -10f);
var vida = stats?.Get("Vida") ?? 0f;
```

En el inspector, pestaña **Estadísticas**, escribe `vida=80`, `hambre+10` o `energía-5`.

**Día y noche** (`DayNightCycle`): añádelo a la entidad de la cámara.

- **Duración (s)**: lo que tarda un día completo.
- **Hora**: 0 = medianoche, 0.5 = mediodía.
- **Luz de día / Luz de noche**: colores que se interpolan.
- El runtime aplica el ambiente resultante a las cámaras.

## 12. IA y guardado

**IA** (`AiAgent`): añade el componente a un enemigo y configura:

- **Objetivo**: etiqueta a perseguir (`player`).
- **Velocidad**, **Visión**, **Patrulla** (radio alrededor de su posición inicial) y **Memoria**
  (segundos que sigue buscando al objetivo tras perderlo de vista).
- El agente patrulla y, cuando ve al objetivo, lo persigue. Si hay un **tilemap con
  “Colisiona”**, calcula la ruta con **A\***; si no, va en línea recta.
- En el inspector, pestaña **IA**, se ve el estado actual (patrullando/persiguiendo).

**Guardado de partida**:

- `Escena → Guardar partida` o el botón del menú de pausa.
- `Escena → Cargar partida` (también en pausa) restaura la escena y reanuda el juego.
- Se guarda en `saves/partida.json` con la escena completa: posiciones, inventario,
  estadísticas, luces y demás componentes.
- **Datos de script**: usa `Save.Set/Get` para guardar lo tuyo (puertas, misiones,
  contadores). Se incluyen en la partida automáticamente.

```csharp
// Al abrir una puerta
Save.SetFlag("puerta_1", true);

// En OnStart, para restaurar el estado
if (Save.GetFlag("puerta_1"))
{
    Entity.Transform.Position = new Vector2(10f, 0f);
}

// Crafteo sencillo con el inventario (2 monedas -> 1 poción)
var bag = Entity.GetComponent<Inventory>();
if (bag is not null && bag.Has("moneda", 2) && bag.Add("pocion", 1) == 1)
{
    bag.Remove("moneda", 2);
}
```

## 13. Audio

Añade un **AudioSource** a una entidad:

- **Clip**: ruta relativa, por ejemplo `assets/audio/ambient.wav`.
- **Volumen**, **bucle**, **entrada (fade)**.
- **Reproducir al iniciar**: suena al pulsar Reproducir.
- **Canal de música**: corta la música anterior.

Puedes generar o copiar tus `.wav`/`.mp3` en `assets/audio/`. El proyecto de ejemplo
incluye `jump`, `coin`, `hit` y `ambient`.

---

## 14. Prefabs, copiar y pegar

- **Guardar como prefab**: clic derecho sobre la entidad → *Guardar como prefab…*.
  Se guarda en `prefabs/nombre.prefab.json`.
- **Instanciar**: doble clic en el explorador o arrástralo al viewport/jerarquía.
- **Copiar / pegar**: `Ctrl+C` y `Ctrl+V` duplican la entidad con nombre único y un
  pequeño desplazamiento.
- **Duplicar**: `Ctrl+D` disponible en el menú de edición y en el clic derecho.

---

## 15. Reproducir y depurar

| Acción | Cómo |
| --- | --- |
| Reproducir | Botón ▶ o `F5`. |
| Pausar / reanudar | `F6`. |
| Detener y restaurar | Botón ⏹ (la escena vuelve al estado anterior). |

Durante la reproducción el teclado y el ratón pertenecen al juego. La consola muestra
los mensajes de `Debug`, errores de scripts y avisos de audio.

---

## 16. Archivos del proyecto

Desde la pestaña **Archivos** del dock inferior:

- **Doble clic** abre scripts, escenas, asigna texturas e instancia prefabs.
- **Clic derecho → Eliminar…** borra un archivo (con confirmación). Si estaba abierto,
  se cierra su pestaña; si era un script, se recompila.
- **Arrastrar** un archivo a la jerarquía o al viewport.

Formato de escena: JSON legible con versión de formato, entidades y componentes con
discriminador `$type`. Puedes versionarlo con git sin problemas.

---

## 17. Atajos de teclado

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

## 18. Problemas comunes

| Síntoma | Causa y solución |
| --- | --- |
| «No se pudo iniciar el viewport» | GPU sin OpenGL 3.0 o controladores antiguos. Actualiza los controladores. |
| No suena el audio | No hay dispositivo de salida o el clip no existe. Revisa la consola. |
| El instalador no arranca | Falta .NET 8 Desktop Runtime. Instálalo desde Microsoft. |
| SmartScreen bloquea el instalador | Beta sin firmar: «Más información → Ejecutar de todas formas». |
| Un script no aparece | Debe ser `public sealed class` con el mismo nombre que el archivo. |
| Cambios de escena no guardados | La pestaña muestra `●`; usa `Ctrl+S` o *Guardar*. |

---

## 19. Flujo recomendado para un prototipo

1. Crea un proyecto con la plantilla **Plataformas**.
2. Abre `scenes/inicio.ollin.json` (pestaña **Escena**).
3. Ajusta el **tilemap** del suelo y marca *Colisiona*.
4. Añade entidades y asígnales sprites desde **Assets**.
5. Escribe el movimiento en `scripts/Plataformas.cs` (autocompletado con `Ctrl+Espacio`).
6. Añade **AudioSource** para saltos y monedas.
7. Guarda la escena, pulsa **Reproducir** y ajusta.
8. Convierte enemigos u obstáculos en **prefabs** para reutilizarlos.

Con eso tienes el ciclo completo: escena → componentes → scripts → sonido → prueba.

## 20. Empaquetar el juego

Cuando el proyecto esté listo: **Archivo → Exportar juego…** (o el botón **⬆ Exportar**).
Elige si quieres pantalla de inicio y marca de agua, y luego la carpeta de destino.
Ollin crea una subcarpeta con el nombre del proyecto y **solo tres archivos**:

- `<Juego>.exe` — el juego completo en un único ejecutable.
- `game.pak` — paquete **cifrado** con escenas, assets, diálogos, objetos y los
  **scripts ya compilados** (`game.dll`).
- `LEEME.txt` — controles.

**El código fuente no viaja en el paquete**: los `.cs` se compilan al exportar y solo se
incluye el ensamblado. `game.pak` usa cifrado AES-256-GCM; el reproductor lo descifra en
memoria y extrae los assets a una carpeta temporal para poder cargarlos.

Al ejecutar `<Juego>.exe` aparece la **pantalla de inicio con el logo de Ollin** y,
durante el juego, una marca de agua «Hecho con Ollin». Ambas se pueden desactivar en el
diálogo de exportación.

**Controles**: `F11` pantalla completa, `Esc` salir (o salir de pantalla completa). El juego muestra el **HUD y los diálogos**; avanza el diálogo con **Enter/E/Espacio**.

**Requisitos**: .NET 8 Desktop Runtime (el reproductor no es autocontenido). El editor
necesita la carpeta `player/` junto al ejecutable; el instalador ya la incluye.

**Limitaciones honestas**: esto protege el código fuente de miradas casuales, pero no es
DRM infalible: un usuario con conocimientos puede decompilar el ensamblado o inspeccionar
la carpeta temporal. Las claves van dentro del ejecutable.

## 21. Modo 2.5D en primera persona

La etapa 6 añade un puente 2.5D sobre el renderer 2D. El mundo usa el plano **X/Z**:
la **X** del transform es el ancho y la **Y** la profundidad; la altura de los ojos se
configura en la cámara.

Componentes:

- **Cámara 3D**: FOV, altura de ojos, giro (Yaw), inclinación (Pitch), fondo, niebla de
  distancia y luz ambiente. Si existe una cámara 3D activa, el viewport cambia solo a
  primera persona.
- **Controlador FPS**: WASD/flechas para moverse, ratón para mirar, **Shift** para
  correr y colisión contra cajas 3D. Añádelo a la misma entidad que la cámara.
- **Pared 3D**: caja con ancho, alto, profundidad, color/textura y opción de colisión.
  Sirve para paredes, puertas, suelos y techos (las cajas finas sin colisión hacen de
  suelo/techo).

Atajos del flujo:

1. Crea un proyecto con la plantilla **2.5D (pasillo)**.
2. Pulsa **Reproducir** y muévete con WASD; el ratón mira.
3. La puerta se abre con **E** al acercarte.
4. Exporta el juego como siempre: el HUD y los diálogos también aparecen.

**Limitaciones de esta primera entrega**: el orden de dibujado es por profundidad
(pintor, sin búfer de profundidad), las paredes son cajas de color liso, aún no hay
cilindros, gizmos 3D en el viewport ni IA 3D. Está pensado para pasillos y estética
retro tipo PS1/Doom.

## 22. Crear un juego 2.5D sin programar

La plantilla **2.5D (pasillo)** es un mini-juego completo sin una línea de código. Los
comportamientos se añaden desde **clic derecho → Añadir componente** y se ajustan en el
inspector:

| Componente | Para qué sirve |
| --- | --- |
| **Salud** | Vida máxima/actual; desactivar al morir; mostrar barra en el HUD |
| **Arma** | Disparo desde la cámara con clic izquierdo; daño, alcance, cadencia y munición |
| **Recogible** | Añade un objeto al inventario o cura; radio y mensaje |
| **Puerta** | Se abre con E si el jugador tiene la llave; la caja sube |
| **Reglas de juego** | Victoria al eliminar a los enemigos; derrota si el jugador muere |
| **Mensaje inicial** | Texto de ayuda al empezar, sin scripts |
| **IA 3D** | Patrulla, visión con línea de vista, persecución y ataque |

Flujo típico sin programar:

1. Crea un proyecto con la plantilla **2.5D (pasillo)**.
2. La cámara ya trae Salud, Arma, Inventario, Reglas y Mensaje.
3. La **Sombra** trae IA 3D con ataque y Salud; la **Llave** es un Recogible; la
   **Puerta** pide `llave`.
4. Ajusta los valores en el inspector, reproduce y exporta.

Para tu propio nivel: coloca cajas 3D, asigna texturas, añade un enemigo con IA 3D +
Salud, coloca una llave y una puerta, y pon las Reglas en la cámara. No hace falta C#.

## 23. Navegación, nivel, combate y pausa (0.27)

### Navegación A*
La IA 3D usa una rejilla derivada de las paredes y **A\*** para rodear obstáculos cuando
no hay línea de vista. Se activa con `UseNavigation` (por defecto) y se ajusta con
`NavCellSize` y `NavRefreshSeconds`. Las puertas abiertas dejan de bloquear la rejilla.

### Herramientas de nivel
- **Pared / Suelo**: activa el pincel y haz clic en el viewport 3D para colocar cajas
  con textura y tamaño por defecto. Vuelve a pulsar para desactivarlo.
- **Imán**: ajusta mover/escalar/colocar a la rejilla (`SnapStep`, 0.5 por defecto).
- **Serie x4**: duplica la selección cuatro veces en fila, en una sola operación de
  deshacer.

### Combate
- **Dos armas** en la cámara: Pistola (ranura 1) y Escopeta (ranura 2), con **1/2** para
  cambiar, **R** para recargar y cargador + reserva.
- **Proyectiles**: la escopeta lanza un proyectil que choca con paredes y daña salud.
- **Reaparición**: el jugador tiene 3 vidas y reaparece en el último **Checkpoint**.
  Las reglas no dan derrota mientras queden vidas.

### Pausa, opciones y guardado (juego exportado)
- **Esc** o el botón **II Pausa** abre el menú.
- **Continuar**, **Guardar partida**, **Cargar partida**, **Opciones** y **Salir**.
- Opciones: volumen, sensibilidad del ratón y pantalla completa.
- Las partidas se guardan en `%APPDATA%\Ollin\Saves\<juego>.json`; la configuración en
  `%APPDATA%\Ollin\player-settings.json`.

### Más comportamientos
| Componente | Uso |
| --- | --- |
| **Botón 3D** | Se pulsa con E y ejecuta una acción sobre otra entidad |
| **Trigger 3D** | Ejecuta una acción al entrar en su radio |
| **Generador** | Clona una entidad plantilla cada cierto tiempo |
| **Checkpoint** | Actualiza el punto de reaparición y cura |

Acciones disponibles: mensaje, abrir puerta, activar, desactivar, generar, checkpoint y ganar.

## 24. Scripting visual y componentes de mundo (0.28)

### Script visual por reglas
Añade **Script visual** a una entidad. Tiene cuatro reglas; cada una combina:

**Evento** → **Condición** → **Acción**.

- Eventos: al iniciar, cada cierto tiempo, al entrar el jugador, al pulsar E o al morir.
- Condiciones: siempre, tener un objeto o vida por debajo de un valor.
- Acciones: mensaje, abrir puerta, activar/desactivar, generar, checkpoint, ganar,
  dañar, curar o teletransportar.

Cada regla tiene objetivo, mensaje, radio, intervalo, cantidad y «solo una vez». No hace
falta escribir código.

### Componentes nuevos
| Componente | Uso |
| --- | --- |
| **Peligro 3D** | Zona que daña por segundo (lava, pinchos) |
| **Teletransporte** | Mueve al jugador a una entidad destino al entrar |
| **Seguidor 3D** | Compañero que sigue al jugador y colisiona con paredes |

### Pincel configurable
La pestaña **Nivel** define ancho, alto, fondo del pincel, tamaño del suelo y los valores
de **Serie x4** (cantidad y separación). Las herramientas **Pared**, **Suelo**, **Imán** y
**Serie x4** están en la barra superior.

## 25. Grafo de nodos, pincel visual, armas y guardado (0.29)

### Grafo de nodos ilimitado
Selecciona una entidad y pulsa **Grafo** en la barra (o añade **Grafo de nodos**).
El editor tiene:

- Paleta con eventos, condiciones y acciones.
- Canvas con nodos arrastrables y conexiones.
- **Conectar →** en una salida y luego clic en el encabezado del nodo destino.
- Condiciones con ramas **Sí** y **No**.
- Panel derecho para editar el nodo seleccionado y eliminarlo.

Nodos disponibles: inicio, temporizador, entrar, pulsar E, muerte; condiciones de objeto
o vida; y todas las acciones (mensaje, puerta, activar, generar, checkpoint, ganar, dañar,
curar, teletransportar). No hay límite de nodos.

### Pincel con vista previa
La pestaña **Nivel** permite elegir:

- Ancho, alto, fondo y tamaño de suelo.
- **Textura de pared** y **textura de suelo** con el ratón.
- Cantidad y separación de la serie.

Al activar **Pared** o **Suelo**, una caja verde sigue al puntero y muestra exactamente
dónde se colocará.

### Editor de armas por ranuras
La pestaña **Arma** tiene un selector de **ranura 1/2/3**. Cada ranura se edita por
separado (nombre, daño, cargador, reserva, recarga, proyectil). El botón **Añadir arma**
crea la siguiente ranura libre. En el juego, **1/2/3** cambia de arma.

### Guardado por ranuras y autoguardado
En la pausa del juego exportado hay **tres ranuras** de guardado/carga y un
**autoguardado cada 45 s**. Los archivos van a `%APPDATA%\Ollin\Saves` con el nombre del
juego y la ranura.

## 26. Variables y expresiones en el grafo (0.30)

El grafo puede usar **variables globales** de la partida, que se guardan con la partida.

### Variables
- **Fijar variable**: asigna un valor (`contador = 2`).
- **Sumar variable**: suma una cantidad (`contador += 1`).
- **Alternar flag**: invierte un booleano (`abierto`).

### Condición de expresión
En una condición elige **Expresión** y escribe, por ejemplo:

- `contador >= 3`
- `contador == 5 && tiene("llave")`
- `vida < 40`
- `abierto == true`
- `2 + 3 * 4 == 14`

También puedes usar `vida`, `vidamax`, `vidas`, `min(...)`, `max(...)`.

### Ejemplo de contador
1. Evento **Al iniciar**.
2. Acción **Fijar variable**: `contador` = `0`.
3. Acción **Sumar variable**: `contador` += `1`.
4. Condición **Expresión**: `contador >= 3`.
5. Rama **Sí** → abrir puerta o ganar.

Las variables se guardan en la partida; al cargar, los contadores y flags continúan.

## 27. Demo técnico de la etapa 6

La plantilla **Demo técnico 2.5D** es un nivel completo para probar el motor:

- Jugador con **tres armas** (pistola, escopeta y rifle), recarga y reaparición.
- Tres enemigos con IA y navegación A\*: dos Sombras y un **Guardián**.
- **Llave**, puerta cerrada, **checkpoint**, **lava**, **portal**, **botiquín** y **trigger**.
- **Contador de bajas** visible en el HUD y variables del grafo.
- Victoria al eliminar a los tres enemigos; derrota al quedarse sin vidas.

Para crearlo: **Proyecto → Nuevo → Demo técnico 2.5D**. La demo técnica ya no se
publica como ZIP en las releases; se genera desde el editor.

Controles: **WASD + ratón**, **1/2/3** armas, **R** recargar, **E** interactuar,
**Esc** pausa.

## 28. Ratón y cámara FPS (0.31.1)

Al reproducir, el juego **bloquea el puntero** y oculta el cursor:
- El ratón gira la cámara y no puede salir de la ventana.
- **Esc** abre la pausa; el cursor vuelve a aparecer para usar el menú.
- Al continuar, el puntero se recentra y se vuelve a bloquear.
- En el editor, el bloqueo se activa solo mientras se reproduce la escena.

Ajustes por entidad en **FPS**:
- **Sens. X** y **Sens. Y** separadas.
- **Invertir eje Y**.
- **Suavizado** de cámara (0 = directo).

En el juego exportado, **Opciones** incluye sensibilidad horizontal, vertical, invertir Y y
suavizado, además de volumen y pantalla completa. La configuración se guarda en
`%APPDATA%\Ollin\player-settings.json`.

## 29. Editor de grafo: historial, zoom y depuración (0.32)

### Historial
- **Deshacer** y **Rehacer** con los botones del grafo o **Ctrl+Z / Ctrl+Y**.
- El historial guarda hasta 80 instantáneas del grafo.
- Cubre crear, borrar, mover, conectar, pegar y cambiar propiedades.

### Copiar y pegar
- **Copiar** / **Pegar** con los botones o **Ctrl+C / Ctrl+V**.
- Se copia el nodo seleccionado, sin sus conexiones, y se pega desplazado.

### Zoom
- Botones **− / 100% / + / Ajustar** y **Ctrl + rueda**.
- El zoom va de 35% a 200%.
- El lienzo se desplaza con las barras del ScrollViewer.

### Validación
El panel derecho muestra avisos y errores automáticamente:
- grafo sin eventos;
- eventos sin salida;
- condiciones sin rama Sí/No;
- acciones que necesitan objetivo;
- objetivos que no existen en la escena;
- variables sin nombre o expresión;
- enlaces rotos;
- ciclos de ejecución.

Los nodos con error se marcan en rojo y los avisos en ámbar.

### Depuración visual
- Mientras se reproduce, el **último nodo ejecutado** se resalta en cian.
- El panel muestra las variables actuales de la partida.
- La ventana del grafo ya no es modal: puede quedarse abierta mientras se reproduce.

## 30. Pruebas de guardado e interfaz (0.33)

### Guardado
- Tres ranuras independientes con `SaveSlotService`.
- Autoguardado separado con sufijo `-auto`.
- El jugador usa `%APPDATA%\Ollin\Saves\<juego>-slotN.json`.
- Los ajustes usan `PlayerSettings` y `%APPDATA%\Ollin\player-settings.json`.

### Pruebas automáticas
- `SaveSlotService`: guardar/cargar ranuras separadas y autoguardado.
- `AutosaveScheduler`: dispara exactamente cada 45 s.
- `PlayerSettings`: normaliza volumen, sensibilidad, inversión y suavizado.
- `NodeGraphHistory`: deshacer/rehacer.
- `GraphEditing`: copiar/pegar sin enlaces.
- `GraphValidator`: objetivos ausentes y ciclos.
- Ratón: delta bloqueado, sensibilidad X/Y, invertir Y y suavizado.

### Smoke test de interfaz
`tools/ui-smoke.ps1`:
1. Ejecuta las 171 pruebas.
2. Abre el demo en el reproductor.
3. Envía **Esc** dos veces y comprueba pausa/reanudación.
4. Abre el editor con `--graph` y verifica que aparezca la ventana del grafo.

Requiere una sesión de escritorio de Windows. No sustituye a pruebas de clic en cada
botón, pero cubre el flujo real de ventana y foco.

## 31. Robustez de escenas (0.34)

- **Carga tolerante**: `SceneSerializer.TryLoad` devuelve un mensaje con línea/columna
  en vez de lanzar una excepción a la interfaz.
- El editor informa del error en la consola y no reemplaza la escena actual.
- **Validación de assets**: texturas, animaciones, audio, scripts, proyectiles y armas.
  Los faltantes aparecen como avisos y en la barra de estado.
- **Validación de jerarquía**: padre inexistente, auto-parentesco, ciclos e IDs duplicados.
- **Entidades hijas**: `ParentId` y posiciones locales se conservan al guardar/cargar;
  `Scene.GetWorldPosition` sigue funcionando tras la carga.
- **GameState**: se limpia al cambiar de escena; al cargar una partida se restaura desde
  el guardado.

## 32. Transformaciones de mundo en 2.5D (0.35)

Las entidades hijas ya usan la posición de mundo en el modo 2.5D:

- **Render**: paredes, sprites, luces y gizmos usan `Scene.GetWorldPosition`.
- **Cámara**: `Camera3D.Eye` incluye la posición del padre.
- **Movimiento**: jugador, IA y seguidor se mueven en mundo y se convierten a local con
  `Scene.SetWorldPosition`.
- **Colisión**: cajas, cilindros y navegación A* usan límites de mundo.
- **Interacción**: recogibles, puertas, triggers, botones, checkpoints, peligro,
  teletransporte, grafo y armas comparan distancias de mundo.
- **Gizmos**: seleccionar y mover un hijo usa su centro de mundo y convierte el arrastre
  de vuelta a local.

`Transform.Position` sigue siendo la posición **local**; los scripts que la modifiquen
deben recordar que el padre se suma al renderizar.

## 33. Grafo: selección múltiple, conexiones y expresiones (0.36)

- **Selección múltiple**: Ctrl+clic o Shift+clic sobre encabezados; Ctrl+A selecciona todo.
- **Mover varios**: arrastra cualquiera de los nodos seleccionados y se mueven todos.
- **Copiar/pegar grupo**: Ctrl+C / Ctrl+V conserva conexiones internas del grupo.
- **Borrar conexiones**: clic derecho sobre una línea o sobre el botón de salida
  (`Conectar`, `Sí`, `No`). No es necesario reemplazar la conexión.
- **Validación de expresiones**: el panel marca errores de sintaxis como `2 +`,
  paréntesis sin cerrar o caracteres inválidos.
- Las expresiones se validan al abrir/refrescar el grafo; un error no ejecuta la acción.

## 34. Grafo: limitaciones de 0.32 resueltas (0.37)

- **Zoom centrado en el cursor** con Ctrl+rueda.
- **Paneo con botón central** arrastrando el canvas.
- **Validación semántica**: funciones desconocidas y variables no definidas.
- **Historial de ejecución** de los últimos 24 nodos.
- **Búsqueda** de nodos por texto, con resaltado.
- **Auto-layout** con el botón **Ordenar** (recorrido desde los eventos).
- **Pointer lock suspendido** mientras el grafo está activo: se puede usar el ratón en el
  editor durante la partida.

Variables como `enemigos_muertos`, escritas por componentes (por ejemplo `Health`),
pueden aparecer como “Variable no definida” si el grafo no las asigna; es un aviso, no un
error de sintaxis.

## 35. Grafo persistente, grupos y breakpoints (0.38)

- **Historial persistente**: se guarda en `%LOCALAPPDATA%\Ollin\graph-history` y sobrevive
  al cierre de la ventana y de la aplicación.
- **Sincronización externa**: la ventana detecta cambios de la escena y recarga el grafo.
- **Comentarios**: nodos de texto que no se ejecutan.
- **Grupos visuales**: caja arrastrable; al mover el grupo se mueven los nodos contenidos.
- **Breakpoints**: botón en el inspector; al ejecutarse el nodo, la partida se pausa.
- El pointer lock se libera mientras el grafo está activo.

## 36. Grafo: marquee, guías y enlaces (0.39)

- **Marquee**: arrastra sobre el fondo del canvas para seleccionar varios nodos.
  Ctrl/Shift añade a la selección existente.
- **Historial con selección múltiple**: deshacer/rehacer restaura todos los nodos
  seleccionados, no solo el principal.
- **Copiar/pegar con enlaces externos**: las conexiones hacia nodos fuera del grupo se
  conservan.
- **Menú contextual de conexiones**: clic derecho sobre una línea o salida y elige
  **Eliminar conexión**.
- **Snap de nodos**: rejilla de 10 px, activable con **Rejilla**.
- **Guías de alineación**: líneas ámbar al alinear bordes con otros nodos.
- **Errores de expresión junto al campo**, además del panel de validación.

## 37. Grafo: marquee por área y rejilla configurable (0.40)

- **Marquee por área completa**: selecciona si el rectángulo toca la tarjeta o el grupo.
- **Rejilla configurable**: activa/desactiva y elige el tamaño (2–50 px).
- **Guías de borde y centro**: izquierda, derecha, centro, superior, inferior.
- **“Ir al destino”** en el menú contextual de una conexión.
- **Copiar/pegar sanea enlaces**: si el destino externo ya no existe, se descarta.
- **Borde rojo** en campos de expresión inválidos, además del texto.

## 38. Grafo: grupos, distribución y conexiones deshabilitadas (0.41)

- **Marquee sobre un grupo** selecciona también sus nodos contenidos.
- **Distribuir H / Distribuir V** reparte los nodos seleccionados con separación igual.
- **Deshabilitar conexión** en el menú contextual: la línea queda gris y no se ejecuta.
- **Breakpoints persistentes** en el sidecar del historial.
- **Historial versionado** y limpieza automática de entradas de más de 30 días.
- **Borde rojo en vivo** mientras se escribe una expresión inválida.

## 39. Grafo: paso a paso y grupos plegables (0.42)

- **Breakpoint**: pausa antes de ejecutar el nodo marcado.
- **Paso**: botón en la barra del grafo; ejecuta un solo nodo desde el breakpoint.
- **Grupo plegable**: casilla **Plegado** en el inspector; oculta los nodos contenidos
  y sus conexiones.
- **Grupos anidados visualmente**: un grupo dentro de otro se oculta al plegar el padre.
- El paso conserva el runtime en pausa; al reanudar sigue la partida.

## 40. Barra del grafo en una línea (0.43)

- La barra usa una sola fila; **no salta a dos líneas**.
- Acciones secundarias (Ajustar, Ordenar, Paso, Distribuir H/V) están en **Más ▾**.
- En ventanas muy estrechas la barra se desplaza horizontalmente.
- Rejilla, tamaño de rejilla y búsqueda siguen visibles.

## 41. Cierre de limitaciones de 0.41 (0.44)

- **Distribuir H/V** usa separación uniforme entre tarjetas, no solo posiciones.
- **Conexiones deshabilitadas** muestran la etiqueta `DESACTIVADA`.
- **Historial**: limpieza por antigüedad (30 días) y por cantidad (máx. 500 archivos).

## 42. Modo paso y grupos con paternidad explícita (0.45)

- **Modo paso** en **Más ▾**: la partida se pausa en cada nodo, sin breakpoint.
- **Paso** ejecuta un nodo y vuelve a pausar en el siguiente.
- **Agrupar seleccionados** asigna `ParentGroupId` explícito a los nodos.
- **Desagrupar** elimina la pertenencia directa.
- Al plegar un grupo se ocultan sus hijos y los nodos anidados.
- Arrastrar un grupo **plegado** ya no mueve a sus miembros ocultos.

## 43. Barra compacta sin scroll (0.46)

- La barra cabe en una fila sin desplazamiento horizontal.
- Acciones principales como iconos: deshacer, rehacer, copiar, pegar, borrar.
- **Rejilla** y **tamaño de rejilla** están dentro de **Más ▾**.
- El buscador permanece visible.
- Los iconos tienen tooltip con su atajo.

## 44. Acciones rápidas del grafo (0.47)

- **Ajustar, Ordenar, Paso y Distribuir H/V** tienen botones directos en la barra.
- No hace falta abrir **Más** en anchos normales.
- En ventanas de menos de 1080 px, esas acciones se repliegan en **Más** para
  mantener una sola fila.
- Los iconos tienen tooltip.

## 45. Cierre de limitaciones de 0.44 (0.48)

- **Separar H/V**: conserva los huecos existentes y solo corrige solapamientos
  con una separación mínima de 24 px.
- **Conexiones desactivadas cortas**: no dibujan etiqueta para evitar solape;
  muestran tooltip y el puerto indica `(off)`.
- **Historial**: además de 500 archivos y 30 días, límite de **20 MB**.

## 46. Cierre de limitaciones de 0.45 (0.49)

- **Ejecutar hasta seleccionado** en **Más ▾**: reanuda y pausa al llegar al nodo.
- **Migración automática** de grupos antiguos a `ParentGroupId`.
- **Mover hijos ocultos** opcional al arrastrar un grupo plegado.
- **Variables por nodo** en el panel de depuración (últimos 3 nodos).
- El **modo paso** se conserva al cerrar y reabrir el grafo.

## 47. Atajos, accesibilidad y buscador (0.50)

- Atajos:
  - `Ctrl+Z` deshacer, `Ctrl+Y` rehacer.
  - `Ctrl+C` / `Ctrl+V` copiar/pegar.
  - `Ctrl+F` buscar y expandir el buscador.
  - `Ctrl+0` ajustar.
  - `Ctrl+Shift+O` ordenar.
  - `Ctrl+Shift+H` / `Ctrl+Shift+V` distribuir.
  - `F8` paso.
  - `Esc` limpia la búsqueda.
- Los iconos tienen **nombre de accesibilidad** además del tooltip.
- El buscador se amplía al enfocarse y vuelve a su tamaño compacto.
- El zoom 100 % se muestra como **1:1**.

## 48. Esquema de versiones (6.50)

- **6.50**: beta de la etapa 6.
- **6.51, 6.52…**: arreglos de limitaciones, bugs y mejoras dentro de la beta.
- **1.0**: versión completa con todas las etapas.
- Después de la final: **1.1, 1.2…** para mejoras leves; **2.0** para
  integraciones grandes.
- La demo técnica ya no se publica con las versiones.