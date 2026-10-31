## Ollin 0.34.0 beta — robustez de escenas

### Incluye

- Carga tolerante de escenas: error claro con línea/columna, sin tumbar el editor.
- Validador de **assets faltantes** (texturas, audio, scripts, proyectiles y armas).
- Validador de **jerarquía**: padres inexistentes, ciclos, auto-parentesco e IDs duplicados.
- Guardado/carga de **entidades hijas 2.5D** con posición local y mundo.
- **GameState** se limpia al cambiar de escena y se restaura al cargar partida.
- **177 pruebas automatizadas**.

### Cómo probarlo

1. Instala 0.34.0.
2. Abre una escena válida y comprueba la consola.
3. Corrompe una copia del `.json` y ábrela: debe mostrar error sin cerrar la escena actual.
4. Borra una textura o audio y vuelve a abrir: aparecerá un aviso de asset faltante.
5. Crea un padre y un hijo, guarda, cierra y abre: el hijo conserva padre y posición.

### Limitaciones

- La validación avisa, no repara automáticamente.
- Una escena corrupta no recupera entidades parciales.
- La jerarquía 2.5D se guarda/carga, pero el renderer y la IA de hijos aún usan
  transformaciones locales en varios sistemas.
- El validador no cubre todos los assets posibles ni migraciones de formato.