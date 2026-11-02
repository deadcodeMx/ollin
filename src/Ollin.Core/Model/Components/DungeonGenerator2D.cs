using System.Text.Json.Serialization;

namespace Ollin.Core.Model.Components;

/// <summary>Genera una mazmorra simple en el Tilemap de la misma entidad.</summary>
public sealed class DungeonGenerator2D : Component, ITickable
{
    public int Width { get; set; } = 32;
    public int Height { get; set; } = 20;
    public int Seed { get; set; } = 12345;
    public int FloorTile { get; set; } = 0;
    public int WallTile { get; set; } = 1;
    public bool GenerateOnStart { get; set; } = true;

    private bool _generated;

    public void OnUpdate(float deltaTime)
    {
        if (_generated || !GenerateOnStart) return;
        _generated = true;
        Generate();
    }

    public void Generate()
    {
        if (Owner?.GetComponent<Tilemap>() is not { } map) return;
        map.Resize(Width, Height);
        var random = new Random(Seed);
        for (var y = 0; y < Height; y++)
            for (var x = 0; x < Width; x++)
                map.SetTile(x, y, WallTile);

        var cx = Width / 2;
        var cy = Height / 2;
        var xPos = cx;
        var yPos = cy;
        var steps = Width * Height * 3;
        for (var i = 0; i < steps; i++)
        {
            map.SetTile(xPos, yPos, FloorTile);
            var direction = random.Next(4);
            if (direction == 0) xPos = System.Math.Clamp(xPos + 1, 1, Width - 2);
            else if (direction == 1) xPos = System.Math.Clamp(xPos - 1, 1, Width - 2);
            else if (direction == 2) yPos = System.Math.Clamp(yPos + 1, 1, Height - 2);
            else yPos = System.Math.Clamp(yPos - 1, 1, Height - 2);
        }
        map.Collides = true;
    }

    public override Component CloneFor(Entity newOwner) => new DungeonGenerator2D
    {
        Enabled = Enabled, Width = Width, Height = Height, Seed = Seed, FloorTile = FloorTile,
        WallTile = WallTile, GenerateOnStart = GenerateOnStart, Owner = newOwner,
    };
}
