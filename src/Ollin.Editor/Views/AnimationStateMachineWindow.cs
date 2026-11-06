using System.Collections.ObjectModel;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Layout;
using Avalonia.Media;
using Ollin.Core.Model;
using Ollin.Core.Model.Components;

namespace Ollin.Editor.Views;

/// <summary>Editor visual mínimo de estados y transiciones de AnimationStateMachine.</summary>
public sealed class AnimationStateMachineWindow : Window
{
    private readonly AnimationStateMachine _draft;
    private readonly ObservableCollection<string> _labels = new();
    private readonly Canvas _canvas = new() { Height = 220, Background = new SolidColorBrush(Color.Parse("#0B0F18")) };
    private readonly TextBox _stateName = new() { Watermark = "idle / run / jump" };
    private readonly TextBox _from = new() { Watermark = "desde" };
    private readonly TextBox _to = new() { Watermark = "hacia" };
    private readonly TextBox _parameter = new() { Text = "speed" };
    private readonly NumericUpDown _threshold = new() { Minimum = -1000, Maximum = 1000, Increment = 0.1m, FormatString = "0.##" };
    private readonly ListBox _states;
    private int _selected;

    public AnimationStateMachineWindow(AnimationStateMachine machine)
    {
        _draft = (AnimationStateMachine)machine.CloneFor(new Entity());
        Title = "Editor FSM de animación"; Width = 680; Height = 620; MinWidth = 560; MinHeight = 480;
        WindowStartupLocation = WindowStartupLocation.CenterOwner;
        Background = new SolidColorBrush(Color.Parse("#121722")); Foreground = new SolidColorBrush(Color.Parse("#E6ECF7"));
        _states = new ListBox { ItemsSource = _labels, Height = 160 };
        _states.SelectionChanged += (_, _) => { _selected = _states.SelectedIndex; if (_selected >= 0 && _selected < _draft.States.Count) _stateName.Text = _draft.States[_selected].Name; Draw(); };

        var addState = new Button { Content = "+ Estado" };
        addState.Click += (_, _) => { _draft.States.Add(new AnimationClip2D { Name = $"state{_draft.States.Count + 1}" }); Rebuild(); Draw(); };
        var removeState = new Button { Content = "Quitar" };
        removeState.Click += (_, _) => { if (_selected >= 0 && _selected < _draft.States.Count) _draft.States.RemoveAt(_selected); Rebuild(); Draw(); };
        var addTransition = new Button { Content = "+ Transición" };
        addTransition.Click += (_, _) =>
        {
            if (string.IsNullOrWhiteSpace(_from.Text) || string.IsNullOrWhiteSpace(_to.Text)) return;
            _draft.Transitions.Add(new AnimationTransition2D
            {
                From = _from.Text!.Trim(), To = _to.Text!.Trim(),
                Parameter = string.IsNullOrWhiteSpace(_parameter.Text) ? "speed" : _parameter.Text!.Trim(),
                Threshold = (float)(_threshold.Value ?? 0m),
            });
            Draw();
        };
        var save = new Button { Content = "Guardar FSM", Background = new SolidColorBrush(Color.Parse("#F2A93B")), Foreground = new SolidColorBrush(Color.Parse("#1A1206")) };
        save.Click += (_, _) => { Apply(); Result = _draft; Close(true); };
        var cancel = new Button { Content = "Cancelar" };
        cancel.Click += (_, _) => Close(false);

        Content = new ScrollViewer
        {
            Content = new StackPanel
            {
                Margin = new Thickness(20), Spacing = 8,
                Children =
                {
                    new TextBlock { Text = "FSM de animación", FontSize = 20, FontWeight = FontWeight.Bold, Foreground = new SolidColorBrush(Color.Parse("#F2A93B")) },
                    new TextBlock { Text = "Estados y transiciones automáticas por parámetro.", FontSize = 11, Foreground = new SolidColorBrush(Color.Parse("#93A4BC")) },
                    _canvas,
                    new Grid
                    {
                        ColumnDefinitions = new ColumnDefinitions("220,*"),
                        Children =
                        {
                            new StackPanel { Spacing = 5, Children = { _states, new StackPanel { Orientation = Orientation.Horizontal, Spacing = 5, Children = { addState, removeState } } } },
                            new StackPanel { Margin = new Thickness(12, 0, 0, 0), Spacing = 5, Children = {
                                Row("Estado", _stateName), Row("Desde", _from), Row("Hacia", _to), Row("Parámetro", _parameter), Row("Umbral", _threshold), addTransition } },
                        },
                    },
                    new StackPanel { Orientation = Orientation.Horizontal, HorizontalAlignment = HorizontalAlignment.Right, Spacing = 8, Children = { cancel, save } },
                },
            },
        };

        Rebuild();
        Draw();
    }

    public AnimationStateMachine? Result { get; private set; }

    private static Control Row(string label, Control field)
    {
        var grid = new Grid { ColumnDefinitions = new ColumnDefinitions("86,*"), Margin = new Thickness(0, 1) };
        grid.Children.Add(new TextBlock { Text = label, Classes = { "hint" }, VerticalAlignment = VerticalAlignment.Center });
        Grid.SetColumn(field, 1); grid.Children.Add(field); return grid;
    }

    private void Rebuild()
    {
        _labels.Clear();
        foreach (var state in _draft.States) _labels.Add(state.Name);
        if (_labels.Count > 0 && _selected >= _labels.Count) _selected = _labels.Count - 1;
        _states.SelectedIndex = _selected;
    }

    private void Draw()
    {
        _canvas.Children.Clear();
        for (var i = 0; i < _draft.States.Count; i++)
        {
            var state = _draft.States[i];
            var border = new Border
            {
                Width = 130, Height = 44, CornerRadius = new CornerRadius(6), Padding = new Thickness(8),
                Background = new SolidColorBrush(i == _selected ? Color.Parse("#3A2A12") : Color.Parse("#151922")),
                BorderBrush = new SolidColorBrush(i == _selected ? Color.Parse("#F2A93B") : Color.Parse("#2B3342")),
                BorderThickness = new Thickness(1),
                Child = new TextBlock { Text = state.Name, Foreground = new SolidColorBrush(Color.Parse("#E7EAF0")), VerticalAlignment = VerticalAlignment.Center },
            };
            Canvas.SetLeft(border, 20 + i % 3 * 160);
            Canvas.SetTop(border, 20 + i / 3 * 70);
            _canvas.Children.Add(border);
        }

        var transitions = string.Join("  ·  ", _draft.Transitions.Select(item => $"{item.From} → {item.To} ({item.Parameter} ≥ {item.Threshold:0.##})"));
        _canvas.Children.Add(new TextBlock
        {
            Text = string.IsNullOrWhiteSpace(transitions) ? "Sin transiciones" : transitions,
            Foreground = new SolidColorBrush(Color.Parse("#93A4BC")), FontSize = 10,
        });
        Canvas.SetLeft(_canvas.Children[^1], 20); Canvas.SetTop(_canvas.Children[^1], 190);
    }

    private void Apply()
    {
        if (_selected >= 0 && _selected < _draft.States.Count && !string.IsNullOrWhiteSpace(_stateName.Text))
            _draft.States[_selected].Name = _stateName.Text!.Trim();
    }
}
