# Versionado y releases

## Estado actual

- Última versión legacy: **6.278.0**.
- Esa versión queda congelada como cierre del ciclo 6.x.
- El próximo ciclo público será **7.0.0-beta.1**.
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
7. Se conservan como máximo las **10 releases más recientes**.
8. Las releases antiguas se eliminan de GitHub; el historial resumido queda en `CHANGELOG.md`.

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

El **2026-11-05** se conservaron las 10 releases/tags más recientes:

```text
v6.269.0 … v6.278.0
```

Se eliminaron **246 releases** y **167 tags**. El corte se debe a que se habían
acumulado 256 releases y más de 160 tags, muchas de un solo cambio.

La versión de desarrollo actual es **7.0.0-beta.1**; todavía no tiene release público.

## Próximo ciclo

1. Cerrar 6.278.0 como legacy.
2. Abrir `7.0.0-beta.1` con el siguiente bloque funcional.
3. Publicar solo cuando build, pruebas e instalador estén verificados.
4. Actualizar `CHANGELOG.md` con el resumen del bloque, no con cada commit.
