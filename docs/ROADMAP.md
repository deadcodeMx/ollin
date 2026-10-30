# Roadmap de Ollin

Desarrollo por etapas de un **motor 2D generalista**, pensado para que una sola persona
pueda construir **cualquier tipo de juego**: plataformas, aventura, RPG, puzles, acción
top-down, terror o prototipos. Nada de escala AAA: prioridad a herramientas cómodas y a
que un desarrollador solo pueda terminar juegos pequeños y variados.

Cada etapa entrega algo usable y verificable. Ninguna se cierra sin compilar, con las
pruebas en verde y documentada aquí.

## Principios del proyecto

- **Uso personal.** No hay objetivos de marketplace, plugins de terceros ni editor para
  otros equipos. Las decisiones priorizan mi velocidad, no la extensibilidad.
- **Alcance pequeño.** Escenas de minutos, no mundos abiertos gigantes; recursos
  artesanales antes que procedurales; rendimiento suficiente, no máximo.
- **2D primero, 3D después.** El 2D debe quedar redondo antes de abrir el 3D.
- **Puente 2.5D.** Antes del 3D completo, un paso intermedio barato: mundo con
  profundidad y sprites, cámara en primera persona.
- **Todo editable en el editor.** Si algo requiere tocar código para ajustar una escena,
  falta una herramienta.

## Reglas de alcance

| Área | Lo que entra | Lo que queda fuera |
| --- | --- | --- |
| 2D general | Plataformas, aventura, RPG, puzles, acción, terror y prototipos | Mundos abiertos continuos o simulación masiva |
| Herramientas | Editor, escenas, componentes, scripting, audio, prefabs y guardado | Marketplace, plugins de terceros, editor para equipos |
| Rendimiento | Escenas 2D estables para un jugador | Multijugador, GI en tiempo real, gráficos de última generación |
## Estado actual

| Etapa | Contenido | Estado |
| --- | --- | --- |
| 0 | Cimientos: stack, entorno, solución | ✅ |
| 1 | Editor: escena, jerarquía, inspector, viewport, cámara, JSON | ✅ |
| 2 | Runtime (Play/Pausa/Detener), assets, gizmos, deshacer/rehacer | ✅ |
| 3 | Scripting en C# con Roslyn, `Input`/`Time` y recarga en caliente | ✅ |
| 4 | Cerrar el 2D | ✅ |
| 5 | Herramientas de juego 2D (luces, narrativa, sistemas, HUD) | 🚧 |
| 6 | 2.5D (puente) | ⏳ |
| 7 | 3D | ⏳ |
| 8 | Producto: build, guardado, opciones | ⏳ |

---

## Etapa 3 — Scripting en C# (completada)

- [x] Compilación en memoria con **Roslyn** y ensamblado recargable (`AssemblyLoadContext`).
- [x] Ciclo de vida: `OnStart`, `OnUpdate(dt)`, `OnReload`, `OnDestroy`.
- [x] API: `Entity`/`Transform`, `Scene`, `Input` (teclado y ratón), `Time`
      (`DeltaTime`, `Elapsed`, `Frame`) y `Debug`/`Warn`/`Error`.
- [x] Errores de compilación en la consola con archivo, línea y código.
- [x] `ScriptComponent` enlazado desde el inspector y plantillas generadas por el editor.
- [x] Recarga en caliente al guardar un `.cs`, **sin detener** la partida y conservando
      los campos compatibles.
- [x] 12 pruebas de scripting (47 en total).

Queda para la etapa 4: temporizadores (`WaitForSeconds`) y dibujo de depuración.

---

## Etapa 4 — Cerrar el 2D (esfuerzo: M)

**Objetivo:** que un prototipo 2D de 10 minutos sea cómodo de construir y jugar.

### Scripting (resto)
- [x] Temporizadores: `StartCoroutine` + `WaitForSeconds`.
- [x] Dibujo de depuración en el viewport: líneas y cajas (`DebugDraw`).

### Contenido
- [x] Animación por sprites: hoja de cuadros, FPS, bucle y recorte por cuadro.
- [ ] Eventos de frame y varias animaciones por entidad.
- [x] Tilemap con tileset, pintado en el viewport y colisión por celda.
- [ ] Capas de tilemap y *autotiling*.
- [x] Física 2D: AABB, velocidad/gravedad, triggers, colisión por celda y cuerpos cinemáticos.
- [x] Audio 2D: efectos, música, canales con volumen y *fade*.
- [x] Prefabs: entidad plantilla guardada en `*.prefab.json` e instancias independientes.

### Herramientas
- [x] Proyectos independientes con carpetas, explorador de archivos y apertura de escenas.
- [x] Escenas múltiples con pestañas propias y estado independiente (cámara, selección, historial).
- [x] Plantillas de proyecto (vacío, plataformas, terror).
- [x] Copiar/pegar entidades (Ctrl+C / Ctrl+V).
- [x] Buscador en la jerarquía.
- [ ] Buscador de propiedades en el inspector.

**Criterio de cierre:** un nivel 2D de 10 minutos con animación, colisiones, sonido y
guardado de partida.

Pendientes de pulido para la fase de mejoras: varias animaciones por entidad, capas de
tilemap/*autotiling* y buscador de propiedades en el inspector.

---

## Etapa 5 — Herramientas de juego 2D (esfuerzo: L)

**Objetivo:** las herramientas que sirven a cualquier juego 2D, sin atarse a un género.

### Atmósfera y presentación
- [x] Luces 2D: puntuales y conos, con color, radio, intensidad y parpadeo.
- [~] Sombras por oclusión (shadow casting) para luces 2D: versión experimental opt-in.`n- [ ] Sombras suaves y oclusión desde tilemaps.
- [ ] Niebla, viñeta, grano de película y aberración cromática (post-proceso).
- [x] Capas de paralaje para fondos (factor por sprite).
- [x] Cámara de seguimiento con etiqueta y suavizado.`n- [ ] Cámaras fijas por habitación (estilo clásico).
- [x] Linterna con cono de luz y parpadeo configurable.

### Narrativa e interacción
- [x] Triggers de zona (entrar, salir, interactuar) y aviso de interacción con E.
- [ ] Puertas, cerraduras, llaves y objetos interactuables.
- [x] Diálogos con hablante y texto; notas y documentos legibles.`n- [ ] Retratos en los diálogos y elecciones de respuesta.
- [ ] Secuencias guionizadas (timeline simple: mover cámara, esperar, sonido, texto).
- [ ] Sistema de capítulos y checkpoints.

### Sistemas de juego
- [ ] Inventario con pilas y crafteo por recetas.
- [ ] Estadísticas: salud, hambre, sed, energía, cordura (configurables).
- [ ] Ciclo día/noche con reloj y clima básico (lluvia, niebla).
- [ ] Recolección de recursos y estaciones de trabajo.
- [ ] Construcción simple por celdas (muros, puertas, mobiliario).
- [ ] IA básica: patrulla, persecución, pérdida de rastro, visión y oído.
- [ ] Pathfinding en rejilla (A*) con costes por terreno.
- [ ] Guardado persistente del mundo (posición, inventario, estado de puertas y enemigos).

### Interfaz de juego
- [ ] HUD configurable desde escena (barras, iconos, mensajes).
- [ ] Menús: pausa, opciones, guardar/cargar y muerte.
- [ ] Fuentes bitmap y texto con riqueza mínima (color, tamaño, alineación).

**Criterio de cierre:** un nivel de 10–15 minutos con luces, triggers, diálogos e
inventario, y un bucle de juego con ciclo día/noche y guardado, ambos jugables de
principio a fin.

---

## Etapa 6 — 2.5D: el puente (esfuerzo: M)

**Objetivo:** probar el lenguaje del 3D sin reescribir el motor.

- [ ] Escena con profundidad: sprites y billboards colocados en un espacio 3D.
- [ ] Cámara en primera persona con mirada y movimiento.
- [ ] Colisiones 3D simples (cajas y cilindros) para paredes y puertas.
- [ ] Profundidad y orden correcto de billboards.
- [ ] Iluminación por vértice o por objeto + niebla de distancia.
- [ ] Audio posicional básico.

**Por qué:** permite hacer juegos con estética retro estilo PS1/Doom con el renderer y el flujo actuales, y
descubrir los problemas de cámara, colisión y profundidad antes de invertir en 3D real.

**Criterio de cierre:** un pasillo jugable en primera persona con enemigos sprite,
puertas y ambiente.

---

## Etapa 7 — 3D (esfuerzo: XL, por fases)

**Objetivo:** un 3D contenido: geometría simple, iluminación efectiva y primera persona.

### 7.1 Núcleo
- [ ] `Transform3D` con cuaterniones y jerarquía; convivencia con el 2D.
- [ ] Renderer con búfer de profundidad, culling y shaders con iluminación.
- [ ] Cámara en perspectiva y controlador en primera persona.
- [ ] Carga de mallas **glTF** (SharpGLTF) y OBJ; materiales con textura y color.
- [ ] Editor: viewport 3D, gizmos de traslación/rotación/escala, importador de modelos.

### 7.2 Atmósfera
- [ ] Luz direccional + varias puntuales; sombras con *shadow map*.
- [ ] Skybox, niebla exponencial y *fog* por zonas.
- [ ] Iluminación horneada simple (lightmap por vértice) para escenas estáticas.
- [ ] Post-proceso: viñeta, grano, *bloom* suave y corrección de color.

### 7.3 Sistemas
- [ ] Física 3D (BepuPhysics2) o controlador propio con colisiones de cápsula.
- [ ] *Navmesh* o rejilla 3D para IA; enemigos que patrullan y persiguen.
- [ ] Audio 3D con atenuación por distancia y oclusión básica.
- [ ] Puertas, escaleras, escaleras de mano y ascensores.
- [ ] Guardado de escena 3D y de estado del mundo.

**Criterio de cierre:** un nivel 3D de 5–10 minutos con exploración, un enemigo que
persigue y ambiente de tensión.

### Notas técnicas para el 3D

| Tema | Decisión propuesta |
| --- | --- |
| Transformaciones | Añadir `Transform3D` sin romper `Transform2D`; el formato de escena sube a `formatVersion: 2` con migración |
| Render | Interfaz `IRenderer` con implementaciones 2D (sprite batch) y 3D (mallas, profundidad) |
| Mallas | glTF como formato principal; sin editor de modelado propio |
| Iluminación | Tiempo real simple + lightmaps horneados; **sin** GI en tiempo real |
| Física | BepuPhysics2 para 3D y Box2D para 2D (o solver propio en 2D si alcanza) |
| IA | Rejilla + A* primero; navmesh solo si hace falta |
| Escala | Niveles pequeños, interiores y pasillos; evitar terrenos gigantes |

---

## Etapa 8 — Producto (esfuerzo: M)

- [ ] Build del jugador a carpeta autocontenida (escenas y assets embebidos o copiados).
- [ ] Scripts compilados en el build (sin Roslyn en el jugador).
- [ ] Perfilador: FPS, draw calls, memoria, tiempo de scripts y física.
- [ ] Guardado con ranuras, autoguardado y captura de pantalla.
- [ ] Opciones: resolución, pantalla completa, volumen, sensibilidad, rebind de teclas.
- [ ] Localización de textos (español/inglés) y fuentes.
- [ ] Pantallas de carga y transiciones.
- [ ] Registro de errores a archivo para probar builds.

**Criterio de cierre:** un juego corto empaquetado que otra persona pueda abrir y jugar
sin instalar .NET.

---

## Backlog aparcado (no prioritario)

Ideas buenas para otro momento; no entran en el plan salvo que un juego concreto las pida.

- Multijugador o cooperativo.
- Editor de *scripts* por nodos (visual scripting).
- Marketplace o sistema de plugins de terceros.
- Exportación a móvil, web o consolas.
- VR/AR.
- Iluminación global en tiempo real y *ray tracing*.
- Terrenos enormes, *streaming* de mundo y LOD automático.
- Animación esqueletal compleja y *motion matching*.
- Editor de materiales por nodos.

## Cómo se elige la siguiente etapa

1. Terminar la etapa en curso hasta su criterio de cierre.
2. Si un juego concreto necesita algo del backlog, se adelanta **solo** esa pieza.
3. Cualquier funcionalidad nueva entra con pruebas y documentación.
4. Si una etapa se vuelve más grande de lo previsto, se parte en dos antes de seguir.
