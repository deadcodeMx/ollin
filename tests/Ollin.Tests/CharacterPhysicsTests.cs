using System.Numerics;
using Ollin.Core.Model.Components;
using Ollin.Core.Scenes;
using Xunit;

namespace Ollin.Tests;

public sealed class CharacterPhysicsTests
{
    [Fact]
    public void ContactoDeSueloYPendienteActualizaEstado()
    {
        var character = new CharacterBody2D { MaxSlopeAngle = 50f };

        character.RegisterContact(new Vector2(0f, 1f), null);
        Assert.True(character.IsGrounded);
        Assert.Equal(Vector2.UnitY, character.FloorNormal);

        character.BeginPhysicsStep();
        Assert.True(character.WasGrounded);
        Assert.False(character.IsGrounded);

        character.RegisterContact(new Vector2(0.9f, 0.1f), null);
        Assert.False(character.IsGrounded);
        Assert.True(character.IsOnWall);
    }

    [Fact]
    public void PlataformaMovilExponeDeltaYVelocidad()
    {
        var scene = new Scene();
        var entity = scene.Create("Plataforma", new Vector2(0f, 0f));
        var platform = entity.AddComponent(new MovingPlatform2D { Travel = new Vector2(2f, 0f), Speed = 1f });

        platform.OnUpdate(0.1f);
        var first = platform.DeltaThisFrame;
        platform.OnUpdate(0.1f);

        Assert.NotEqual(Vector2.Zero, first);
        Assert.NotEqual(Vector2.Zero, platform.Velocity);
    }
}
