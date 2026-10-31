using System.Numerics;

namespace Ollin.Rendering;

/// <summary>Vista previa de la caja que colocará el pincel 3D.</summary>
public readonly record struct BrushPreview3D(Vector3 Center, Vector3 Size);
