using System.Text.Json.Serialization;

namespace Ollin.Core.Model.Components;

/// <summary>Carta de un mazo; el contenido se define por Id.</summary>
public sealed class CardDefinition
{
    public string Id { get; set; } = "card";
    public string Name { get; set; } = "Carta";
    public int Cost { get; set; } = 1;
    public string Description { get; set; } = string.Empty;
}

/// <summary>Mazo, mano y descarte con operaciones básicas.</summary>
public sealed class CardDeck : Component
{
    public int MaxHand { get; set; } = 5;
    public List<string> DrawPile { get; set; } = new();
    public List<string> Hand { get; set; } = new();
    public List<string> DiscardPile { get; set; } = new();

    public void Shuffle(int? seed = null)
    {
        var random = seed is { } value ? new Random(value) : Random.Shared;
        for (var i = DrawPile.Count - 1; i > 0; i--)
        {
            var j = random.Next(i + 1);
            (DrawPile[i], DrawPile[j]) = (DrawPile[j], DrawPile[i]);
        }
    }

    public int Draw(int count = 1)
    {
        var drawn = 0;
        for (var i = 0; i < count && Hand.Count < MaxHand; i++)
        {
            if (DrawPile.Count == 0) Reshuffle();
            if (DrawPile.Count == 0) break;
            Hand.Add(DrawPile[0]);
            DrawPile.RemoveAt(0);
            drawn++;
        }
        return drawn;
    }

    public bool Play(int index)
    {
        if (index < 0 || index >= Hand.Count) return false;
        DiscardPile.Add(Hand[index]);
        Hand.RemoveAt(index);
        return true;
    }

    public void Reshuffle()
    {
        DrawPile.AddRange(DiscardPile);
        DiscardPile.Clear();
        Shuffle();
    }

    public override Component CloneFor(Entity newOwner) => new CardDeck
    {
        Enabled = Enabled, MaxHand = MaxHand,
        DrawPile = new List<string>(DrawPile), Hand = new List<string>(Hand),
        DiscardPile = new List<string>(DiscardPile), Owner = newOwner,
    };
}
