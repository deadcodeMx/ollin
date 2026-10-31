## Ollin 0.27.0 beta — navegación, nivel, combate y pausa

### Incluye

- **IA con A\*** para rodear paredes en laberintos.
- **Pincel de pared/suelo**, **imán** y **duplicar serie x4** en el editor.
- **Proyectiles**, **dos armas** (1/2), cargador, reserva y recarga con R.
- **Reaparición** con 3 vidas y **checkpoints**.
- **Menú de pausa** en el juego exportado: continuar, guardar, cargar, opciones y salir.
- **Botones, triggers, generadores y checkpoints** configurables sin código.
- 145 pruebas automatizadas.

### Cómo probarlo

1. Instala 0.27.0 y crea un proyecto **2.5D (pasillo)**.
2. En el editor usa **Pared / Suelo / Imán / Serie x4**.
3. Reproduce: 1/2 cambia de arma, R recarga, E interactúa, Esc abre la pausa.
4. Guarda y carga desde el menú de pausa.

### Limitaciones

- La navegación es por rejilla; niveles muy grandes o rampas siguen fuera de alcance.
- El inspector edita el arma de la ranura 1; la escopeta viene configurada en la plantilla.
- Sin nodos visuales: los comportamientos son componentes con campos.