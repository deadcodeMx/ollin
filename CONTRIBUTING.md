# Cómo contribuir a Ollin

¡Gracias por el interés! Este documento resume lo necesario para compilar, probar y
enviar cambios sin fricción.

## Preparar el entorno

```powershell
git clone https://github.com/deadcodeMx/ollin.git
cd ollin
dotnet build Ollin.sln
dotnet run --project src/Ollin.Editor
```

Requisitos: **.NET 8 SDK** y Windows 10/11 con OpenGL ES 3.0.

## Pruebas

Antes de abrir un *pull request*, ejecuta:

```powershell
dotnet run --project tests/Ollin.Tests
```

(o `dotnet test tests/Ollin.Tests` en un entorno normal). El resultado debe ser
**41/41**. Si añades funcionalidad, añade pruebas en el proyecto que corresponda:

| Cambio | Proyecto de pruebas |
| --- | --- |
| Escena, serialización, historial, assets | `tests/Ollin.Tests` |
| Runtime, scripts | `tests/Ollin.Tests` (RuntimeTests, ScriptingTests) |

## Estilo de código

- **Código en inglés** (clases, métodos, campos, mensajes de commit).
- **Interfaz y mensajes de usuario en español.**
- Namespaces alineados con la carpeta: `Ollin.Core.Model`, `Ollin.Rendering`, …
- Un tipo público por archivo; comentarios XML en la API pública.
- Nada de dependencias nuevas sin justificarlo en el PR.

## Flujo de trabajo

1. Abre un *issue* describiendo el problema o la propuesta.
2. Crea una rama: `feat/mi-cosa`, `fix/mi-error`, `docs/mi-texto`.
3. Commits pequeños y en imperativo: `Añade gizmo de rotación`.
4. Abre el *pull request* rellenando la plantilla.
5. Asegúrate de que la CI pasa.

## Añadir un componente nuevo

1. Crea la clase en `src/Ollin.Core/Model/Components/` heredando de `Component`.
2. Implementa `CloneFor`.
3. Regístrala en `src/Ollin.Core/Scene/Component.cs` con `[JsonDerivedType(...)]`.
4. Si debe animarse, implementa también `ITickable`.
5. Exponla en el inspector (`EntityNode` + `MainWindow.axaml`).
6. Añade una prueba de serialización.

## Reportar errores

Incluye:

- Qué esperabas y qué ocurrió.
- Pasos para reproducirlo.
- Versión de Windows y de .NET (`dotnet --info`).
- Mensajes de la consola del editor y, si aplica, captura de pantalla.

## Licencia

Al contribuir aceptas que tu aportación se publique bajo la licencia [MIT](LICENSE).
