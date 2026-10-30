namespace Ollin.Core.Model.Components;

/// <summary>Una estadística configurable (vida, hambre, energía...).</summary>
public sealed class StatEntry
{
    public string Name { get; set; } = string.Empty;

    public float Value { get; set; } = 100f;

    public float Max { get; set; } = 100f;

    /// <summary>Puntos que pierde por segundo (0 = no cambia).</summary>
    public float DecayPerSecond { get; set; }
}

/// <summary>Conjunto de estadísticas de una entidad, con desgaste automático.</summary>
public sealed class Stats : Component, ITickable
{
    public List<StatEntry> Entries { get; set; } = new();

    public StatEntry? Find(string name) =>
        Entries.FirstOrDefault(entry => string.Equals(entry.Name, name, StringComparison.OrdinalIgnoreCase));

    public StatEntry Ensure(string name, float max = 100f)
    {
        var entry = Find(name);

        if (entry is null)
        {
            entry = new StatEntry { Name = name, Value = max, Max = max };
            Entries.Add(entry);
        }

        return entry;
    }

    public float Get(string name) => Find(name)?.Value ?? 0f;

    public void Set(string name, float value)
    {
        var entry = Ensure(name);
        entry.Value = System.Math.Clamp(value, 0f, System.Math.Max(0.01f, entry.Max));
    }

    public void Add(string name, float delta)
    {
        var entry = Ensure(name);
        entry.Value = System.Math.Clamp(entry.Value + delta, 0f, System.Math.Max(0.01f, entry.Max));
    }

    public void OnUpdate(float deltaTime)
    {
        foreach (var entry in Entries)
        {
            if (entry.DecayPerSecond <= 0f)
            {
                continue;
            }

            entry.Value = System.Math.Clamp(
                entry.Value - entry.DecayPerSecond * deltaTime,
                0f,
                System.Math.Max(0.01f, entry.Max));
        }
    }

    public override Component CloneFor(Entity newOwner) => new Stats
    {
        Enabled = Enabled,
        Entries = Entries.Select(entry => new StatEntry
        {
            Name = entry.Name,
            Value = entry.Value,
            Max = entry.Max,
            DecayPerSecond = entry.DecayPerSecond,
        }).ToList(),
        Owner = newOwner,
    };
}
