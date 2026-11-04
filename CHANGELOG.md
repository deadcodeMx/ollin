# Historial de cambios

Formato basado en [Keep a Changelog](https://keepachangelog.com/es-ES/1.1.0/) y
versionado [SemVer](https://semver.org/lang/es/).

## [No publicado]

### Cambiado

- Roadmap reorientado a un motor personal para terror lineal y supervivencia, con
  puente 2.5D antes del 3D y etapas reordenadas (4–8).

### Planeado

- Audio 2D y prefabs.
- Luces 2D, triggers y diálogos para terror.
- Puente 2.5D y 3D.
- Exportación de un juego a un ejecutable.

## [6.141.0] — 2026-11-04

### Añadido

- Line2D y Polygon2D.
- Render de contornos 2D.
- Componentes en el menú Visual.

## [6.140.0] — 2026-11-04

### Añadido

- Easing por keyframe: Linear, Smooth, EaseIn, EaseOut y Step.
- Track de eventos de animación.
- Editor de easing en keyframes.
- Prueba de eventos y easing.

## [6.139.0] — 2026-11-04

### Añadido

- Polígonos navegables.
- Agente de navegación con destino y velocidad.
- Pathfinding por grafo de visibilidad.
- Inspector de navegación poligonal.

## [6.138.0] — 2026-11-04

### Añadido

- Backend Linux basado en paplay/aplay/ffplay.
- Fallback silencioso si no hay reproductor.
- Loop básico reiniciando el proceso.

## [6.137.0] — 2026-11-04

### Añadido

- Elemento de lista de UI.
- Scroll vertical de listas.
- Texto multilínea con ajuste.

## [6.136.0] — 2026-11-04

### Añadido

- Foco de UI por teclado.
- Activación de botones con Enter/Espacio.
- Etiquetas de accesibilidad por elemento.
- Borde de foco.

## [6.135.0] — 2026-11-04

### Añadido

- AudioSource2D con atenuación por distancia.
- AudioListener2D.
- Inspector de audio 2D.
- Selección automática del jugador como listener.

## [6.134.0] — 2026-11-04

### Añadido

- Contenedores de UI con layout vertical/horizontal.
- Aplanado de jerarquía con padding y gap.
- Tema global de panel, texto, botón y acento.

## [6.133.0] — 2026-11-04

### Añadido

- Cámara pixel-perfect.
- Editor de animación y anclajes de UI consolidados.
- Localización, deadzone y touch.

## [6.132.0] — 2026-11-04

### Añadido

- Localización con plurales y detección RTL.
- Selector de idioma en opciones del reproductor.
- Zona muerta configurable del Input Map.
- Estado de touch en el reproductor.

## [6.131.0] — 2026-11-04

### Añadido

- Editor visual de animación.
- Clips, tracks y keyframes editables.
- Edición de tiempo, posición, número y audio/evento.

## [6.130.0] — 2026-11-04

### Añadido

- Anclajes de UI: esquinas, bordes y centro.
- Layout por alineación y margen.
- Overlay de UI con Grid en editor y reproductor.

## [6.129.0] — 2026-11-04

### Añadido

- Y-sort real entre sprites y celdas de tilemap.
- Checkbox de Y-sort por capa.
- Render combinado por Y y orden de capa.

## [6.128.0] — 2026-11-04

### Añadido

- IK de dos huesos con objetivo y dirección de codo.
- Inspector de IK del esqueleto.
- Prueba de IK.

## [6.127.0] — 2026-11-04

### Añadido

- Captura de teclas en el Input Map.
- Captura de botones de gamepad.
- Bindings editables sin escribir el formato.

## [6.126.0] — 2026-11-04

### Añadido

- Blend modes de sprite: Normal, Add y Multiply.
- Separación automática del batch al cambiar de blend.

## [6.125.0] — 2026-11-04

### Añadido

- Navegación por coste de tile.
- Oclusión de luces por tile.
- Editor de coste y oclusión en TileSet.
- IA respeta navegación por tile.
- Prueba de navegación y oclusión.

## [6.124.0] — 2026-11-04

### Añadido

- Selector de capa de tilemap.
- Añadir y quitar capas.
- Pintado en la capa activa.
- Terreno y variantes de autotile por tile.
- Editor de variantes en TileSet.

## [6.123.0] — 2026-11-04

### Añadido

- TileSet como asset JSON reutilizable.
- Editor visual de TileSet.
- Edición de colisión, offset, one-way y pendiente por tile.
- Carga y guardado de TileSet.
- Prueba de serialización de TileSet.

## [6.122.0] — 2026-11-04

### Añadido

- Materiales de fricción y rebote por collider.
- Joints de muelle y pin.
- CCD configurable por cuerpo.
- Normal de pendiente por tile.
- Inspectores de materiales y CCD.

## [6.121.0] — 2026-11-04

### Añadido

- RayCast2D y ShapeCast2D.
- Consultas `Physics2D.Raycast` y `Physics2D.BoxCast` para scripts.
- Excepciones de colisión por tags.
- Inspector de tags ignoradas.
- Pruebas de consultas físicas.

## [6.120.0] — 2026-11-04

### Añadido

- CanvasLayer con orden, visibilidad y Y-sort.
- AnimationPlayer2D con timelines y tracks.
- UiCanvas runtime con paneles, etiquetas, botones y barras.
- Overlay de UI en editor y reproductor.
- Pruebas de UI, animación y capas.

## [6.119.0] — 2026-11-04

### Añadido

- TileSet con colisión y región por tile.
- TileMapLayer con múltiples capas por entidad.
- Colisión one-way por tile.
- Render de capas con TileSet.
- Inspector básico de TileSet.
- IA y floor snap respetan colisión por tile.

## [6.118.0] — 2026-11-04

### Añadido

- Snap al suelo y contactos persistentes del personaje.
- Arrastre por plataformas móviles sin doble desplazamiento.
- Subpasos adaptativos para evitar tunneling a alta velocidad.
- Pruebas de contactos y plataforma móvil.

## [6.117.0] — 2026-11-04

### Añadido

- Contactos de suelo, pared y techo en CharacterBody2D.
- Normal de pendiente y pendiente máxima caminable.
- Arrastre por plataformas móviles y velocidad de plataforma.
- Persistencia de suelo y API de estado del personaje.
- Inspectores de pendiente máxima y snap al suelo.

## [6.116.0] — 2026-11-03

### Añadido

- Soporte Linux x64: editor y reproductor.
- Publicación self-contained y paquete `.tar.gz`.
- Instalador Linux (`install.sh` y lanzador `.desktop`).
- Gamepad Linux por `/dev/input/js*`.
- Exportación de juegos multiplataforma.

### Limitaciones

- Audio silencioso en Linux en esta primera versión.

## [6.115.0] — 2026-11-02

### Mejorado

- Las etiquetas del HUD admiten varias líneas.

## [6.114.0] — 2026-11-02

### Corregido

- El modo Vacío se guardaba como Completo al crear desde Archivo.
- Selector de modo de componentes para proyectos existentes.

## [6.113.0] — 2026-11-02

### Corregido

- Los menús de componentes vacíos se ocultan en el modo vacío.

## [6.112.0] — 2026-11-02

### Añadido

- Proyecto vacío con solo componentes esenciales.
- Proyecto por género con componentes recomendados.
- Botón para eliminar proyectos en la ventana de proyectos.

## [6.111.0] — 2026-11-02

### Añadido

- Turnos por iniciativa.
- Mazo, mano y descarte.
- Generador de mazmorra procedural.
- Esqueleto 2D con preview.
- IA y luces con sombras existentes.

## [6.110.0] — 2026-11-02

### Añadido

- Equipment y QuestLog.
- Mixer de audio con música/efectos/UI.
- Localización por JSON y diálogos localizables.

## [6.109.0] — 2026-11-02

### Añadido

- Plataformas móviles y de un solo sentido.
- Partículas y estela.
- Máquina de estados de animación.
- Autotile básico por vecinos.
- Transición de pantalla con fundido.

## [6.108.0] — 2026-11-02

### Cambiado

- Knockback sale de Hitbox y pasa a Knockback2D.
- Reglas de diseño documentadas y auditadas.

## [6.107.0] — 2026-11-02

### Corregido

- Todas las secciones del inspector incluyen "✕ Quitar componente".
- Hitbox, Hurtbox, Checkpoint 2D y Arma 2D se pueden quitar.

## [6.106.0] — 2026-11-02

### Corregido

- Projectile2D choca contra colliders sólidos y no atraviesa paredes.

## [6.105.0] — 2026-11-02

### Añadido

- Collision layers y masks.
- EdgeCollider2D.
- Hitbox y Hurtbox con daño por equipo.
- Checkpoint2D.
- Weapon2D y Projectile2D.

## [6.104.0] — 2026-11-02

### Añadido

- Tres ranuras de guardado en el editor.
- Guardado atómico con respaldo .bak.
- Carga con recuperación desde respaldo.

## [6.103.0] — 2026-11-02

### Añadido

- Inventario runtime con cuadrícula de slots.
- I abre/cierra el inventario en editor y juego exportado.
- El jugador se detecta por tag player.

## [6.102.0] — 2026-11-02

### Añadido

- Input Map configurable por proyecto.
- Acciones para scripts: GetAction, GetActionDown, GetActionUp.
- Soporte de gamepad mediante XInput.
- Editor de mapa de entrada en Editar.
- Personaje 2D usa move_left/move_right/jump.

## [6.101.0] — 2026-11-02

### Corregido

- El render del collider recorre todos los puntos de la forma, no solo 4.

## [6.100.0] — 2026-11-02

### Corregido

- Los círculos se dibujan como círculos, no como cuadrados.
- Las cápsulas se dibujan con su contorno real.
- La física usa el contorno real de la cápsula.

## [6.99.0] — 2026-11-02

### Añadido

- Colisión exacta círculo-polígono y polígono-polígono.
- Cápsulas como polígono convexo.
- Añadir componente organizado por categorías.

## [6.98.0] — 2026-11-02

### Añadido

- Area2D como zona de detección.
- Colliders circular, cápsula y polígono.
- Física y render usan Collider2D común.

## [6.97.0] — 2026-11-02

### Añadido

- Cámara 2D con zona muerta y límites de mapa.

## [6.96.0] — 2026-11-02

### Añadido

- Personaje 2D con movimiento, salto, gravedad, coyote time y jump buffer.
- Se añade con Rigidbody y collider automáticamente.

## [6.95.0] — 2026-11-02

### Corregido

- Los componentes se pliegan por defecto para mostrar todos los encabezados.

## [6.94.0] — 2026-11-02

### Corregido

- Interactuable notifica HasInteractable y aparece al añadirlo.
- Se notifican todos los Has de componentes.

## [6.93.0] — 2026-11-02

### Corregido

- Visual (Sprite) e Interactuable añadidos a Añadir componente.

## [6.92.0] — 2026-11-02

### Añadido

- Añadir componente → Script C#… abre lista de scripts existentes.
- El diálogo permite crear uno nuevo con nombre y carpeta.

## [6.91.0] — 2026-11-02

### Corregido

- Crash al abrir el motor por reparentar componentes de física.
- El inspector vuelve a abrir correctamente.

## [6.90.0] — 2026-11-02

### Corregido

- Los componentes físicos conservan su orden y muestran el botón de quitar.
- Los scripts se pliegan por defecto para dejar visible colisión.

## [6.89.0] — 2026-11-02

### Corregido

- Se llama a WrapComponentPanels para colisión, cuerpo, rotador y oscilador.
- Los botones de quitar componente ya se generan.

## [6.88.0] — 2026-11-02

### Corregido

- Botón visible para quitar componentes desde el contenido.
- Botón Abrir script en cada componente Script.

## [6.87.0] — 2026-11-02

### Corregido

- Al quitar un script se invalida la caché del inspector.

## [6.86.0] — 2026-11-02

### Corregido

- Cada componente tiene botón ✕ para eliminarlo.
- Los campos públicos usan el valor inicial real del script, no 0.

## [6.85.0] — 2026-11-02

### Corregido

- Rotador y Oscilador ya tienen sección en el inspector.
- Se notifican todos los cambios de visibilidad de componentes.

## [6.84.0] — 2026-11-02

### Corregido

- Quitar un script lo elimina del inspector.
- Campos y propiedades públicas aparecen tras recargar scripts.
- Los valores públicos se aplican al reproducir.

## [6.83.0] — 2026-11-02

### Corregido

- Los scripts aparecen dentro de la entidad, antes de colisión/física.

## [6.82.0] — 2026-11-02

### Corregido

- Los scripts se enlazan directamente a la entidad y siempre se dibujan en el inspector.

## [6.81.0] — 2026-11-02

### Corregido

- Los scripts aparecen justo después de la entidad, no al final del inspector.

## [6.80.0] — 2026-11-02

### Corregido

- El host de scripts se añade al inspector.
- Los scripts añadidos desde Archivos aparecen y se pueden editar.

## [6.79.0] — 2026-11-02

### Corregido

- Añadir scripts desde Archivos refresca el inspector.
- Cada script aparece como componente independiente.
- Los títulos de sección usan el color de acento.

### Cambiado

- Script retirado de Añadir componente; solo se añade desde Archivos.

## [6.78.0] — 2026-11-02

### Cambiado

- Cada script es un componente independiente.
- Se pueden añadir varios scripts por entidad desde Archivos.
- Se quitó la creación de scripts desde el inspector.

## [6.77.0] — 2026-11-02

### Mejorado

- Inspector tipo Unity: componentes plegables, encabezados resaltados.
- Menú contextual para quitar componentes.
- Campos públicos de scripts visibles y editables.
- Crear scripts desde Archivos y añadirlos a la entidad.

## [6.76.0] — 2026-11-02

### Corregido

- Compatibilidad de scripts antiguos sin valores de campos.

## [6.75.0] — 2026-11-02

### Añadido

- Secciones del inspector contraíbles y con encabezados resaltados.
- Crear scripts desde Archivos y añadirlos a la entidad seleccionada.
- Campos públicos de scripts compilados como campos editables.

## [6.74.0] — 2026-11-02

### Corregido

- Colisión, cuerpo físico y script se muestran al añadirlos.
- Las secciones de física viven dentro de la entidad y conservan su contexto.

## [6.73.0] — 2026-11-02

### Corregido

- Las secciones de física se actualizan al añadir o quitar componentes.

## [6.72.0] — 2026-11-02

### Corregido

- Las notificaciones de componentes se envían desde RefreshAll.
- Colisión, cuerpo físico y script aparecen al añadirlos.

## [6.71.0] — 2026-11-02

### Corregido

- Las secciones de colisión, cuerpo físico y script se reconstruyen y
  aparecen al añadir el componente.

## [6.70.0] — 2026-11-02

### Corregido

- Los componentes añadidos desde el botón ya aparecen en el inspector.
- El inspector recibe notificación de todos los cambios de componente.

## [6.69.0] — 2026-11-02

### Añadido

- Diálogo de nombre y carpeta al crear un script.
- Crear carpetas desde Archivos.
- El explorador muestra todas las carpetas raíz.

### Cambiado

- Inspector sin sección Física; cada componente aparece solo si existe.
- Botón de añadir componente fijo al final.
- 205 pruebas automatizadas.

## [6.68.0] — 2026-11-02

### Corregido

- La rejilla mantiene densidad y grosor al hacer zoom.
- Los ejes X/Y no desaparecen.
- El contorno de selección conserva su grosor.
- El inspector ya no muestra separadores raros.

### Añadido

- Varias ranuras de script por entidad.
- Atajos del teclado configurables.
- Símbolo y color desde el menú contextual de Jerarquía.
- Botón de añadir componente fijo al final del inspector.

### Cambiado

- Lista de componentes reducida a los esenciales 2D.
- 205 pruebas automatizadas.

## [6.67.0] — 2026-11-02

### Cambiado

- Interfaz centrada en 2D; herramientas y componentes 2.5D ocultos.
- Plantillas 2.5D ocultas.
- Menú + en Jerarquía para crear entidades.
- + Sprite / + Vacía fuera de la barra superior.
- 205 pruebas automatizadas.

## [6.66.0] — 2026-11-02

### Cambiado

- Inspector unificado en una sola pestaña.
- Tags en pestaña independiente.
- Herramientas directas en la barra, sin menú desplegable.
- 205 pruebas automatizadas.

## [6.65.0] — 2026-11-02

### Corregido

- El desinstalador funciona con doble clic sin `--uninstall`.
- Espera al proceso padre y reintenta el borrado.
- Cierra Ollin antes de desinstalar.
- Confirmación antes de desinstalar.
- Acceso directo Desinstalar Ollin en el menú Inicio.
- 205 pruebas automatizadas.

## [6.64.0] — 2026-11-02

### Añadido

- Paleta visual de mosaicos con selección por clic.
- Columnas de tileset configurables.
- Pegado entre escenas con el portapapeles interno.
- 205 pruebas automatizadas.

## [6.63.0] — 2026-11-02

### Añadido

- Copiar/pegar múltiples entidades.
- Borrador, relleno y limpiar tilemap.
- Deshacer por trazo de pintura.
- 205 pruebas automatizadas.

## [6.62.0] — 2026-11-02

### Añadido

- Edición en lote de posición, rotación, escala y orden de capa.
- Duplicar toda la selección.
- 205 pruebas automatizadas.

## [6.61.0] — 2026-11-02

### Añadido

- Selección múltiple desde la jerarquía.
- Sincronización jerarquía/viewport.
- Inspector de resumen en selección múltiple.
- Supr elimina todas las seleccionadas.
- 205 pruebas automatizadas.

## [6.60.0] — 2026-11-02

### Añadido

- Lazo de selección 2D con rectángulo visual.
- Selección por intersección al soltar.
- Ctrl para lazo aditivo.
- 205 pruebas automatizadas.

## [6.59.0] — 2026-11-02

### Añadido

- Selección múltiple con Ctrl+clic en el viewport.
- Menú Alinear: izquierda, centro, derecha, arriba, centro vertical, abajo.
- Distribuir horizontal y vertical.
- 205 pruebas automatizadas.

## [6.58.0] — 2026-11-02

### Añadido

- Volteo vertical de sprite.
- Botones de orden de capas: al fondo, atrás, adelante, al frente.
- Barra de zoom flotante en el viewport.
- 205 pruebas automatizadas.

## [6.57.0] — 2026-11-02

### Cambiado

- Barra del editor reorganizada y simplificada.
- Herramientas 3D/2.5D en un menú desplegable.
- Imán 2D configurable y toggle de rejilla.
- 205 pruebas automatizadas.

## [6.56.0] — 2026-11-02

### Añadido

- Foco inicial y Escape en la ventana de atajos.
- Región viva para el estado del grafo.
- Texto de ayuda del buscador.
- Guía de accesibilidad para Narrator/NVDA.
- 205 pruebas automatizadas.

## [6.55.0] — 2026-11-02

### Cambiado

- Ventana de atajos accionable (ejecuta y cierra).
- Smoke UIA de nombres de accesibilidad.
- 205 pruebas automatizadas.

## [6.54.0] — 2026-11-02

### Añadido

- Paso atrás/adelante de traza.
- Diff de variables antes/después por nodo.
- 205 pruebas automatizadas.

## [6.53.0] — 2026-11-02

### Cambiado

- Ventana de atajos con F1.
- Buscador sin expansión que apriete la barra en ventanas estrechas.
- Nombre de accesibilidad del buscador.
- 205 pruebas automatizadas.

## [6.52.0] — 2026-11-02

### Cambiado

- Migración de grupos con deshacer.
- Variables por nodo después de la acción.
- Cancelar “ejecutar hasta”.
- Persistencia de “mover hijos ocultos”.
- 205 pruebas automatizadas.

## [6.51.0] — 2026-10-31

### Cambiado

- Acciones rápidas visibles también por debajo de 1080 px.
- En compacto, copiar/pegar/borrar y zoom pasan a **Más** o atajos.
- Conexiones desactivadas cortas muestran `OFF`.
- 204 pruebas automatizadas.

## [6.50.0] — 2026-10-31

### Cambiado

- Nuevo esquema de versiones: Beta 6.50.0 para la etapa 6.
- Próximos arreglos/mejoras: 6.51.0, 6.52.0…
- La demo técnica deja de publicarse.
- 204 pruebas automatizadas.

## [0.50.0] — 2026-10-31

### Añadido

- Atajos para buscar, ajustar, ordenar, distribuir y paso.
- Nombres de accesibilidad en los iconos.
- Buscador expandible con Ctrl+F; Esc limpia.
- Zoom 100 % como 1:1.
- 204 pruebas automatizadas.

## [0.49.0] — 2026-10-31

### Añadido

- Ejecutar hasta el nodo seleccionado.
- Migración de grupos geométricos a paternidad explícita.
- Opción Mover hijos ocultos al arrastrar grupos plegados.
- Variables por nodo en la depuración.
- Persistencia del modo paso al cerrar/reabrir.
- 1 prueba nueva: 204 en total.

## [0.48.0] — 2026-10-31

### Cambiado

- Separar H/V conserva huecos y corrige solapamientos.
- Etiquetas desactivadas sin solape; puerto `(off)` y tooltip.
- Historial limitado también por 20 MB.
- 1 prueba nueva: 203 en total.

## [0.47.0] — 2026-10-31

### Cambiado

- Acciones rápidas directas: Ajustar, Ordenar, Paso y Distribuir H/V.
- Repliegue automático a **Más** por debajo de 1080 px.
- 202 pruebas automatizadas.

## [0.46.0] — 2026-10-31

### Cambiado

- Barra compacta en una sola fila, sin scroll horizontal.
- Acciones principales como iconos con tooltip.
- Rejilla y tamaño movidos al menú **Más ▾**.
- 202 pruebas automatizadas.

## [0.45.0] — 2026-10-31

### Añadido

- Modo paso sin breakpoint (pausa en cada nodo).
- Paternidad explícita de grupos con Agrupar/Desagrupar.
- Arrastrar un grupo plegado ya no mueve miembros ocultos.
- 2 pruebas nuevas: 202 en total.

## [0.44.0] — 2026-10-31

### Cambiado

- Distribución H/V con separación uniforme entre tarjetas.
- Etiqueta `DESACTIVADA` en conexiones deshabilitadas.
- Límite de archivos del historial (500) además de la limpieza por edad.
- 1 prueba nueva: 200 en total.

## [0.43.0] — 2026-10-31

### Cambiado

- La barra del grafo ya no salta a dos líneas.
- Acciones secundarias movidas al menú **Más ▾**.
- Barra con desplazamiento horizontal en ventanas estrechas.

## [0.42.0] — 2026-10-31

### Añadido

- **Paso a paso real**: ejecuta un nodo desde el breakpoint.
- **Grupos plegables**: ocultan sus nodos y conexiones.
- **Anidación visual**: grupos dentro de grupos se pliegan con el padre.
- 1 prueba nueva: 199 en total.

## [0.41.0] — 2026-10-31

### Añadido

- El marquee de un grupo selecciona sus miembros.
- Distribución horizontal/vertical con separación igual.
- Conexiones deshabilitables (grises, sin ejecución).
- Breakpoints persistentes en el sidecar.
- Historial versionado con limpieza a 30 días.
- Borde rojo en vivo al editar expresiones.
- 2 pruebas nuevas: 198 en total.

## [0.40.0] — 2026-10-31

### Añadido

- Marquee por área completa de tarjeta/grupo.
- Rejilla configurable (2–50 px) con casilla.
- Guías de borde y centro.
- “Ir al destino” en el menú contextual.
- Saneo de enlaces externos al pegar.
- Borde rojo en expresiones inválidas.
- 196 pruebas automatizadas.

## [0.39.0] — 2026-10-31

### Añadido

- Selección por **marquee** con Ctrl/Shift.
- Historial que restaura la **selección múltiple**.
- Copiar/pegar conserva **enlaces externos**.
- **Menú contextual** para eliminar conexiones.
- **Snap a rejilla de 10 px** y **guías de alineación**.
- Errores de expresión **junto al campo**.
- 2 pruebas nuevas: 196 en total.

## [0.38.0] — 2026-10-31

### Añadido

- Historial del grafo persistente en `%LOCALAPPDATA%\Ollin\graph-history`.
- Sincronización de la ventana con cambios externos de escena.
- Nodos de **comentario** y **grupo visual** (mueve nodos contenidos).
- **Breakpoints** que pausan la partida.
- 3 pruebas nuevas: 194 en total.

## [0.37.0] — 2026-10-31

### Añadido

- Zoom del grafo centrado en el cursor.
- Paneo con botón central.
- Validación semántica: funciones desconocidas y variables no definidas.
- Historial de ejecución de los últimos 24 nodos.
- Búsqueda de nodos con resaltado y auto-layout.
- Pointer lock suspendido mientras el grafo está activo.
- 4 pruebas nuevas: 191 en total.

## [0.36.0] — 2026-10-31

### Añadido

- **Selección múltiple** de nodos con Ctrl/Shift y **mover varios** a la vez.
- **Copiar/pegar grupos** conservando conexiones internas.
- **Borrar conexiones con clic derecho** sobre línea o salida.
- **Validación de sintaxis de expresiones** en el grafo.
- 5 pruebas nuevas: 187 en total.

## [0.35.0] — 2026-10-31

### Añadido

- Transformaciones de mundo para entidades hijas 2.5D en render, cámara, colisión,
  IA, navegación, interacción y gizmos.
- `Scene.SetWorldPosition` y `Scene.WorldDistance`.
- `Wall3D.Center`, `Bounds` y `Camera3D.Eye` usan la jerarquía.
- 5 pruebas nuevas: 182 en total.

## [0.34.0] — 2026-10-31

### Añadido

- `SceneSerializer.TryLoad` con errores de JSON con línea/columna.
- `SceneValidator`: assets faltantes, jerarquía rota, ciclos e IDs duplicados.
- El editor informa de escenas corruptas sin cerrar la escena actual.
- Entidades hijas 2.5D verificadas al guardar/cargar (padre, posición local y mundo).
- `SceneRuntime.ResetGameState`: GameState se limpia al cambiar de escena.
- 6 pruebas nuevas: 177 en total.

## [0.33.0] — 2026-10-31

### Añadido

- `SaveSlotService`: tres ranuras y autoguardado separado.
- `AutosaveScheduler` y `PlayerSettings` reutilizables y probados.
- `GraphEditing`, `NodeGraphHistory` y `GraphValidator` compartidos por el editor.
- **Smoke test de interfaz** `tools/ui-smoke.ps1`: pausa/reanudación real y ventana del grafo.
- 8 pruebas nuevas: 171 en total.

## [0.32.0] — 2026-10-31

### Añadido

- **Deshacer/rehacer** en el editor de grafo (instantáneas, hasta 80 pasos).
- **Copiar/pegar** de nodos con Ctrl+C / Ctrl+V.
- **Zoom** del canvas (35%–200%), botones y Ctrl+rueda.
- **Validación automática**: enlaces rotos, eventos sin salida, objetivos inexistentes,
  variables incompletas y ciclos.
- **Depuración visual**: nodo activo en cian y variables en vivo.
- Ventana del grafo no modal para observar la ejecución.
- 163 pruebas automatizadas.

## [0.31.1] — 2026-10-31

### Añadido

- **Bloqueo del puntero** al jugar y en el modo reproducir; el cursor se oculta.
- **Recentrado** automático al entrar al viewport o reanudar.
- **Invertir eje Y**, **sensibilidad horizontal/vertical** y **suavizado de cámara**.
- Opciones correspondientes en el menú de pausa del juego exportado.
- Pruebas de ejes, inversión, sensibilidad y suavizado: 163 en total.

## [0.31.0] — 2026-10-31

### Añadido

- **Demo técnico 2.5D** jugable: tres armas, tres enemigos, jefe, llave, puerta,
  checkpoint, lava, portal, botiquín, contador de bajas y victoria/derrota.
- Plantilla **Demo técnico 2.5D** en el diálogo de proyecto nuevo.
- **Contador de bajas** automático con `Health.DeathVariable` y **VariableHud**.
- **Colisión cilíndrica** para pilares, integrada en jugador, IA y navegación.
- 3 pruebas nuevas: 159 en total.

### Cerrado

- **Etapa 6 (2.5D)** completada: núcleo jugable, herramientas y demo exportable.

## [0.30.0] — 2026-10-31

### Añadido

- **Variables globales** en el grafo: fijar, sumar y alternar flags.
- **Condición de expresión** con comparaciones, aritmética, booleanos y `tiene("objeto")`.
- Funciones `vida`, `vidamax`, `vidas`, `min` y `max`.
- Las variables se guardan y cargan con la partida.
- 4 pruebas nuevas: 156 en total.

## [0.29.1] — 2026-10-31

### Corregido

- El evento «al iniciar» del grafo de nodos y del script visual se ejecuta una sola vez,
  aunque la regla esté marcada como repetible.

## [0.29.0] — 2026-10-31

### Añadido

- **Grafo de nodos ilimitado** con editor de canvas, conexiones, condiciones y ramas.
- **Vista previa del pincel** 3D y selección de textura/tamaño con el ratón.
- **Editor de armas por ranuras** con selector 1/2/3 y botón para añadir armas.
- **Tres ranuras de guardado** y **autoguardado cada 45 s** en el juego exportado.
- 2 pruebas nuevas: 152 en total.

## [0.28.0] — 2026-10-31

### Añadido

- **Scripting visual por reglas**: cuatro reglas por entidad con evento, condición y acción.
- Nuevas acciones: dañar, curar y teletransportar.
- Componentes **Peligro 3D**, **Teletransporte** y **Seguidor 3D**.
- **Pincel configurable**: ancho, alto, fondo, tamaño de suelo y serie con separación.
- 5 pruebas nuevas: 150 en total.

## [0.27.0] — 2026-10-31

### Añadido

- **Navegación A\*** para la IA 3D: rejilla desde paredes, desvío de obstáculos y
  actualización periódica.
- **Herramientas de nivel**: pincel de pared/suelo, imán de rejilla y duplicar serie x4.
- **Combate ampliado**: proyectiles, dos armas con ranuras, cargador, reserva y recarga.
- **Reaparición** con vidas y checkpoints; las reglas respetan las vidas restantes.
- **Menú de pausa** en el juego exportado con opciones (volumen, sensibilidad, pantalla
  completa) y guardado/carga persistente.
- **Botones, triggers, generadores y checkpoints** como componentes sin código.
- 7 pruebas nuevas: 145 en total.

## [0.26.2] — 2026-10-31

### Corregido

- La cámara ignora el ratón durante 0.8 s al arrancar: evita que el juego exportado
  aparezca en negro por el salto inicial del puntero al entrar en la ventana.

## [0.26.1] — 2026-10-31

### Corregido

- La cámara ya no gira al recibir un salto enorme del ratón al entrar al viewport
  (el juego exportado aparecía en negro).
- README y visión del motor actualizados: **gratis y libre (MIT) para cualquier persona**,
  con creación de juegos sin programar.

## [0.26.0] — 2026-10-31

### Añadido

- **Comportamientos sin código**: Salud, Arma, Recogible, Puerta, Reglas de juego y
  Mensaje inicial, con pestañas en el inspector.
- **Ataque de la IA 3D** y animación de billboards con `SpriteAnimator`.
- **Mini-juego 2.5D sin scripts**: salud, disparo, llave, puerta con llave, enemigo que
  ataca y victoria/derrota.
- El motor se declara **gratis y libre (MIT) para cualquier persona**.

### Cambiado

- La plantilla 2.5D ya no incluye scripts: todo se configura con componentes.

## [0.25.0] — 2026-10-31

### Añadido

- **IA 3D**: `AiAgent3D` con patrulla, visión por radio y línea de vista, persecución
  con memoria y colisión contra cajas 3D.
- Enemigo **Sombra** con IA en la plantilla 2.5D; la cámara FPS lleva la etiqueta
  `player`.
- Pestaña **IA 3D** en el inspector con estado patrulla/persecución.
- 3 pruebas nuevas: 132 en total.

## [0.24.0] — 2026-10-31

### Añadido

- **Gizmo 3D de rotación** (alrededor de Y) y **escala** por eje o uniforme.
- Botones **Mover / Rotar / Escalar** en la barra y atajos **1 / 2 / 3**.
- `Wall3D.Yaw` con cajas rotadas en el renderer y *bounds* rotados para selección.
- Rotación de billboards con `Transform.Rotation` (roll).
- Campo **Giro Y** en el inspector de Pared 3D y refresco de campos 3D.
- 1 prueba nueva: 129 en total.

## [0.23.0] — 2026-10-31

### Añadido

- **Texturas repetidas** en paredes, suelos y techos: `TextureScale` y UV con wrapping
  `Repeat`; texturas de ejemplo `wall.png`, `floor.png` y `ceiling.png`.
- **Gizmo 3D**: selección por rayo y arrastre en el plano del suelo con ejes X/Y/Z,
  ajuste con Ctrl y deshacer.
- Corredor de ejemplo con paredes de ladrillo, suelo de losas y techo de paneles.
- 1 prueba nueva: 128 en total.

## [0.22.1] — 2026-10-31

### Añadido

- **Búfer de profundidad real** para la geometría 2.5D (paredes, suelos, techos y
  billboards) mediante un lote 3D dedicado.
- **Altura base** en `Wall3D` para colocar techos y plataformas.
- Corredor de ejemplo con suelo y techo, iluminación cálida/fría y niebla más suave.

### Corregido

- Giros bruscos de cámara al entrar al viewport (delta del ratón limitado).
- La niebla del corredor tapaba la geometría cercana.

## [0.22.0] — 2026-10-31

### Añadido

- **Núcleo 2.5D**: `Camera3D` con perspectiva, niebla y ambiente; `Wall3D` (cajas para
  paredes, suelos y puertas); `FirstPersonController` (WASD + ratón + colisión).
- Renderer de billboards/walls con proyección en perspectiva, orden por profundidad,
  iluminación por objeto y niebla.
- **Audio posicional** básico (`AudioSource.Spatial` con atenuación por distancia).
- Plantilla **2.5D (pasillo)** con puerta interactiva (E), enemigo sprite y ambiente.
- Pestañas del inspector para Cámara 3D, Pared 3D y Controlador FPS.
- Serialización de `Vector3` en escenas (necesaria para las cajas 3D).
- 6 pruebas nuevas: 126 en total.

## [0.21.0] — 2026-10-30

### Añadido

- **HUD y diálogos en el juego exportado**: barras, etiquetas, mensajes, diálogo con
  retrato y pantalla de fin de partida. Antes solo se veía el viewport.
- Esto corrige que el diálogo inicial quedara activo pero invisible y bloqueara el
  movimiento: ahora se ve y se avanza con Enter/E/Espacio.

## [0.20.3] — 2026-10-30

### Corregido

- El instalador **cierra el editor y el reproductor** antes de copiar. Si estaban
  abiertos, Windows bloqueaba los DLL y la instalación se quedaba en la versión
  anterior, por lo que seguirían fallando los juegos exportados.
- Tras actualizar, asegúrate de **volver a exportar** el juego.

## [0.20.2] — 2026-10-30

### Corregido

- El reproductor ya no intenta abrir ni compilar el último proyecto del editor antes
  de cargar el juego. Eso generaba errores de compilación falsos en `player.log`.
- El registro del reproductor queda limpio: backend, scripts precargados, escena y
  audios.

## [0.20.1] — 2026-10-30

### Corregido

- El juego exportado no siempre recibía el teclado: ahora la entrada se captura a
  nivel de ventana y la ventana se activa al terminar la pantalla de inicio.
- Los clics y el movimiento del ratón se reenvían explícitamente al juego.
- La exportación **verifica el paquete** (escena inicial, `game.json`, `project.ollin`
  y `game.dll`) y falla con un mensaje claro si falta algo.
- El reproductor escribe `player.log` junto al ejecutable con versión, scripts y audio
  para poder diagnosticar.

## [0.20.0] — 2026-10-30

### Corregido

- **Los scripts no se ejecutaban en el juego exportado**: al iniciar la partida se
  recompilaban desde una carpeta de scripts vacía y se perdían los scripts del paquete.
  Ahora se conservan los scripts compilados.
- **Sin sonido en el juego exportado**: el reproductor no inicializaba el backend de
  audio. Ahora inicializa NAudio y resuelve los audios contra la carpeta extraída.
- Con ello vuelven a funcionar los menús, los botones y los sonidos de los juegos
  exportados.

## [0.19.0] — 2026-10-30

### Añadido

- **Empaquetado seguro**: el juego exportado son solo `<Juego>.exe` + `game.pak` +
  `LEEME.txt`. Los scripts se compilan a DLL y **no se incluye el código fuente**.
- **Paquete cifrado** AES-256-GCM con escenas, assets, diálogos, objetos y scripts.
- **Un único ejecutable** (reproductor single-file), con el nombre del juego.
- Diálogo de exportación para activar o desactivar la pantalla de inicio y la marca de agua.
- Exportación por línea de comandos: `--export <carpeta> [--no-splash] [--no-watermark]`.

### Corregido

- El botón **Exportar** ahora sí aparece y la exportación termina con código correcto.

## [0.18.1] — 2026-10-30

### Corregido

- El botón/menú **Exportar juego** no aparecía: ahora está en **Archivo → Exportar juego…**
  y como botón visible en la barra de herramientas.

## [0.18.0] — 2026-10-30

### Añadido

- **Exportar juego**: `Archivo → Exportar juego…` empaqueta el proyecto con un
  reproductor independiente (`Ollin.Player.exe`), sus datos y `game.json`.
- **Pantalla de inicio** con el logo de Ollin y **marca de agua** «Hecho con Ollin»
  durante el juego (activadas por defecto, desactivables en `game.json`).
- Reproductor con pantalla completa (F11) y salida con Esc.
- El instalador incluye la plantilla del reproductor en `player/`.
- 1 prueba nueva: 120 en total.

## [0.17.0] — 2026-10-30

### Añadido

- **Lanzador de proyectos** antes del editor, con recientes, crear y abrir carpeta.
- **Tres temas**: oscuro, claro y Ollin (colores del logo), con selector en Ver → Tema.
- **Blueprints** (`.blueprint.json`): guardan una entidad y toda su jerarquía y se
  instancian con doble clic.
- **Ventana de juego** aparte (Ctrl+G), movible a otro monitor y a pantalla completa
  con F11.
- **Plantilla Tutorial** con recorrido guiado: HUD, estadísticas, inventario, diálogo,
  IA y guardado.
- **Símbolo y color por entidad** en la jerarquía.
- **Quitar componentes** desde el menú contextual de la jerarquía.
- La plantilla Vacío crea solo la cámara; las luces ya no vienen puestas.

### Corregido

- Las colisiones podían atravesarse tras un frame largo: la física ahora usa subpasos
  y anula la velocidad en el eje del impacto.

## [0.16.0] — 2026-10-30

### Añadido

- **Datos persistentes de script**: `Save.Set/Get/SetFlag/SetNumber/Remove`, incluidos
  en el guardado de partida.
- **Memoria de la IA**: el agente sigue buscando al objetivo unos segundos después de
  perderlo de vista, configurable en el inspector.
- Recuperación correcta de los datos de script al cargar una partida.
- Ejemplos de crafteo y puertas persistentes en la guía.
- 3 pruebas nuevas: 114 en total.

## [0.15.0] — 2026-10-30

### Añadido

- **IA**: componente `AiAgent` con patrulla, visión por etiqueta y persecución.
- **Pathfinding A*** sobre tilemaps sólidos, sin cortar esquinas.
- **Guardado y carga de partida** desde el menú Escena y el menú de pausa
  (`saves/partida.json` con la escena completa).
- Enemigo de ejemplo («Merodeador») en la escena de demostración.
- Pestaña del inspector para la IA.
- 6 pruebas nuevas: 111 en total.

### Pendiente

- Crafteo, clima y guardado de variables internas de scripts.

## [0.14.0] — 2026-10-30

### Añadido

- **Inventario** con capacidad, pilas, añadir/quitar y definiciones en `items/*.json`.
- **Estadísticas** configurables con máximo y desgaste por segundo.
- **Ciclo día/noche** con duración, hora, colores de día y noche aplicados al ambiente.
- Pestañas del inspector para Inventario, Estadísticas y Día/Noche.
- El script de ejemplo sincroniza vida y monedas con el HUD.
- 9 pruebas nuevas: 105 en total.

### Pendiente

- Crafteo, clima, IA con pathfinding y guardado persistente.

## [0.13.0] — 2026-10-30

### Añadido

- **HUD para scripts**: `Hud.SetBar`, `Hud.SetLabel`, `Hud.Message`, `Hud.GameOver` y
  `Hud.Clear`, dibujado sobre el viewport durante la reproducción.
- **Menú de pausa** con Escape: continuar, reiniciar escena, detener y volumen maestro.
- **Pantalla de fin de partida** con reintentar o detener.
- Bandera `--play` para abrir el proyecto y empezar a reproducir.
- 5 pruebas nuevas del HUD: 96 en total.

### Pendiente

- Guardar/cargar desde el menú y fuentes bitmap propias.

## [0.12.0] — 2026-10-30

### Añadido

- **Sombras 2D experimentales**: las luces puntuales pueden proyectar la silueta de los
  colliders (casilla *Proyectar sombras*, desactivada por defecto).
- Geometría de sombras reutilizable y pruebas de proyección.

### Nota

- La calidad de las sombras aún es básica (sin tilemaps ni suavizado). Se dejan como
  opción para no afectar la iluminación estable de 0.11.0.

## [0.11.0] — 2026-10-30

### Añadido

- **Luces 2D**: componente `Light2D` puntual o en cono, con color, radio, intensidad,
  ángulo y parpadeo. Se dibujan de forma aditiva sobre la escena.
- **Luz ambiente** por cámara: oscurece lo que no está iluminado.
- **Viñeta** sutil en el viewport para dar profundidad.
- **Cámara con seguimiento**: `FollowTag` y suavizado (por defecto sigue a `player`).
- **Paralaje** en sprites: factor 0 (fijo a la cámara) a 1 (mundo).
- Luces de ejemplo en la escena de demostración y en las plantillas (linterna en Terror).
- 5 pruebas nuevas: 88 en total.

### Pendiente

- Sombras por oclusión y post-proceso (niebla, grano, aberración cromática).

## [0.10.0] — 2026-10-30

### Añadido

- **Sistema de diálogos**: archivos `dialogs/<id>.json`, base de datos por proyecto,
  reproductor línea a línea y API `Dialogue.Play(id)` para scripts.
- **Interacción con E**: componente `Interactable` con aviso, radio, diálogo y uso
  único; el runtime busca al jugador por la etiqueta `player`.
- **Etiquetas de entidad** para identificar jugador, enemigos y objetos.
- **Interfaz de diálogo** en el viewport durante la reproducción (hablante, texto y
  aviso de continuación).
- **Callback `OnInteract`** para scripts y notas interactivas en las plantillas.
- Carpeta `dialogs/` en los proyectos nuevos con un diálogo de bienvenida.
- 11 pruebas nuevas: 83 en total.

## [0.9.0] — 2026-10-30

### Añadido

- Logo del motor: icono de la aplicación, del instalador y de la ventana, y logo en
  el diálogo «Acerca de» y en el README.
- Guía de uso completa en `docs/GUIA.md`: interfaz, componentes, scripts, audio,
  prefabs, atajos, problemas comunes y flujo recomendado.
- Sección de descargas en el README y notas de versión para GitHub Releases.
- Script `tools/release.ps1` que compila el instalador y publica el release (con `gh`).
- Iconos generados a partir del logo en `docs/images/` y `src/Ollin.Editor/Assets/`.

### Corregido

- El instalador mostraba la versión 0.4.0: ahora la lee del ensamblado, igual que el
  diálogo «Acerca de» y las notas del registro de Windows.
- El diálogo de proyecto nuevo tiene desplazamiento y altura mínima; sus botones ya no
  se pierden en pantallas pequeñas.
- El mismo diálogo ya no se cierra al fallar la validación y evita un bloqueo al
  envolver su contenido.

## [0.8.0] — 2026-10-30

### Añadido

- Arrastrar scripts desde el explorador a una entidad (jerarquía o viewport) para
  enlazarlos, y prefabs para instanciarlos en el punto de destino.
- Menú contextual de entidad: crear hijo, añadir componente, duplicar, enfocar,
  guardar como prefab y eliminar.
- Botones para añadir componentes desde el inspector y quitar el script de una entidad.
- Eliminar archivos del proyecto con clic derecho en el explorador (con confirmación).
- Doble clic en la jerarquía centra la cámara en la entidad.
- Autocompletado C# en el editor: `Ctrl+Espacio` y tras `.`, con palabras clave,
  tipos, API, entidades de la escena y clases de script.
- 2 pruebas del catálogo de autocompletado: 76 en total.

## [0.7.1] — 2026-10-30

### Corregido

- El diálogo de proyecto nuevo valida nombre y carpeta en línea y ya no se cierra
  perdiendo los datos si la carpeta está ocupada.
- Guardar un prefab como `torreta.json` producía `torreta.json.prefab.json`;
  ahora la extensión se normaliza siempre a `.prefab.json`.
- Cerrar una escena con cambios pide confirmación en lugar de no hacer nada.
- No se puede cambiar ni cerrar una escena mientras se reproduce (evita desincronizar
  la escena del runtime).
- El pintado de tilemap funciona arrastrando y avisa si no hay un tilemap seleccionado.
- Las pestañas de script se sincronizan con la recarga en caliente cuando el archivo
  cambia por fuera y no hay cambios sin guardar.
- 6 pruebas nuevas de regresión: 74 en total.

## [0.7.0] — 2026-10-30

### Añadido

- Audio 2D con NAudio: componente `AudioSource`, canales de efecto y música,
  fundidos y API `Audio.Play` / `Audio.Music` para scripts. Sonidos de ejemplo.
- Prefabs `*.prefab.json`: guardar la entidad seleccionada e instanciarla desde el
  explorador de archivos.
- Copiar y pegar entidades con `Ctrl+C` / `Ctrl+V`.
- Buscador de entidades en la jerarquía.
- Plantillas de proyecto: Vacío, Plataformas y Terror, con escena y script propios.
- Colisión por celda en tilemaps y cuerpos cinemáticos.
- 8 pruebas nuevas (prefabs, audio, colisión por celda y plantillas): 68 en total.

## [0.6.0] — 2026-10-30

### Añadido

- Resaltado de sintaxis C# en el editor integrado, con numeración de líneas y tema
  oscuro propio (AvaloniaEdit).
- Escenas múltiples: cada pestaña conserva su escena, cámara, selección e historial.
- Pestañas de escena numeradas, cerrables y reutilizables al abrir la misma escena.

### Corregido

- El editor de código no cargaba el texto si el script se abría antes de crear la ventana.

## [0.5.0] — 2026-10-30

### Añadido

- Sistema de proyectos: crear, abrir y cerrar proyectos con carpetas `scenes`,
  `scripts`, `assets` y `prefabs`, archivo `project.ollin` y contenido inicial.
- Recordatorio del último proyecto y pantalla de bienvenida con acciones.
- Pestañas de documentos en el área central (escena + scripts abiertos).
- Editor de scripts integrado con guardado (`Ctrl+S`) y recarga en caliente.
- Dock inferior con pestañas **Consola** y **Archivos**, con explorador del proyecto
  que abre scripts, escenas y texturas con doble clic.
- Inspector reorganizado en pestañas: Entidad, Visual, Mundo, Tilemap y Cámara.
- Apertura por línea de comandos: `--project` y `--script`.
- 5 pruebas del sistema de proyectos (60 en total).

### Corregido

- El editor de scripts heredaba la altura fija del estilo global de `TextBox`.

## [0.4.0] — 2026-10-30

### Añadido

- `SpriteAnimator`: animación por hoja de sprites con cuadros, FPS, bucle y recorte.
- `Tilemap`: rejilla con tileset, redimensionado y pintado con el ratón en el viewport.
- Física 2D: `BoxCollider`, `Rigidbody2D` con gravedad, resolución AABB y disparadores.
- Avisos de física a los scripts: `OnCollisionEnter`, `OnTriggerEnter` y `OnTriggerExit`.
- Rutinas en scripts: `StartCoroutine` con `WaitForSeconds`.
- `DebugDraw.Line/Rect` para depurar desde los scripts.
- Hoja de animación del héroe y tileset de ejemplo; la escena por defecto usa tilemap.
- Instalador de Windows con accesos directos y desinstalador (`tools/Ollin.Setup`).
- Guía de la beta y de generación del instalador en `docs/BETA.md`.
- 8 pruebas nuevas (55 en total).

### Corregido

- Los tilemaps nuevos nacían con el mosaico 0 en todas las celdas.
- El recorte de animación no se aplicaba en modo edición (se veía la hoja completa).
- El desinstalador no podía borrar su propia carpeta; ahora se relanza desde `%TEMP%`.

## [0.3.0] — 2026-10-30

### Añadido

- Proyecto `Ollin.Scripting` con compilación en memoria mediante Roslyn.
- Clase base `OllinScript` con `OnStart`, `OnUpdate` y `OnDestroy`.
- `ScriptWorkspace` con `AssemblyLoadContext` recolectable para recargar scripts.
- Recarga en caliente al guardar un script **sin detener** la reproducción, conservando
  los campos compatibles y llamando a `OnReload`.
- Vigilancia automática de la carpeta `scripts/` para recompilar al guardar.
- API de scripts `Input` (teclado y ratón) y `Time` (`DeltaTime`, `Elapsed`, `Frame`).
- `Warn` y `Error` en la API de depuración de scripts.
- Sección de script en el inspector y creación de plantillas desde el editor.
- Ciclo de vida de scripts en `SceneRuntime`, con aislamiento de errores.
- Errores de compilación en la consola con archivo, línea y código (por ejemplo `CS0029`).
- Script de ejemplo `PlayerBob` enlazado al jugador de la escena por defecto.
- Seis pruebas del compilador y del workspace, más seis de entrada, tiempo y recarga en caliente (47 en total).

### Corregido

- La consola no seguía los mensajes nuevos (desplazamiento diferido al final del layout).
- Un proyecto sin scripts se reportaba como error de compilación.

## [0.2.0] — 2026-10-30

### Añadido

- Proyecto `Ollin.Runtime` con reloj, pausa y restauración exacta de la escena.
- Controles Reproducir, Pausar y Detener (F5/F6) con bloqueo de la edición.
- Componentes animables `RotatorComponent` y `OscillatorComponent`.
- Pipeline de assets: panel con miniaturas, importación y asignación de texturas.
- `ProjectLocator` y rutas relativas para que el proyecto se pueda mover.
- Gizmos de mover, rotar y escalar en el viewport, con ajuste manteniendo `Ctrl`.
- Historial de deshacer/rehacer con agrupación de ediciones continuas.
- Sprites de ejemplo (héroe, caja, moneda, suelo).

### Corregido

- El gizmo no registraba el paso de deshacer al soltar el ratón.
- El inspector y el gizmo se retroalimentaban generando pasos intermedios.

## [0.1.0] — 2026-10-30

### Añadido

- Solución con `Ollin.Core`, `Ollin.Rendering` y `Ollin.Editor`.
- Modelo de escena con jerarquía, componentes y serialización JSON.
- Viewport OpenGL ES 3.0 con sprite batch, cuadrícula y ejes.
- Cámara ortográfica con paneo, zoom y encuadre.
- Jerarquía, inspector en vivo, consola y barra de estado.
- Selección por árbol y por clic, con contorno de selección.
- Guardar y abrir escenas, marca de cambios sin guardar.
- 17 pruebas del núcleo.
