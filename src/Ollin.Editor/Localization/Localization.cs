using Avalonia.Controls;
using Avalonia.VisualTree;

namespace Ollin.Editor.Localization;

public enum UiLanguage
{
    Spanish,
    English,
}

/// <summary>Traduce textos conocidos de la interfaz. El español es el idioma fuente.</summary>
public static class Localization
{
    private static readonly Dictionary<string, string> English = new(StringComparer.Ordinal)
    {
        ["Archivo"] = "File",
        ["Editar"] = "Edit",
        ["Ver"] = "View",
        ["Escena"] = "Scene",
        ["Ayuda"] = "Help",
        ["Nuevo proyecto…"] = "New project…",
        ["Abrir proyecto…"] = "Open project…",
        ["Cerrar proyecto"] = "Close project",
        ["Guardar escena"] = "Save scene",
        ["Abrir escena…"] = "Open scene…",
        ["Atajos del teclado…"] = "Keyboard shortcuts…",
        ["Mapa de entrada…"] = "Input map…",
        ["Tema"] = "Theme",
        ["Oscuro"] = "Dark",
        ["Claro"] = "Light",
        ["Idioma"] = "Language",
        ["Español (oficial)"] = "Spanish (official)",
        ["Scripts"] = "Scripts",
        ["Grafo"] = "Graph",
        ["Mover"] = "Move",
        ["Rotar"] = "Rotate",
        ["Escalar"] = "Scale",
        ["Cámara"] = "Camera",
        ["Reproducir"] = "Play",
        ["Pausa"] = "Pause",
        ["Detener"] = "Stop",
        ["Exportar"] = "Export",
        ["Jerarquía"] = "Hierarchy",
        ["Archivos"] = "Files",
        ["Assets"] = "Assets",
        ["Consola"] = "Console",
        ["Inspector"] = "Inspector",
        ["Entidad"] = "Entity",
        ["Añadir componente"] = "Add component",
        ["Crear hijo"] = "Create child",
        ["Duplicar"] = "Duplicate",
        ["Eliminar"] = "Delete",
        ["Guardar como prefab…"] = "Save as prefab…",
        ["Guardar como blueprint…"] = "Save as blueprint…",
        ["Buscar entidad…"] = "Search entity…",
        ["Buscar en esta carpeta…"] = "Search this folder…",
        ["Entidad vacía"] = "Empty entity",
        ["Sprite 2D"] = "2D Sprite",
        ["Hijo de la selección"] = "Child of selection",
        ["Movimiento"] = "Movement",
        ["Mundo"] = "World",
        ["Scripts"] = "Scripts",
        ["Plataforma móvil"] = "Moving platform",
        ["Muelle (joint)"] = "Spring joint",
        ["Pin (joint)"] = "Pin joint",
        ["Máquina de animación"] = "Animation state machine",
        ["Esqueleto 2D"] = "2D Skeleton",
        ["Canvas de UI"] = "UI Canvas",
        ["Interactuable"] = "Interactable",
        ["Recogible"] = "Pickup",
        ["Puerta"] = "Door",
        ["Salud"] = "Health",
        ["Arma"] = "Weapon",
        ["Colisión"] = "Collision",
        ["Visual"] = "Visual",
        ["Caja"] = "Box",
        ["Círculo"] = "Circle",
        ["Cilindro"] = "Cylinder",
        ["Cono"] = "Cone",
        ["Cápsula"] = "Capsule",
        ["Disparador"] = "Trigger",
        ["Personaje 2D"] = "2D Character",
        ["Cuerpo físico"] = "Rigidbody",
        ["Cámara 2D"] = "2D Camera",
        ["Rotador"] = "Rotator",
        ["Animación"] = "Animation",
        ["Luz 2D"] = "2D Light",
        ["Partículas"] = "Particles",
        ["Script C#"] = "C# Script",
        ["Script C#…"] = "C# Script…",
        ["Crear script de personaje"] = "Create character script",
        ["Abrir en ventana"] = "Open in window",
        ["Actualizar cámaras"] = "Refresh cameras",
        ["Sin cámara 2D activa"] = "No active 2D camera",
        ["VISTA DE CÁMARA"] = "CAMERA VIEW",
        ["Guardar"] = "Save",
        ["Cancelar"] = "Cancel",
        ["Cerrar"] = "Close",
        ["Abrir"] = "Open",
        ["Añadir"] = "Add",
        ["Quitar"] = "Remove",
        ["Recompilar scripts"] = "Reload scripts",
        ["Ventana de juego"] = "Game window",
        ["Centrar cámara"] = "Center camera",
        ["Enfocar selección"] = "Focus selection",
        ["Mostrar cuadrícula"] = "Show grid",
        ["Ajustes"] = "Settings",
        ["Proyecto"] = "Project",
        ["Nuevo"] = "New",
        ["Guardar partida"] = "Save game",
        ["Cargar partida"] = "Load game",
    };

    public static UiLanguage Current { get; private set; } = UiLanguage.Spanish;

    public static string Normalize(string? value) => value?.Equals("en", StringComparison.OrdinalIgnoreCase) == true ? "en" : "es";

    public static void Apply(Control root, string? language)
    {
        Current = Normalize(language) == "en" ? UiLanguage.English : UiLanguage.Spanish;
        foreach (var control in Descendants(root))
        {
            switch (control)
            {
                case TextBlock text when !string.IsNullOrWhiteSpace(text.Text):
                    text.Text = Translate(text.Text!, Current);
                    break;
                case Window window when !string.IsNullOrWhiteSpace(window.Title):
                    window.Title = Translate(window.Title!, Current);
                    break;
                case MenuItem menu when menu.Header is string header:
                    menu.Header = Translate(header, Current);
                    break;
                case TabItem tab when tab.Header is string tabHeader:
                    tab.Header = Translate(tabHeader, Current);
                    break;
                case Button button when button.Content is string content:
                    button.Content = Translate(content, Current);
                    break;
                case CheckBox check when check.Content is string checkContent:
                    check.Content = Translate(checkContent, Current);
                    break;
                case ComboBoxItem item when item.Content is string itemContent:
                    item.Content = Translate(itemContent, Current);
                    break;
            }

            if (control is Control c && ToolTip.GetTip(c) is string tip)
                ToolTip.SetTip(c, Translate(tip, Current));
        }
    }

    public static string Translate(string text, UiLanguage language)
    {
        if (language == UiLanguage.Spanish)
        {
            foreach (var pair in English)
                if (pair.Value == text) return pair.Key;
            return text;
        }

        return English.TryGetValue(text, out var translated) ? translated : text;
    }

    private static IEnumerable<Control> Descendants(Control root)
    {
        var stack = new Stack<Control>();
        stack.Push(root);
        while (stack.Count > 0)
        {
            var control = stack.Pop();
            yield return control;
            foreach (var child in control.GetVisualChildren().OfType<Control>()) stack.Push(child);
        }
    }
}