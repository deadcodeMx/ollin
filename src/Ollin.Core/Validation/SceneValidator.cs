using Ollin.Core.Model;
using Ollin.Core.Model.Components;
using Ollin.Core.Scenes;

namespace Ollin.Core.Validation;

public enum SceneIssueKind
{
    MissingAsset,
    Hierarchy,
    DuplicateId,
}

/// <summary>Problema detectado al validar una escena antes de reproducir o exportar.</summary>
public sealed record SceneValidationIssue(
    SceneIssueKind Kind,
    string EntityName,
    string Detail,
    bool IsError = false);

/// <summary>
/// Valida rutas de assets y jerarquía. No bloquea la carga: informa para que el
/// editor muestre mensajes claros en vez de fallar en silencio.
/// </summary>
public static class SceneValidator
{
    public static List<SceneValidationIssue> Validate(Scene scene, string projectRoot)
    {
        var issues = new List<SceneValidationIssue>();
        var ids = new HashSet<Guid>();

        foreach (var entity in scene.Entities)
        {
            if (!ids.Add(entity.Id))
            {
                issues.Add(new SceneValidationIssue(SceneIssueKind.DuplicateId, entity.Name, "Id de entidad duplicado.", true));
            }

            if (entity.ParentId == entity.Id)
            {
                issues.Add(new SceneValidationIssue(SceneIssueKind.Hierarchy, entity.Name, "La entidad es su propio padre.", true));
            }
            else if (entity.ParentId is { } parentId && scene.Find(parentId) is null)
            {
                issues.Add(new SceneValidationIssue(SceneIssueKind.Hierarchy, entity.Name, "El padre no existe en la escena.", true));
            }

            foreach (var component in entity.Components)
            {
                CheckAsset(issues, entity, projectRoot, "Textura", (component as SpriteRenderer)?.TexturePath);
                CheckAsset(issues, entity, projectRoot, "Animación", (component as SpriteAnimator)?.TexturePath);
                CheckAsset(issues, entity, projectRoot, "Audio", (component as AudioSource)?.Clip);
                CheckAsset(issues, entity, projectRoot, "Script", (component as ScriptComponent)?.ScriptPath);
            }
        }

        DetectHierarchyCycles(scene, issues);
        return issues;
    }

    public static bool HasMissingAssets(IEnumerable<SceneValidationIssue> issues) =>
        issues.Any(issue => issue.Kind == SceneIssueKind.MissingAsset);

    private static void CheckAsset(
        List<SceneValidationIssue> issues,
        Entity entity,
        string projectRoot,
        string kind,
        string? path)
    {
        if (string.IsNullOrWhiteSpace(path))
        {
            return;
        }

        var full = Path.IsPathRooted(path) ? path : Path.Combine(projectRoot, path.Replace('/', Path.DirectorySeparatorChar));

        if (!File.Exists(full))
        {
            issues.Add(new SceneValidationIssue(
                SceneIssueKind.MissingAsset,
                entity.Name,
                $"{kind}: no existe «{path}».",
                false));
        }
    }

    private static void DetectHierarchyCycles(Scene scene, List<SceneValidationIssue> issues)
    {
        foreach (var entity in scene.Entities)
        {
            var seen = new HashSet<Guid> { entity.Id };
            var current = entity;

            for (var guard = 0; guard < 256; guard++)
            {
                if (current.ParentId is not { } parentId)
                {
                    break;
                }

                if (!seen.Add(parentId))
                {
                    issues.Add(new SceneValidationIssue(
                        SceneIssueKind.Hierarchy,
                        entity.Name,
                        "Jerarquía circular detectada.",
                        true));
                    break;
                }

                var parent = scene.Find(parentId);

                if (parent is null)
                {
                    break;
                }

                current = parent;
            }
        }
    }
}
