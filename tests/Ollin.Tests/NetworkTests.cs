using System.Numerics;
using Ollin.Core.Networking;
using Xunit;

namespace Ollin.Tests;

public sealed class NetworkTests
{
    [Fact]
    public void RpcSeSerializaYDeserializa()
    {
        var rpc = new NetworkRpc
        {
            PeerId = 12, Tick = 99, Method = "saludar", Payload = "hola",
        };

        var copy = NetworkRpc.FromBytes(rpc.ToBytes());

        Assert.NotNull(copy);
        Assert.Equal("saludar", copy!.Method);
        Assert.Equal("hola", copy.Payload);
        Assert.Equal(12, copy.PeerId);
    }

    [Fact]
    public void HostExponePuertoYSeDetiene()
    {
        try
        {
            Assert.True(NetworkManager.StartHost(0));
            Assert.True(NetworkManager.IsRunning);
            Assert.True(NetworkManager.IsHost);
            Assert.True(NetworkManager.BoundPort > 0);
        }
        finally
        {
            NetworkManager.Stop();
        }

        Assert.False(NetworkManager.IsRunning);
    }

    [Fact]
    public void SnapshotSeSerializaYDeserializa()
    {
        var snapshot = new NetworkSnapshot
        {
            PeerId = 7,
            Transforms = new()
            {
                new NetworkTransformState { Id = Guid.NewGuid(), Position = new Vector2(3f, 4f), Scale = Vector2.One },
            },
        };

        var bytes = snapshot.ToBytes();
        var copy = NetworkSnapshot.FromBytes(bytes);

        Assert.NotNull(copy);
        Assert.Equal(7, copy!.PeerId);
        Assert.Single(copy.Transforms);
        Assert.Equal(new Vector2(3f, 4f), copy.Transforms[0].Position);
    }
}
