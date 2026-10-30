using System.Numerics;
using System.Text.Json.Serialization;
using Ollin.Core.Scenes;

namespace Ollin.Core.Model;

/// <summary>
/// Objeto de la escena. Tiene identidad estable (GUID), nombre visible, jerarquía
/// opcional mediante <see cref="ParentId"/> y una lista de componentes.
/// </summary>
public sealed class Entity
{
    public Guid Id { get; set; } = Guid.NewGuid();

    public string Name { get; set; } = "Entidad";

    public Guid? ParentId { get; set; }

    public bool Active { get; set; } = true;

    public Transform2D Transform { get; set; } = new();

    public List<Component> Components { get; set; } = new();

    /// <summary>Escena a la que pertenece (no se serializa).</summary>
    [JsonIgnore]
    public Scene? Scene { get; internal set; }

    [JsonIgnore]
    public Vector2 Position
    {
        get => Transform.Position;
        set => Transform.Position = value;
    }

    /// <summary>Devuelve el primer componente del tipo pedido, o null.</summary>
    public T? GetComponent<T>() where T : Component
    {
        foreach (var component in Components)
        {
            if (component is T typed)
            {
                return typed;
            }
        }

        return null;
    }

    public bool HasComponent<T>() where T : Component => GetComponent<T>() is not null;

    public T AddComponent<T>(T component) where T : Component
    {
        component.Owner = this;
        Components.Add(component);
        return component;
    }

    public T AddComponent<T>() where T : Component, new() => AddComponent(new T());

    public bool RemoveComponent(Component component)
    {
        if (Components.Remove(component))
        {
            component.Owner = null;
            return true;
        }

        return false;
    }

    public Entity Clone(bool newId = true)
    {
        var copy = new Entity
        {
            Id = newId ? Guid.NewGuid() : Id,
            Name = Name,
            ParentId = ParentId,
            Active = Active,
            Transform = Transform.Clone(),
        };

        foreach (var component in Components)
        {
            copy.Components.Add(component.CloneFor(copy));
        }

        return copy;
    }

    public override string ToString() => $"{Name} ({Id.ToString()[..8]})";
}
