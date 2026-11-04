using System.Numerics;
using Ollin.Core.Model;
using Ollin.Core.Model.Components;
using Ollin.Core.Scenes;
using Ollin.Scripting;
using Xunit;

namespace Ollin.Tests;

public sealed class ScriptingApiTests
{
    private sealed class TestScript : OllinScript
    {
    }

    [Fact]
    public void ApiDeNavegacionUiYSeñales()
    {
        var scene = new Scene();
        var entity = scene.Create("Jugador", new Vector2(1f, 2f));
        var canvas = entity.AddComponent(new UiCanvas());
        canvas.Add("vidas", UiElementKind.Label, "3", 0f, 0f);
        entity.AddComponent(new NavigationAgent2D());

        var script = new TestScript();
        script.Attach(entity);

        Assert.Equal(new Vector2(1f, 2f), script.WorldPosition);
        Assert.True(script.SetUiText("vidas", "2"));
        Assert.True(script.MoveTo(new Vector2(4f, 2f)));
        Assert.False(string.IsNullOrEmpty(script.Entity.Name));

        object? received = null;
        script.Connect("test", value => received = value);
        script.Emit("test", "ok");
        Assert.Equal("ok", received);
    }
}
