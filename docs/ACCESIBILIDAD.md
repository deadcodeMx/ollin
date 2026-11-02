# Accesibilidad del editor de grafo

## Comprobaciones automáticas

`tools/ui-smoke.ps1` comprueba, mediante UI Automation:

- El buscador expone el nombre **Buscar nodo**.
- El botón **Más** expone `More graph actions`.
- El grafo abre correctamente.

Además, el editor expone:

- Nombre de ventana: **Grafo de nodos**.
- Estado del grafo como región viva (`Polite`).
- Texto de ayuda en el buscador.
- Nombres en los botones de la barra y en la ventana **F1**.

## Prueba manual con Narrator

1. Abre Ollin y el editor de grafo.
2. Activa Narrator con `Win + Ctrl + Enter`.
3. Pulsa `F1`.
4. Comprueba que Narrator anuncia el título **Atajos del grafo**.
5. Recorre las acciones con `Tab` y confirma que se lee cada atajo.
6. Pulsa `Esc` y comprueba que la ventana se cierra.
7. Enfoca el buscador con `Ctrl+F`: debe anunciarse **Buscar nodo**.
8. Escribe y comprueba que el estado del grafo se anuncia sin bloquear la edición.
9. Reduce la ventana y verifica que el foco no queda atrapado.

## Prueba manual con NVDA

1. Inicia NVDA.
2. Abre el grafo y pulsa `F1`.
3. Verifica que se lee el título y las acciones.
4. Navega con `Tab`/`Shift+Tab` y `Enter`.
5. Comprueba el buscador y el estado.

## Pendiente

- Ejecutar la prueba manual en Narrator y NVDA.
- Añadir contraste alto y tamaño de texto si se solicita.
