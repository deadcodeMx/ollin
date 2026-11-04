using System.Numerics;
using Ollin.Core.Model;
using Ollin.Core.Model.Components;

namespace Ollin.Core.Scenes;

/// <summary>
/// Contenedor de entidades. Mantiene la jerarquía por <see cref="Entity.ParentId"/>
/// y el grafo de componentes. Es serializable a JSON con
/// <see cref="SceneSerializer"/>.
/// </summary>
public sealed class Scene
{
    public string Name { get; set; } = "Escena sin título";

    public List<Entity> Entities { get; set; } = new();

    /// <summary>Entidades sin padre.</summary>
    public IEnumerable<Entity> RootEntities =>
        Entities.Where(e => e.ParentId is null || Find(e.ParentId.Value) is null);

    public IEnumerable<Entity> ChildrenOf(Guid parentId) =>
        Entities.Where(e => e.ParentId == parentId);

    public Entity? Find(Guid id)
    {
        foreach (var entity in Entities)
        {
            if (entity.Id == id)
            {
                return entity;
            }
        }

        return null;
    }

    public Entity? Find(string name) => Entities.FirstOrDefault(e => e.Name == name);

    public Entity Add(Entity entity)
    {
        entity.Scene = this;
        foreach (var component in entity.Components)
        {
            component.Owner = entity;
        }

        Entities.Add(entity);
        return entity;
    }

    public Entity Create(string name, Vector2 position, Guid? parentId = null)
    {
        var entity = new Entity { Name = name, ParentId = parentId, Transform = { Position = position } };
        return Add(entity);
    }

    public bool Remove(Guid id)
    {
        var entity = Find(id);
        if (entity is null)
        {
            return false;
        }

        // Reparenta los hijos al abuelo para no perder ramas completas.
        foreach (var child in ChildrenOf(id).ToList())
        {
            child.ParentId = entity.ParentId;
        }

        Entities.Remove(entity);
        entity.Scene = null;
        return true;
    }

    /// <summary>Matriz de mundo acumulando la jerarquía de padres.</summary>
    public Matrix4x4 GetWorldMatrix(Entity entity)
    {
        var matrix = entity.Transform.LocalMatrix;
        var parentId = entity.ParentId;
        var guard = 0;

        while (parentId is not null && guard++ < 256)
        {
            var parent = Find(parentId.Value);
            if (parent is null)
            {
                break;
            }

            matrix *= parent.Transform.LocalMatrix;
            parentId = parent.ParentId;
        }

        return matrix;
    }

    public Vector2 GetWorldPosition(Entity entity) => GetWorldMatrix(entity).Translation.AsVector2();

    /// <summary>Coloca una entidad por su posición de mundo respetando la jerarquía.</summary>
    public void SetWorldPosition(Entity entity, Vector2 world)
    {
        var parent = entity.ParentId is { } parentId ? Find(parentId) : null;

        if (parent is null)
        {
            entity.Transform.Position = world;
            return;
        }

        var parentMatrix = GetWorldMatrix(parent);

        if (Matrix4x4.Invert(parentMatrix, out var inverse))
        {
            var local = Vector3.Transform(new Vector3(world, 0f), inverse);
            entity.Transform.Position = new Vector2(local.X, local.Y);
        }
        else
        {
            entity.Transform.Position = world;
        }
    }

    /// <summary>Distancia entre dos entidades usando sus posiciones de mundo.</summary>
    public float WorldDistance(Entity a, Entity b) => Vector2.Distance(GetWorldPosition(a), GetWorldPosition(b));

    /// <summary>Cámara 3D principal, si la escena usa el modo 2.5D.</summary>
    public Camera2D? GetMainCamera() =>
        Entities.Select(e => e.GetComponent<Camera2D>())
            .FirstOrDefault(c => c is { Enabled: true, IsMain: true });

    /// <summary>Reconstruye las referencias internas después de deserializar.</summary>
    public void Rebind()
    {
        foreach (var entity in Entities)
        {
            entity.Scene = this;
            foreach (var component in entity.Components)
            {
                component.Owner = entity;
            }
        }
    }
}

internal static class MatrixExtensions
{
    public static Vector2 AsVector2(this System.Numerics.Vector3 value) => new(value.X, value.Y);
}
