using System.Collections.Concurrent;
using System.Numerics;
using Ollin.Core.Model.Components;
using Ollin.Core.Networking;
using Ollin.Core.Scenes;

namespace Ollin.Runtime;

/// <summary>Sincroniza transforms de entidades con NetworkIdentity/NetworkTransform.</summary>
internal sealed class NetworkSystem
{
    private readonly ConcurrentQueue<NetworkSnapshot> _incoming = new();
    private float _timer;
    private Scene? _scene;

    public NetworkSystem()
    {
        NetworkManager.SnapshotReceived += snapshot => _incoming.Enqueue(snapshot);
    }

    public void Step(Scene scene, float deltaTime)
    {
        if (!NetworkManager.IsRunning) return;
        _scene = scene;
        while (_incoming.TryDequeue(out var snapshot)) Apply(scene, snapshot);
        _timer += deltaTime;
        if (_timer < 0.05f) return;
        _timer = 0f;
        Send(scene);
    }

    private static void Send(Scene scene)
    {
        var snapshot = new NetworkSnapshot { PeerId = NetworkManager.PeerId, Tick = Environment.TickCount64 };
        foreach (var entity in scene.Entities)
        {
            if (!entity.Active) continue;
            if (entity.GetComponent<NetworkIdentity>() is not { IsLocal: true } identity) continue;
            if (entity.GetComponent<NetworkTransform>() is not { Enabled: true } sync) continue;
            identity.OwnerPeer = NetworkManager.PeerId;
            snapshot.Transforms.Add(new NetworkTransformState
            {
                Id = identity.NetworkId,
                Owner = identity.OwnerPeer,
                Position = sync.SyncPosition ? entity.Transform.Position : Vector2.Zero,
                Rotation = sync.SyncRotation ? entity.Transform.Rotation : 0f,
                Scale = sync.SyncScale ? entity.Transform.Scale : Vector2.One,
            });
        }
        if (snapshot.Transforms.Count > 0) NetworkManager.Send(snapshot);
    }

    private static void Apply(Scene scene, NetworkSnapshot snapshot)
    {
        if (snapshot.PeerId == NetworkManager.PeerId) return;
        foreach (var state in snapshot.Transforms)
        {
            var entity = scene.Entities.FirstOrDefault(item => item.GetComponent<NetworkIdentity>()?.NetworkId == state.Id);
            if (entity is null) continue;
            var identity = entity.GetComponent<NetworkIdentity>()!;
            if (identity.IsLocal) continue;
            var sync = entity.GetComponent<NetworkTransform>();
            if (sync is not { Enabled: true }) continue;
            if (sync.SyncPosition) entity.Transform.Position = state.Position;
            if (sync.SyncRotation) entity.Transform.Rotation = state.Rotation;
            if (sync.SyncScale) entity.Transform.Scale = state.Scale;
        }
    }
}
