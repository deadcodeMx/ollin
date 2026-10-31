## Ollin 0.28.0 beta — scripting visual y más componentes

### Incluye

- **Script visual por reglas**: evento + condición + acción, sin código.
- **Cuatro reglas** por entidad con objetivo, mensaje, radio y repetición.
- Acciones nuevas: **dañar**, **curar** y **teletransportar**.
- Componentes **Peligro 3D**, **Teletransporte** y **Seguidor 3D**.
- **Pincel configurable**: tamaño de pared/suelo y serie con cantidad y separación.
- 150 pruebas automatizadas.

### Cómo probarlo

1. Instala 0.28.0 y crea un proyecto **2.5D (pasillo)**.
2. Añade **Script visual** a una entidad y configura una regla (por ejemplo, al iniciar →
   abrir puerta).
3. Añade **Peligro 3D** o **Teletransporte** y pruébalo al reproducir.
4. Ajusta el pincel en la pestaña **Nivel** y coloca paredes o suelos.

### Limitaciones

- El script visual son reglas fijas, no un grafo de nodos.
- Cuatro reglas por entidad; para lógica muy compleja se sigue necesitando C#.
- El pincel no tiene previsualización fantasma todavía.