# Componentes necesarios para juegos 2D

Objetivo: cubrir los sistemas que aparecen en la mayoría de juegos 2D sin convertir el motor en una colección de componentes específicos de un solo género.

Referencias:
- Godot 4, lista de características: https://docs.godotengine.org/en/stable/about/list_of_features.html
- Godot 4, tutoriales 2D: https://docs.godotengine.org/en/stable/tutorials/2d/index.html
- Unity 2D Physics: https://docs.unity3d.com/Manual/Physics2D.html
- GameMaker, colisiones: https://manual.gamemaker.io/monthly/en/GameMaker_Language/GML_Reference/Movement_And_Collisions/Collisions/Collisions.htm

Prioridades:
- **P0**: sin esto no se puede hacer un juego completo.
- **P1**: cubre plataformas, acción, RPG, aventura y shooters.
- **P2**: mejora el acabado y cubre géneros más complejos.
- **P3**: especializado o de producción grande.

## P0 — Núcleo

| Componente / sistema | Para qué | Estado Ollin |
|---|---|---|
| Transform2D | Posición, rotación, escala, jerarquía | Sí |
| SpriteRenderer | Dibujar sprite, atlas, flip, orden | Sí, básico |
| SpriteAnimator | Animación por frames | Sí, básico |
| Camera2D | Cámara ortográfica, zoom | Sí |
| Tilemap | Niveles por celdas | Sí, básico |
| BoxCollider2D | Colisión rectangular | Sí |
| Rigidbody2D | Cuerpo dinámico/estático/kinemático | Sí |
| Area2D / Trigger | Detectar sin empujar | Parcial con BoxCollider trigger |
| AudioSource | Música y efectos | Sí |
| Input Map | Acciones, teclado, mando | Parcial |
| UI básica | HUD, menús, botones, texto | Parcial |
| Scene / Prefab | Escenas y objetos reutilizables | Sí, básico |
| Save / Load | Guardar partida | Parcial |
| Game State | Pausa, game over, reinicio | Parcial |
| Scripting | Lógica de juego | Sí, C# |
| Sorting / Capas | Orden visual | Básico |

## P1 — Imprescindibles de gameplay

### Física y movimiento

| Componente | Uso |
|---|---|
| CharacterBody2D | Movimiento controlado con física |
| KinematicBody2D | Plataformas y enemigos predecibles |
| CircleCollider2D | Bolas, personajes redondos |
| CapsuleCollider2D | Personajes humanos |
| PolygonCollider2D | Terreno y formas irregulares |
| EdgeCollider2D | Suelos y rampas |
| OneWayPlatform | Plataformas atravesables |
| MovingPlatform | Plataformas móviles con arrastre |
| SlopeController | Rampas y pendientes |
| Joint2D | Pin, distancia, muelle, deslizante |
| RayCast2D | Suelo, paredes, línea de visión |
| ShapeCast2D | Detección de formas |
| Collision Layers / Masks | Filtrar colisiones |
| PhysicsMaterial2D | Rebote y fricción |
| Rigidbody sleeping | Optimización |

### Combate y vida

| Componente | Uso |
|---|---|
| Health | Vida, daño, invulnerabilidad |
| Damageable | Recibir daño |
| Hitbox | Zona que hace daño |
| Hurtbox | Zona que recibe daño |
| DamageType | Físico, fuego, etc. |
| Knockback | Empuje al recibir daño |
| Invulnerability | Parpadeo y margen |
| Death / Loot | Muerte y recompensa |
| Projectile | Balas, flechas, magia |
| Weapon | Cadencia, daño, munición |
| MeleeAttack | Ataque cuerpo a cuerpo |
| Team / Faction | Aliados y enemigos |
| StatusEffect | Veneno, quemadura, ralentización |
| Shield / Armor | Absorción de daño |
| Combo / Cooldown | Habilidades |

### Interacción y mundo

| Componente | Uso |
|---|---|
| Interactable | Hablar, abrir, recoger |
| Pickup | Monedas, llaves, objetos |
| Door / Portal | Cambio de zona |
| Chest / Container | Contenedores |
| Checkpoint | Punto de reaparición |
| Respawn | Reaparición |
| TriggerZone | Eventos por entrada |
| Spawner | Enemigos y objetos |
| WaveManager | Oleadas |
| Destructible | Objetos rompibles |
| Collectible | Coleccionables |
| Sign / Readable | Textos del mundo |
| PressurePlate | Placas de presión |
| Lever / Switch | Palancas |
| MovingPath | Patrullas y plataformas |
| Teleporter | Teletransporte |

### Cámara y presentación

| Componente | Uso |
|---|---|
| CameraFollow | Seguir objetivo |
| CameraDeadzone | Zona muerta |
| CameraShake | Impacto y explosiones |
| CameraBounds | Límites de mapa |
| CameraLookAhead | Anticipación de movimiento |
| ParallaxLayer | Fondos con profundidad |
| BackgroundLayer | Capas de fondo |
| CanvasLayer | UI y capas independientes |
| YSort | Orden por profundidad |
| ScreenTransition | Fundidos y cambios de escena |
| Viewport / SubViewport | Minimapas y cámaras múltiples |

### Animación

| Componente | Uso |
|---|---|
| AnimationClip | Secuencia de frames |
| AnimationStateMachine | Idle, run, jump, attack |
| AnimationTransition | Condiciones entre estados |
| AnimationEvent | Disparar sonido/daño en un frame |
| Tween / Easing | Movimiento y UI suave |
| ParticleEmitter2D | Chispas, humo, sangre |
| TrailRenderer | Estelas |
| SpriteSheet / Atlas | Optimización de texturas |
| NinePatch | UI y bordes escalables |
| ShaderMaterial | Efectos visuales |
| Flash / Tint | Feedback de daño |

### IA y navegación

| Componente | Uso |
|---|---|
| AIController | Base de enemigos/NPC |
| StateMachine | Patrulla, persigue, ataca |
| BehaviorTree | IA más compleja |
| PatrolPath | Rutas |
| ChaseTarget | Persecución |
| LineOfSight | Visión |
| Hearing / Alert | Detección |
| Pathfinding A* | Rutas en grid |
| Navigation2D | Movimiento por superficies |
| Avoidance | Evitar obstáculos |
| NPC Schedule | Rutinas de NPC |
| Conversation / Dialogue | Diálogos |

### Inventario, progresión y economía

| Componente | Uso |
|---|---|
| Inventory | Objetos y slots |
| ItemDefinition | Datos de objeto |
| Equipment | Armas, armadura |
| Hotbar | Acceso rápido |
| Crafting | Recetas |
| Shop | Compra/venta |
| Currency | Dinero |
| LootTable | Botín |
| Stats / Attributes | Fuerza, defensa, velocidad |
| Experience / Level | Progresión |
| SkillTree | Habilidades |
| Quest | Misiones |
| Objective | Objetivos |
| Achievement | Logros |

### UI / HUD

| Componente | Uso |
|---|---|
| Canvas / UI Root | Base de interfaz |
| Label / Text | Texto |
| Button | Botones |
| Image | Iconos e imágenes |
| ProgressBar | Vida, energía |
| Panel / Container | Layout |
| GridContainer | Inventario |
| ScrollView | Listas |
| Tooltip | Información |
| Minimap | Mapa |
| Crosshair | Apuntado |
| PauseMenu | Pausa |
| SettingsMenu | Opciones |
| KeyRebind | Reasignar teclas |
| Localization | Idiomas |
| Accessibility | Tamaño, contraste, subtítulos |

### Audio

| Componente | Uso |
|---|---|
| AudioSource2D | Sonido posicional |
| AudioSourceGlobal | Música/UI |
| AudioBus / Mixer | Volúmenes |
| MusicCrossfade | Cambio de música |
| SfxPool | Muchos efectos |
| RandomPitch | Variación |
| AudioZone | Ambiente por zona |
| Ducking | Bajar música al hablar |

## P2 — Acabado y géneros avanzados

| Sistema | Géneros |
|---|---|
| Light2D + Occluder | Terror, aventura |
| Normal maps / 2D shaders | Acción, atmósfera |
| Skeletal2D (Spine/DragonBones) | Personajes animados |
| IK2D | Extremidades |
| DestructibleTerrain | Worms, excavación |
| Grid / HexGrid | Estrategia, táctico |
| TurnManager | RPG por turnos |
| Card / Deck | Cartas, roguelike |
| DialogueGraph | Aventura, RPG |
| QuestGraph | RPG |
| ProceduralGenerator | Roguelike |
| Room / Dungeon | Roguelike |
| RacingPath | Carreras |
| Vehicle2D | Coches |
| FightingState | Versus |
| RhythmTrack | Ritmo |
| Match3 | Puzzle |
| PhysicsPuzzle | Puzzle |
| SaveSlots | Varios guardados |
| CloudSave | Multiplataforma |
| Profiler | Optimización |
| DebugConsole | Desarrollo |
| Replay / Ghost | Carreras |
| TimeScale / SlowMotion | Acción |
| ScreenEffects | Postproceso |
| Achievement/Steam | Distribución |
| Multiplayer | Netcode |
| Lobby / Matchmaking | Multijugador |
| Rollback | Lucha/online |
| ECS | Mundos grandes |

## Cobertura por género

| Género | P0 | P1 clave | P2 clave |
|---|---|---|---|
| Plataformas | Sí | CharacterBody, OneWay, CameraFollow, Health | Shaders, partículas |
| Metroidvania | Sí | Checkpoint, Inventory, Ability gates | Minimap, diálogos |
| Top-down acción | Sí | AI, Projectile, Hitbox, YSort | Light2D, pathfinding |
| RPG | Sí | Stats, Inventory, Quest, Dialogue | Turnos, skill tree |
| Roguelike | Sí | Procedural, Loot, Deck/Grid | Meta-progresión |
| Shooter 2D | Sí | Projectile, Weapon, AI, CameraShake | Shaders, multiplayer |
| Puzzle | Sí | Grid, Trigger, UI | Match3, physics puzzle |
| Aventura | Sí | Interactable, Dialogue, Inventory | DialogueGraph |
| Terror | Sí | Light2D, AudioZone, AI | Shaders, oclusión |
| Carreras | Sí | Vehicle, Checkpoint, Camera | Replay, time trial |
| Estrategia/RTS | Sí | Grid, Selection, Pathfinding | Fog of war, formations |
| Cartas | Sí | Deck, Card, TurnManager | Animaciones, online |
| Ritmo | Sí | Audio sync, Input timing | Chart editor |
| Gestión | Sí | Grid, Economy, UI | Needs, schedules |

## Estado actual de Ollin

Ya existe:
- Transform, Sprite, Animator, Camera2D.
- Tilemap básico.
- BoxCollider, Rigidbody2D, trigger.
- AudioSource, Light2D.
- IA básica, Interactable, Inventory, Stats, DayNight.
- Health, Weapon, Pickup, Door, Rules, Intro, Respawn.
- Scripts C#, varios por entidad.
- UI de editor, jerarquía, inspector, prefabs, escenas.

Falta o está incompleto:
- CharacterBody2D y controlador de plataformas.
- Colisiones circle/capsule/polygon/edge.
- Area2D real y capas/máscaras.
- One-way y plataformas móviles.
- Hitbox/Hurtbox separados.
- Cámara con deadzone, shake y bounds.
- Parallax, partículas, trail.
- Animation State Machine y eventos.
- Pathfinding y navegación.
- Inventario/equipo completo.
- Diálogos y misiones.
- UI runtime (HUD, menús, inventario).
- Save/load de partida y ajustes.
- Input remapping y mando.
- Tilemap avanzado: autotile, terrain, colliders.
- Audio mixer y buses.
- Localización y accesibilidad.
- Profiler y debug tools.

## Orden recomendado

### Etapa A — Base jugable
1. CharacterBody2D.
2. Colliders circle/capsule/polygon/edge.
3. Collision layers/masks.
4. Area2D y trigger real.
5. CameraFollow + deadzone + bounds.
6. Health + Hitbox/Hurtbox.
7. Projectile + Weapon.
8. Checkpoint/respawn.

### Etapa B — Nivel y presentación
9. One-way y moving platforms.
10. Parallax.
11. Partículas y trail.
12. Animation State Machine.
13. Tilemap autotile y colliders.
14. Screen transitions.

### Etapa C — Juego completo
15. Inventory/equipment.
16. Dialogue.
17. Quest/objectives.
18. Save/load + settings.
19. UI runtime/HUD/menus.
20. Input rebinding/gamepad.
21. Audio mixer.
22. Localization.

#### Estado real de la Etapa C (Beta 6.110.0)

| Punto | Estado | Detalle |
|---|---|---|
| 15. Inventory/equipment | ⚠️ Parcial | Inventario y `Equipment` existen; falta pantalla para equipar/desequipar y arrastrar objetos. |
| 16. Dialogue | ✅ | Diálogos por JSON y localizables por clave. |
| 17. Quest/objectives | ⚠️ Parcial | `QuestLog` y HUD de objetivo; no hay editor visual ni carga de misiones desde assets. |
| 18. Save/load + settings | ✅ | 3 ranuras, autoguardado, respaldo `.bak` y ajustes del jugador. |
| 19. UI runtime/HUD/menus | ✅ | HUD, pausa, fin de partida e inventario. |
| 20. Input rebinding/gamepad | ⚠️ Parcial | Input Map editable como texto y gamepad XInput; falta captura automática de tecla/botón. |
| 21. Audio mixer | ⚠️ Parcial | Master, música, efectos y UI; el volumen se aplica al iniciar cada sonido, no a los ya activos. |
| 22. Localization | ⚠️ Parcial | Carga `localization/*.json` y localiza diálogos; falta selector de idioma en ajustes. |

#### Limitaciones actuales de la Etapa C

- No existe pantalla de equipamiento; `Equipment` solo tiene datos, API e inspector de lectura.
- No hay editor visual de misiones; `QuestLog` se rellena por código y no hay carpeta `quests/*.json`.
- La localización no tiene selector de idioma ni textos localizados en todos los menús.
- El rebinding se edita manualmente en el Input Map; no hay captura automática de tecla o botón.
- El gamepad usa XInput y solo está probado en Windows.
- El mixer no cambia el volumen de sonidos ya reproduciéndose.
- El inventario no tiene arrastrar/soltar ni combinación de objetos.
- El HUD de misiones muestra un solo objetivo activo.

### Etapa D — Géneros
23. Pathfinding/AI.
24. Grid/hex + turn manager.
25. Cards/deck.
26. Procedural generation.
27. Light2D avanzado/shaders.
28. Skeletal/IK.
29. Multiplayer.

## Regla de diseño

- Un componente debe resolver una responsabilidad.
- Evitar componentes específicos de un juego (Weapon concreta, puerta concreta) si pueden ser datos + componentes genéricos.
- Los componentes deben combinarse: `Health + Hitbox + Hurtbox + Knockback`.
- Los datos de contenido (items, diálogos, misiones) deben vivir en assets, no en código.
- Todo componente necesita inspector, undo, serialización y preview.
