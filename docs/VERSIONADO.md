# Versionado y releases

## Estado actual

- Última versión legacy: **6.278.0**.
- Esa versión queda congelada como cierre del ciclo 6.x.
- La beta consolidada actual es **7.0.0-beta.13**.
- No se publica una release por cada cambio pequeño.

## Esquema

Se usa SemVer:

```text
MAJOR.MINOR.PATCH[-pre.N]
```

- **MAJOR**: cambios incompatibles de arquitectura o de formato.
- **MINOR**: funciones nuevas compatibles.
- **PATCH**: correcciones sin funciones nuevas.
- **Pre-release**: `-beta.N` o `-rc.N` para betas.

Ejemplos:

```text
7.0.0-beta.1
7.0.0-beta.2
7.0.0-rc.1
7.0.0
7.1.0
7.1.1
```

## Reglas

1. `Directory.Build.props` es la única fuente de la versión.
2. Una release publicada no se reutiliza ni se reescribe.
3. No se crea tag si ya existe.
4. No se publica si el build o las pruebas fallan.
5. Una release debe tener notas en `docs/RELEASE-NOTES.md`.
6. Los builds de desarrollo no se publican como releases.
7. Se conservan como máximo las **5 releases más recientes**.
8. No se publica una release por cada corrección: se acumulan en una ventana.
9. Los builds intermedios son locales; solo se publica el cierre de ventana.
10. Las releases antiguas se eliminan de GitHub; el historial resumido queda en `CHANGELOG.md`.

## Ventanas de release

- Una ventana dura como mínimo una semana o hasta cerrar un bloque de feedback.
- Dentro de la ventana: build y pruebas locales, sin releases.
- Al cerrar: una sola release con todas las correcciones.
- No se reutiliza ni se renombra un tag publicado.

## Cadencia

- **Desarrollo diario**: build local, sin release.
- **Hito**: beta cuando hay un bloque funcional Cerrado.
- **Estable**: solo cuando el juego de validación esté completo.
- **Máximo**: una release pública por semana salvo corrección urgente.

## Assets por release

- Windows: `Ollin-Setup-<version>.exe` (si la versión ya incluye `-beta.N`, no se duplica el sufijo).
- Linux: paquete `.tar.gz` cuando esté validado.
- `SHA256SUMS.txt`.
- Notas de release.

## Limpieza aplicada

El **2026-11-05** se consolidaron las betas 7.0.0-beta.1 a beta.5 en **7.0.0-beta.6**. Quedaron 5 releases/tags:

```text
v6.277.0 … v6.278.0
v7.0.0-beta.6
v7.0.0-beta.7
v7.0.0-beta.8
v7.0.0-beta.9
v7.0.0-beta.10
v7.0.0-beta.11
v7.0.0-beta.12
v7.0.0-beta.13
```

Se eliminaron **246 releases** y **167 tags**. El corte se debe a que se habían
acumulado 256 releases y más de 160 tags, muchas de un solo cambio.

La versión de desarrollo actual es **7.0.0-beta.6**; las correcciones posteriores (tema claro) son locales y se publicarán en la próxima ventana.

## Próximo ciclo

1. Cerrar el feedback restante de interfaz en local.
2. Ejecutar build y 186 pruebas.
3. Publicar una sola vez como `7.0.0-rc.1`.
4. No crear releases intermedias por correcciones.