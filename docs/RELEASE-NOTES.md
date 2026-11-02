## Ollin Beta 6.54.0 — cierre de 6.52

### Incluye

- **Paso atrás/adelante de traza**.
- **Diff de variables** `antes → después` por nodo.
- **205 pruebas automatizadas**.

### Cómo probarlo

1. Instala 6.54.0 y abre el grafo.
2. Reproduce una acción que cambie `GameState` y pausa.
3. Usa **Más ▾ → Paso atrás (traza)**.
4. Revisa el diff de variables del nodo.

### Limitaciones

- El paso atrás es de **traza**, no revierte el mundo.
- El diff solo muestra variables cambiadas, máximo 8.
- La demo técnica no se publica.