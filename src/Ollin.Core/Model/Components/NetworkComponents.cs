using System.Text.Json.Serialization;

namespace Ollin.Core.Model.Components;

/// <summary>Identidad de red de una entidad.</summary>
public sealed class NetworkIdentity : Component
{
    public Guid NetworkId { get; set; } = Guid.NewGuid();
    public int OwnerPeer { get; set; }
    public bool IsLocal { get; set; } = true;

    public override Component CloneFor(Entity newOwner) => new NetworkIdentity
    {
        Enabled = Enabled, NetworkId = NetworkId, OwnerPeer = OwnerPeer, IsLocal = IsLocal, Owner = newOwner,
    };
}

/// <summary>Marca el transform de una entidad para sincronización de red.</summary>
public sealed class NetworkTransform : Component
{
    public bool SyncPosition { get; set; } = true;
    public bool SyncRotation { get; set; } = true;
    public bool SyncScale { get; set; } = true;

    public override Component CloneFor(Entity newOwner) => new NetworkTransform
    {
        Enabled = Enabled, SyncPosition = SyncPosition, SyncRotation = SyncRotation, SyncScale = SyncScale, Owner = newOwner,
    };
}
