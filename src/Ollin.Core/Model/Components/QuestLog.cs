using System.Text.Json.Serialization;

namespace Ollin.Core.Model.Components;

/// <summary>Equipo por ranuras (arma, armadura, accesorio) basado en ItemId.</summary>
public sealed class Equipment : Component
{
    public Dictionary<string, string> Slots { get; set; } = new(StringComparer.OrdinalIgnoreCase);

    public bool Equip(string slot, string itemId)
    {
        if (string.IsNullOrWhiteSpace(slot) || string.IsNullOrWhiteSpace(itemId)) return false;
        Slots[slot] = itemId;
        return true;
    }

    public void Unequip(string slot) => Slots.Remove(slot);

    public string? Get(string slot) =>
        Slots.TryGetValue(slot, out var item) ? item : null;

    public bool Has(string slot, string itemId) =>
        string.Equals(Get(slot), itemId, StringComparison.OrdinalIgnoreCase);

    public override Component CloneFor(Entity newOwner) => new Equipment
    {
        Enabled = Enabled,
        Slots = new Dictionary<string, string>(Slots, StringComparer.OrdinalIgnoreCase),
        Owner = newOwner,
    };
}

/// <summary>Objetivo de una misión.</summary>
public sealed class QuestObjective
{
    public string Id { get; set; } = "objective";
    public string Text { get; set; } = string.Empty;
    public int Required { get; set; } = 1;
    public int Progress { get; set; }
    [JsonIgnore] public bool Completed => Progress >= Required;
}

/// <summary>Misión activa en el registro del jugador.</summary>
public sealed class QuestState
{
    public string Id { get; set; } = string.Empty;
    public string Title { get; set; } = string.Empty;
    public List<QuestObjective> Objectives { get; set; } = new();
    [JsonIgnore] public bool Completed => Objectives.Count > 0 && Objectives.All(objective => objective.Completed);
}

/// <summary>Registro de misiones del jugador.</summary>
public sealed class QuestLog : Component
{
    public List<QuestState> Quests { get; set; } = new();

    public QuestState? Start(string id, string title, IEnumerable<(string Id, string Text, int Required)>? objectives = null)
    {
        if (string.IsNullOrWhiteSpace(id)) return null;
        var existing = Quests.FirstOrDefault(quest => quest.Id.Equals(id, StringComparison.OrdinalIgnoreCase));
        if (existing is not null) return existing;
        var quest = new QuestState { Id = id, Title = title };
        if (objectives is not null)
            foreach (var (objectiveId, text, required) in objectives)
                quest.Objectives.Add(new QuestObjective { Id = objectiveId, Text = text, Required = System.Math.Max(1, required) });
        Quests.Add(quest);
        RefreshHud();
        return quest;
    }

    public bool Advance(string questId, string objectiveId, int amount = 1)
    {
        var quest = Quests.FirstOrDefault(item => item.Id.Equals(questId, StringComparison.OrdinalIgnoreCase));
        var objective = quest?.Objectives.FirstOrDefault(item => item.Id.Equals(objectiveId, StringComparison.OrdinalIgnoreCase));
        if (objective is null) return false;
        objective.Progress = System.Math.Clamp(objective.Progress + amount, 0, objective.Required);
        RefreshHud();
        return true;
    }

    public bool Complete(string questId)
    {
        var quest = Quests.FirstOrDefault(item => item.Id.Equals(questId, StringComparison.OrdinalIgnoreCase));
        if (quest is null) return false;
        foreach (var objective in quest.Objectives) objective.Progress = objective.Required;
        RefreshHud();
        return true;
    }

    public void RefreshHud()
    {
        var active = Quests.FirstOrDefault(quest => !quest.Completed);
        if (active is null) { Hud.HudHost.SetLabel("Objetivo", string.Empty); return; }
        var objective = active.Objectives.FirstOrDefault(item => !item.Completed);
        var text = objective is null ? active.Title : $"{active.Title}: {objective.Text} ({objective.Progress}/{objective.Required})";
        Hud.HudHost.SetLabel("Objetivo", text);
    }

    public override Component CloneFor(Entity newOwner) => new QuestLog
    {
        Enabled = Enabled,
        Quests = Quests.Select(quest => new QuestState
        {
            Id = quest.Id, Title = quest.Title,
            Objectives = quest.Objectives.Select(objective => new QuestObjective
            {
                Id = objective.Id, Text = objective.Text, Required = objective.Required, Progress = objective.Progress,
            }).ToList(),
        }).ToList(),
        Owner = newOwner,
    };
}
