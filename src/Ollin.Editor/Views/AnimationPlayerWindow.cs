using System.Collections.ObjectModel;
using System.Numerics;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Layout;
using Avalonia.Media;
using Ollin.Core.Model.Components;

namespace Ollin.Editor.Views;

/// <summary>Editor básico de clips, tracks y keyframes de AnimationPlayer2D.</summary>
public sealed class AnimationPlayerWindow : Window
{
    private readonly AnimationPlayer2D _draft;
    private readonly ObservableCollection<string> _clipLabels = new();
    private readonly ObservableCollection<string> _trackLabels = new();
    private readonly ObservableCollection<string> _keyLabels = new();
    private readonly ListBox _clips;
    private readonly ListBox _tracks;
    private readonly ListBox _keys;
    private readonly TextBox _clipName;
    private readonly NumericUpDown _duration;
    private readonly CheckBox _loop;
    private readonly TextBox _trackProperty;
    private readonly NumericUpDown _keyTime;
    private readonly NumericUpDown _keyX;
    private readonly NumericUpDown _keyY;
    private readonly NumericUpDown _keyNumber;
    private readonly TextBox _keyText;
    private readonly ComboBox _keyEasing;
    private int _clipIndex, _trackIndex, _keyIndex = -1;

    public AnimationPlayerWindow(AnimationPlayer2D player)
    {
        _draft = (AnimationPlayer2D)player.CloneFor(player.Owner!);
        Title = "Editor de animación";
        Width = 760; Height = 620; MinWidth = 620; MinHeight = 480;
        WindowStartupLocation = WindowStartupLocation.CenterOwner;
        Background = new SolidColorBrush(Color.Parse("#151922"));
        Foreground = new SolidColorBrush(Color.Parse("#E7EAF0"));

        _clips = new ListBox { ItemsSource = _clipLabels, Height = 120 };
        _clips.SelectionChanged += (_, _) => SelectClip(_clips.SelectedIndex);
        _tracks = new ListBox { ItemsSource = _trackLabels, Height = 120 };
        _tracks.SelectionChanged += (_, _) => SelectTrack(_tracks.SelectedIndex);
        _keys = new ListBox { ItemsSource = _keyLabels, Height = 120 };
        _keys.SelectionChanged += (_, _) => SelectKey(_keys.SelectedIndex);
        _clipName = new TextBox { Watermark = "idle" };
        _duration = new NumericUpDown { Minimum = 0.01m, Maximum = 600m, Increment = 0.1m, FormatString = "0.##" };
        _loop = new CheckBox { Content = "Loop" };
        _trackProperty = new TextBox { Watermark = "position, rotation, scale, frame, audio" };
        _keyTime = new NumericUpDown { Minimum = 0m, Maximum = 600m, Increment = 0.05m, FormatString = "0.##" };
        _keyX = new NumericUpDown { Minimum = -10000m, Maximum = 10000m, Increment = 0.1m, FormatString = "0.##" };
        _keyY = new NumericUpDown { Minimum = -10000m, Maximum = 10000m, Increment = 0.1m, FormatString = "0.##" };
        _keyNumber = new NumericUpDown { Minimum = -10000m, Maximum = 10000m, Increment = 0.1m, FormatString = "0.##" };
        _keyText = new TextBox { Watermark = "ruta de audio / evento" };
        _keyEasing = new ComboBox { ItemsSource = Enum.GetValues<AnimationEasing>(), SelectedIndex = 0 };

        var addClip = new Button { Content = "+ Clip" };
        addClip.Click += (_, _) => { _draft.Clips.Add(new AnimationTimeline { Name = $"clip{_draft.Clips.Count + 1}" }); RebuildClips(); _clips.SelectedIndex = _clipLabels.Count - 1; };
        var addTrack = new Button { Content = "+ Track" };
        addTrack.Click += (_, _) => AddTrack();
        var addKey = new Button { Content = "+ Key" };
        addKey.Click += (_, _) => AddKey();
        var remove = new Button { Content = "Quitar" };
        remove.Click += (_, _) => RemoveSelected();
        var save = new Button { Content = "Guardar animación", Background = new SolidColorBrush(Color.Parse("#E9A23B")), Foreground = new SolidColorBrush(Color.Parse("#1A1206")) };
        save.Click += (_, _) => { ApplyFields(); Close(true); };
        var cancel = new Button { Content = "Cancelar" };
        cancel.Click += (_, _) => Close(false);

        Content = new StackPanel
        {
            Margin = new Thickness(20), Spacing = 8,
            Children =
            {
                new TextBlock { Text = "Editor de animación", FontSize = 20, FontWeight = FontWeight.Bold, Foreground = new SolidColorBrush(Color.Parse("#E9A23B")) },
                new TextBlock { Text = "Clips → tracks → keyframes. Propiedades: position, rotation, scale, frame, audio.", FontSize = 11, Foreground = new SolidColorBrush(Color.Parse("#8B94A7")) },
                new Grid { ColumnDefinitions = new ColumnDefinitions("220,*"), Children = { new StackPanel { Spacing = 4, Children = { _clips, addClip } }, new StackPanel { Margin = new Thickness(12, 0, 0, 0), Spacing = 4, Children = { Row("Nombre", _clipName), Row("Duración", _duration), _loop } } } },
                new Grid { ColumnDefinitions = new ColumnDefinitions("220,*"), Children = { new StackPanel { Spacing = 4, Children = { _tracks, addTrack } }, new StackPanel { Margin = new Thickness(12, 0, 0, 0), Spacing = 4, Children = { Row("Propiedad", _trackProperty), new StackPanel { Orientation = Orientation.Horizontal, Spacing = 6, Children = { addKey, remove } } } } } },
                new Grid { ColumnDefinitions = new ColumnDefinitions("220,*"), Children = { _keys, new StackPanel { Margin = new Thickness(12, 0, 0, 0), Spacing = 4, Children = { Row("Tiempo", _keyTime), Row("Valor X", _keyX), Row("Valor Y", _keyY), Row("Número", _keyNumber), Row("Texto", _keyText), Row("Easing", _keyEasing) } } } },
                new StackPanel { Orientation = Orientation.Horizontal, HorizontalAlignment = HorizontalAlignment.Right, Spacing = 8, Children = { cancel, save } },
            },
        };

        RebuildClips();
        if (_clipLabels.Count > 0) _clips.SelectedIndex = 0;
    }

    public AnimationPlayer2D? Result { get; private set; }

    private static Control Row(string label, Control field)
    {
        var grid = new Grid { ColumnDefinitions = new ColumnDefinitions("86,*"), Margin = new Thickness(0, 1) };
        grid.Children.Add(new TextBlock { Text = label, Classes = { "hint" }, VerticalAlignment = VerticalAlignment.Center });
        Grid.SetColumn(field, 1);
        grid.Children.Add(field);
        return grid;
    }

    private AnimationTimeline? Clip => _clipIndex >= 0 && _clipIndex < _draft.Clips.Count ? _draft.Clips[_clipIndex] : null;
    private AnimationTrack? Track => Clip is { } clip && _trackIndex >= 0 && _trackIndex < clip.Tracks.Count ? clip.Tracks[_trackIndex] : null;
    private AnimationKeyframe? Key => Track is { } track && _keyIndex >= 0 && _keyIndex < track.Keys.Count ? track.Keys[_keyIndex] : null;

    private void RebuildClips()
    {
        _clipLabels.Clear();
        foreach (var clip in _draft.Clips) _clipLabels.Add(clip.Name);
    }

    private void RebuildTracks()
    {
        _trackLabels.Clear();
        if (Clip is { } clip) foreach (var track in clip.Tracks) _trackLabels.Add(track.Property);
    }

    private void RebuildKeys()
    {
        _keyLabels.Clear();
        if (Track is { } track) foreach (var key in track.Keys) _keyLabels.Add($"{key.Time:0.##}s  {key.Value.X:0.##},{key.Value.Y:0.##}");
    }

    private void SelectClip(int index)
    {
        ApplyFields();
        _clipIndex = index;
        _trackIndex = 0;
        if (Clip is { } clip)
        {
            _clipName.Text = clip.Name; _duration.Value = (decimal)clip.Duration; _loop.IsChecked = clip.Loop;
        }
        RebuildTracks();
        if (_trackLabels.Count > 0) _tracks.SelectedIndex = 0;
    }

    private void SelectTrack(int index)
    {
        _trackIndex = index;
        if (Track is { } track) _trackProperty.Text = track.Property;
        RebuildKeys();
        if (_keyLabels.Count > 0) _keys.SelectedIndex = 0;
        _keyIndex = _keys.SelectedIndex;
    }

    private void SelectKey(int index)
    {
        _keyIndex = index;
        if (Key is not { } key) return;
        _keyTime.Value = (decimal)key.Time;
        _keyX.Value = (decimal)key.Value.X;
        _keyY.Value = (decimal)key.Value.Y;
        _keyNumber.Value = (decimal)key.Number;
        _keyText.Text = key.Text ?? string.Empty;
        _keyEasing.SelectedItem = key.Easing;
    }

    private void AddTrack()
    {
        if (Clip is not { } clip) return;
        clip.Tracks.Add(new AnimationTrack { Property = "position" });
        RebuildTracks();
        _tracks.SelectedIndex = _trackLabels.Count - 1;
    }

    private void AddKey()
    {
        if (Track is not { } track) return;
        track.Keys.Add(new AnimationKeyframe());
        RebuildKeys();
        _keys.SelectedIndex = _keyLabels.Count - 1;
    }

    private void RemoveSelected()
    {
        if (Track is { } track && _keyIndex >= 0 && _keyIndex < track.Keys.Count)
        {
            track.Keys.RemoveAt(_keyIndex);
            RebuildKeys();
            if (_keyLabels.Count > 0) _keys.SelectedIndex = 0;
        }
    }

    private void ApplyFields()
    {
        if (Clip is { } clip)
        {
            clip.Name = _clipName.Text ?? "clip";
            clip.Duration = MathF.Max(0.01f, (float)(_duration.Value ?? 1m));
            clip.Loop = _loop.IsChecked == true;
        }
        if (Track is { } track) track.Property = _trackProperty.Text ?? "position";
        if (Key is { } key)
        {
            key.Time = MathF.Max(0f, (float)(_keyTime.Value ?? 0m));
            key.Value = new Vector2((float)(_keyX.Value ?? 0m), (float)(_keyY.Value ?? 0m));
            key.Number = (float)(_keyNumber.Value ?? 0m);
            key.Text = string.IsNullOrWhiteSpace(_keyText.Text) ? null : _keyText.Text;
            if (_keyEasing.SelectedItem is AnimationEasing easing) key.Easing = easing;
        }
        RebuildClips();
        RebuildTracks();
        RebuildKeys();
        Result = _draft;
    }
}
