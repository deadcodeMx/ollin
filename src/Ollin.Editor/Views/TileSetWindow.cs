using System.Collections.ObjectModel;
using System.Numerics;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Layout;
using Avalonia.Media;
using Avalonia.Platform.Storage;
using Ollin.Core.Model.Components;
using Ollin.Core.Serialization;

namespace Ollin.Editor.Views;

/// <summary>Editor visual de un TileSet: atlas, tiles y colisión por tile.</summary>
public sealed class TileSetWindow : Window
{
    private readonly TileSet _draft;
    private readonly ObservableCollection<string> _labels = new();
    private readonly ListBox _list;
    private readonly TextBox _name;
    private readonly TextBox _texture;
    private readonly NumericUpDown _width;
    private readonly NumericUpDown _height;
    private readonly NumericUpDown _columns;
    private readonly NumericUpDown _tileId;
    private readonly TextBox _tileName;
    private readonly NumericUpDown _collisionWidth;
    private readonly NumericUpDown _collisionHeight;
    private readonly NumericUpDown _offsetX;
    private readonly NumericUpDown _offsetY;
    private readonly TextBox _terrain;
    private readonly TextBox _variants;
    private readonly NumericUpDown _navigationCost;
    private readonly CheckBox _occludes;
    private readonly CheckBox _oneWay;
    private readonly NumericUpDown _slopeX;
    private readonly NumericUpDown _slopeY;
    private int _selected = -1;

    public TileSetWindow(TileSet tileSet)
    {
        _draft = tileSet.Clone();
        Title = "Editor de TileSet";
        Width = 760;
        Height = 620;
        MinWidth = 620;
        MinHeight = 480;
        WindowStartupLocation = WindowStartupLocation.CenterOwner;
        Background = new SolidColorBrush(Color.Parse("#151922"));
        Foreground = new SolidColorBrush(Color.Parse("#E7EAF0"));

        _name = new TextBox { Text = _draft.Name };
        _texture = new TextBox { Text = _draft.TexturePath ?? string.Empty, Watermark = "assets/sprites/tiles.png" };
        _width = new NumericUpDown { Value = _draft.TileWidth, Minimum = 1, Maximum = 2048, Increment = 1, FormatString = "0" };
        _height = new NumericUpDown { Value = _draft.TileHeight, Minimum = 1, Maximum = 2048, Increment = 1, FormatString = "0" };
        _columns = new NumericUpDown { Value = _draft.Columns, Minimum = 1, Maximum = 128, Increment = 1, FormatString = "0" };

        _tileId = new NumericUpDown { Minimum = 0, Maximum = 4096, Increment = 1, FormatString = "0" };
        _tileName = new TextBox { Watermark = "nombre del tile" };
        _collisionWidth = new NumericUpDown { Minimum = 0, Maximum = 64, Increment = 0.1m, FormatString = "0.##" };
        _collisionHeight = new NumericUpDown { Minimum = 0, Maximum = 64, Increment = 0.1m, FormatString = "0.##" };
        _offsetX = new NumericUpDown { Minimum = -64, Maximum = 64, Increment = 0.05m, FormatString = "0.##" };
        _offsetY = new NumericUpDown { Minimum = -64, Maximum = 64, Increment = 0.05m, FormatString = "0.##" };
        _terrain = new TextBox { Watermark = "pasto, piedra..." };
        _variants = new TextBox { Watermark = "0,1,2,... 16 ids" };
        _navigationCost = new NumericUpDown { Minimum = -1, Maximum = 999, Increment = 1, FormatString = "0" };
        _occludes = new CheckBox { Content = "Proyecta sombra" };
        _oneWay = new CheckBox { Content = "One-way" };
        _slopeX = new NumericUpDown { Minimum = -1, Maximum = 1, Increment = 0.05m, FormatString = "0.##" };
        _slopeY = new NumericUpDown { Minimum = -1, Maximum = 1, Increment = 0.05m, FormatString = "0.##" };

        _list = new ListBox { ItemsSource = _labels, Height = 320 };
        _list.SelectionChanged += (_, _) => SelectTile(_list.SelectedIndex);

        var add = new Button { Content = "+ Tile" };
        add.Click += (_, _) => AddTile();
        var remove = new Button { Content = "Quitar" };
        remove.Click += (_, _) => RemoveTile();
        var load = new Button { Content = "Cargar JSON…" };
        load.Click += async (_, _) => await LoadJson();
        var saveFile = new Button { Content = "Guardar JSON…" };
        saveFile.Click += async (_, _) => await SaveJson();
        var save = new Button
        {
            Content = "Guardar TileSet",
            Background = new SolidColorBrush(Color.Parse("#E9A23B")),
            Foreground = new SolidColorBrush(Color.Parse("#1A1206")),
        };
        save.Click += (_, _) => { ApplyFields(); Close(true); };
        var cancel = new Button { Content = "Cancelar" };
        cancel.Click += (_, _) => Close(false);

        var fields = new StackPanel
        {
            Spacing = 5,
            Children =
            {
                Row("Id", _tileId), Row("Nombre", _tileName),
                Row("Colisión X", _collisionWidth), Row("Colisión Y", _collisionHeight),
                Row("Offset X", _offsetX), Row("Offset Y", _offsetY),
                Row("Terreno", _terrain),
                Row("Variantes", _variants),
                Row("Coste nav.", _navigationCost),
                _occludes,
                _oneWay,
                Row("Pendiente X", _slopeX), Row("Pendiente Y", _slopeY),
            },
        };

        Content = new StackPanel
        {
            Margin = new Thickness(20),
            Spacing = 10,
            Children =
            {
                new TextBlock { Text = "Editor de TileSet", FontSize = 20, FontWeight = FontWeight.Bold, Foreground = new SolidColorBrush(Color.Parse("#E9A23B")) },
                Row("Nombre", _name),
                Row("Textura", _texture),
                new Grid
                {
                    ColumnDefinitions = new ColumnDefinitions("*,*,*"),
                    Children = { Row("Tile W", _width), Place(Row("Tile H", _height), 1), Place(Row("Columnas", _columns), 2) },
                },
                new Grid
                {
                    ColumnDefinitions = new ColumnDefinitions("220,*"),
                    Children =
                    {
                        new StackPanel
                        {
                            Spacing = 6,
                            Children = { _list, new StackPanel { Orientation = Orientation.Horizontal, Spacing = 6, Children = { add, remove } } },
                        },
                        Place(new Border { Margin = new Thickness(14, 0, 0, 0), Child = fields }, 1),
                    },
                },
                new StackPanel { Orientation = Orientation.Horizontal, Spacing = 8, HorizontalAlignment = HorizontalAlignment.Right, Children = { load, saveFile, cancel, save } },
            },
        };

        RebuildLabels();
        if (_labels.Count > 0) _list.SelectedIndex = 0;
    }

    public TileSet? Result { get; private set; }

    private static Control Row(string label, Control field)
    {
        var grid = new Grid { ColumnDefinitions = new ColumnDefinitions("96,*"), Margin = new Thickness(0, 1) };
        grid.Children.Add(new TextBlock { Text = label, Classes = { "hint" }, VerticalAlignment = VerticalAlignment.Center });
        Grid.SetColumn(field, 1);
        grid.Children.Add(field);
        return grid;
    }

    private static Control Place(Control control, int column)
    {
        Grid.SetColumn(control, column);
        return control;
    }

    private void RebuildLabels()
    {
        _labels.Clear();
        foreach (var tile in _draft.Tiles.OrderBy(tile => tile.Id))
            _labels.Add($"{tile.Id}: {tile.Name}");
    }

    private void SelectTile(int index)
    {
        if (index < 0 || index >= _draft.Tiles.Count) return;
        _selected = index;
        var tile = _draft.Tiles.OrderBy(tile => tile.Id).ElementAt(index);
        _tileId.Value = tile.Id;
        _tileName.Text = tile.Name;
        _collisionWidth.Value = (decimal)tile.CollisionSize.X;
        _collisionHeight.Value = (decimal)tile.CollisionSize.Y;
        _offsetX.Value = (decimal)tile.CollisionOffset.X;
        _offsetY.Value = (decimal)tile.CollisionOffset.Y;
        _oneWay.IsChecked = tile.OneWay;
        _terrain.Text = tile.Terrain;
        _variants.Text = string.Join(", ", tile.AutotileVariants);
        _navigationCost.Value = tile.NavigationCost;
        _occludes.IsChecked = tile.Occludes;
        _slopeX.Value = (decimal)tile.SlopeNormal.X;
        _slopeY.Value = (decimal)tile.SlopeNormal.Y;
    }

    private void AddTile()
    {
        ApplyFields();
        var id = _draft.Tiles.Count == 0 ? 0 : _draft.Tiles.Max(tile => tile.Id) + 1;
        _draft.Tiles.Add(new TileDefinition { Id = id, Name = $"Tile {id}" });
        RebuildLabels();
        _list.SelectedIndex = _labels.Count - 1;
    }

    private void RemoveTile()
    {
        var ordered = _draft.Tiles.OrderBy(tile => tile.Id).ToList();
        if (_selected < 0 || _selected >= ordered.Count) return;
        _draft.Tiles.Remove(ordered[_selected]);
        RebuildLabels();
        _list.SelectedIndex = Math.Min(_selected, _labels.Count - 1);
    }

    private void ApplyFields()
    {
        if (_selected < 0 || _selected >= _draft.Tiles.Count) return;
        var ordered = _draft.Tiles.OrderBy(tile => tile.Id).ToList();
        var tile = ordered[_selected];
        tile.Id = (int)(_tileId.Value ?? 0);
        tile.Name = _tileName.Text ?? string.Empty;
        tile.CollisionSize = new Vector2((float)(_collisionWidth.Value ?? 0), (float)(_collisionHeight.Value ?? 0));
        tile.CollisionOffset = new Vector2((float)(_offsetX.Value ?? 0), (float)(_offsetY.Value ?? 0));
        tile.OneWay = _oneWay.IsChecked == true;
        tile.Terrain = _terrain.Text ?? string.Empty;
        tile.AutotileVariants = (_variants.Text ?? string.Empty).Split(',', StringSplitOptions.RemoveEmptyEntries).Select(value => int.TryParse(value.Trim(), out var id) ? id : -1).Where(id => id >= 0).ToList();
        tile.NavigationCost = (int)(_navigationCost.Value ?? 0);
        tile.Occludes = _occludes.IsChecked == true;
        tile.SlopeNormal = new Vector2((float)(_slopeX.Value ?? 0), (float)(_slopeY.Value ?? 0));
        _draft.Name = _name.Text ?? "TileSet";
        _draft.TexturePath = string.IsNullOrWhiteSpace(_texture.Text) ? null : _texture.Text;
        _draft.TileWidth = (int)(_width.Value ?? 32);
        _draft.TileHeight = (int)(_height.Value ?? 32);
        _draft.Columns = (int)(_columns.Value ?? 4);
        RebuildLabels();
        Result = _draft;
    }

    private async Task LoadJson()
    {
        var files = await StorageProvider.OpenFilePickerAsync(new FilePickerOpenOptions { Title = "Cargar TileSet", AllowMultiple = false, FileTypeFilter = new[] { new FilePickerFileType("TileSet") { Patterns = new[] { "*.json" } } } });
        var path = files.Count > 0 ? files[0].TryGetLocalPath() : null;
        if (string.IsNullOrWhiteSpace(path)) return;
        try
        {
            var loaded = TileSetSerializer.Load(path);
            _draft.Tiles = loaded.Tiles;
            _draft.TexturePath = loaded.TexturePath;
            _draft.TileWidth = loaded.TileWidth;
            _draft.TileHeight = loaded.TileHeight;
            _draft.Columns = loaded.Columns;
            _draft.Name = loaded.Name;
            _texture.Text = loaded.TexturePath ?? string.Empty;
            _name.Text = loaded.Name;
            _width.Value = loaded.TileWidth;
            _height.Value = loaded.TileHeight;
            _columns.Value = loaded.Columns;
            RebuildLabels();
            if (_labels.Count > 0) _list.SelectedIndex = 0;
        }
        catch { }
    }

    private async Task SaveJson()
    {
        ApplyFields();
        var file = await StorageProvider.SaveFilePickerAsync(new FilePickerSaveOptions { Title = "Guardar TileSet", SuggestedFileName = $"{_draft.Name}.tileset.json", DefaultExtension = "json", FileTypeChoices = new[] { new FilePickerFileType("TileSet") { Patterns = new[] { "*.json" } } } });
        var path = file?.TryGetLocalPath();
        if (string.IsNullOrWhiteSpace(path)) return;
        TileSetSerializer.Save(_draft, path);
    }
}
