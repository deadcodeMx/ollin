## Ollin 0.30.0 beta — variables y expresiones en el grafo

### Incluye

- **Variables globales** de partida: contadores, flags y textos.
- Acciones **Fijar variable**, **Sumar variable** y **Alternar flag**.
- Condición **Expresión**: comparaciones, aritmética, booleanos, paréntesis y
  `tiene("llave")`.
- Variables de sistema: `vida`, `vidamax`, `vidas`, `min`, `max`.
- Las variables se guardan/cargan con la partida.
- 156 pruebas automatizadas.

### Cómo probarlo

1. Instala 0.30.0 y abre un proyecto **2.5D (pasillo)**.
2. Selecciona una entidad y pulsa **Grafo**.
3. Añade evento **Al iniciar** → **Fijar variable** (`contador` = `0`) → **Sumar variable**
   (`contador` += `1`) → condición **Expresión** (`contador >= 1`) → **Abrir puerta**.
4. Reproduce y comprueba que la puerta se abre; guarda y carga para conservar el contador.

### Limitaciones

- Las variables son globales de la partida; no hay variables por objeto.
- Las expresiones no tienen funciones definidas por el usuario ni bucles.
- Los textos se comparan sin distinguir mayúsculas.