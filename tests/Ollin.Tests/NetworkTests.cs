using System.Numerics;
using Ollin.Core.Networking;
using Xunit;

namespace Ollin.Tests;

public sealed class NetworkTests
{
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
