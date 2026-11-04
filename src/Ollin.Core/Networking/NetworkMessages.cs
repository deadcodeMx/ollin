using System.Numerics;
using System.Text.Json;

namespace Ollin.Core.Networking;

public sealed class NetworkTransformState
{
    public Guid Id { get; set; }
    public int Owner { get; set; }
    public Vector2 Position { get; set; }
    public float Rotation { get; set; }
    public Vector2 Scale { get; set; } = Vector2.One;
}

public sealed class NetworkSnapshot
{
    private static readonly JsonSerializerOptions Options = new() { IncludeFields = true };
    public int PeerId { get; set; }
    public long Tick { get; set; }
    public List<NetworkTransformState> Transforms { get; set; } = new();

    public byte[] ToBytes() => JsonSerializer.SerializeToUtf8Bytes(this, Options);

    public static NetworkSnapshot? FromBytes(byte[] bytes)
    {
        try { return JsonSerializer.Deserialize<NetworkSnapshot>(bytes, Options); }
        catch { return null; }
    }
}
