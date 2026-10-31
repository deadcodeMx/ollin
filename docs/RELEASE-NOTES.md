## Ollin 0.29.1 beta — grafo ilimitado, pincel visual y guardado por ranuras

### Incluye

- **Grafo de nodos ilimitado** con canvas, conexiones, condiciones y ramas Sí/No.
- **Vista previa verde del pincel** y texturas de pared/suelo con el ratón.
- **Editor de armas por ranuras 1/2/3** y botón para añadir armas.
- **Tres ranuras de guardado** y **autoguardado cada 45 s** en el juego exportado.
- 152 pruebas automatizadas.

### Corregido

- El evento «al iniciar» se ejecuta una sola vez en el grafo y en el script visual.

### Cómo probarlo

1. Instala 0.29.1 y crea un proyecto **2.5D (pasillo)**.
2. Selecciona una entidad y pulsa **Grafo**; añade evento + acción y conéctalos.
3. En **Nivel**, elige textura/tamaño y activa **Pared** o **Suelo**; la caja verde sigue
   al ratón.
4. En **Arma**, cambia de ranura y añade otra arma.
5. En el juego, abre la pausa y guarda/carga en una de las tres ranuras.

### Limitaciones

- El grafo ejecuta cadenas de eventos/condiciones/acciones; no evalúa expresiones ni bucles.
- El autoguardado escribe en la ranura activa con sufijo `-auto`; no reemplaza la manual.
- El pincel no edita vértices.