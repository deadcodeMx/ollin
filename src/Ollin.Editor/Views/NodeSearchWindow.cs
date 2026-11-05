using Avalonia;
using Avalonia.Controls;
using Avalonia.Layout;
using Avalonia.Media;

namespace Ollin.Editor.Views;

/// <summary>Buscador de nodos del grafo, estilo paleta contextual.</summary>
public sealed class NodeSearchWindow : Window
{
    private readonly List<(string Label, Action Action)> _all;
    private readonly ListBox _list = new();
    private readonly TextBox _search;

    public NodeSearchWindow(IEnumerable<(string Label, Action Action)> choices)
    {
        _all = choices.ToList();
        Title = "Añadir nodo";
        Width = 520;
        Height = 560;
        WindowStartupLocation = WindowStartupLocation.CenterOwner;
        Background = new SolidColorBrush(Color.Parse("#121722"));
        Foreground = new SolidColorBrush(Color.Parse("#E6ECF7"));

        _search = new TextBox { Watermark = "Buscar nodo…", Margin = new Thickness(0, 0, 0, 8) };
        _search.TextChanged += (_, _) => Filter();

        _list.Height = 400;
        _list.DoubleTapped += (_, _) => Accept();

        var add = new Button { Content = "Añadir nodo", Classes = { "accent" } };
        add.Click += (_, _) => Accept();
        var cancel = new Button { Content = "Cancelar" };
        cancel.Click += (_, _) => Close();

        var buttons = new StackPanel { Orientation = Orientation.Horizontal, HorizontalAlignment = HorizontalAlignment.Right, Spacing = 8, Children = { cancel, add } };
        Content = new StackPanel
        {
            Margin = new Thickness(18), Spacing = 8,
            Children =
            {
                new TextBlock { Text = "Añadir nodo", FontSize = 20, FontWeight = FontWeight.Bold, Foreground = new SolidColorBrush(Color.Parse("#F2A93B")) },
                new TextBlock { Text = "Escribe para filtrar. Enter añade el primer resultado.", FontSize = 11, Foreground = new SolidColorBrush(Color.Parse("#93A4BC")) },
                _search, _list, buttons,
            },
        };
        Opened += (_, _) => _search.Focus();
        KeyDown += (_, e) =>
        {
            if (e.Key == Avalonia.Input.Key.Enter) { Accept(); e.Handled = true; }
            else if (e.Key == Avalonia.Input.Key.Escape) { Close(); e.Handled = true; }
        };
        Filter();
    }

    private void Filter()
    {
        var query = _search.Text?.Trim() ?? string.Empty;
        _list.ItemsSource = _all.Where(choice => query.Length == 0 || choice.Label.Contains(query, StringComparison.OrdinalIgnoreCase))
            .Select(choice => choice.Label).ToList();
        if (_list.ItemCount > 0) _list.SelectedIndex = 0;
    }

    private void Accept()
    {
        var query = _search.Text?.Trim() ?? string.Empty;
        var choice = _all.FirstOrDefault(item => query.Length == 0 || item.Label.Contains(query, StringComparison.OrdinalIgnoreCase));
        if (choice.Action is null) return;
        choice.Action();
        Close();
    }
}
