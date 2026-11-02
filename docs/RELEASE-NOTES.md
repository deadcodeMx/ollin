## Ollin Beta 6.65.0 — desinstalador corregido

### Corregido

- **Desinstalar.exe funciona con doble clic**, sin argumentos.
- Confirmación antes de desinstalar.
- Cierra el editor/reproductor si están abiertos.
- Espera al proceso padre para evitar bloqueos.
- Reintenta el borrado de la carpeta.
- Elimina registro y accesos directos.
- Añade **Desinstalar Ollin** al menú Inicio.

### Probado

- Instalación silenciosa.
- Desinstalación con `--uninstall --silent`: carpeta y registro eliminados.
- Desinstalación sin `--uninstall` (equivalente a doble clic): carpeta eliminada.
- **205 pruebas automatizadas**.

### Limitaciones

- La confirmación visual no se automatiza; se probó la ruta silenciosa.
- La demo técnica no se publica.