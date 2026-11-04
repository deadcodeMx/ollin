using Ollin.Core.Diagnostics;
using Xunit;

namespace Ollin.Tests;

public sealed class ProfilerTests
{
    [Fact]
    public void CalculaFpsYFrameTime()
    {
        Profiler.Reset();
        for (var i = 0; i < 60; i++) Profiler.Tick(1f / 60f);
        Assert.InRange(Profiler.Fps, 59f, 61f);
        Assert.InRange(Profiler.FrameMs, 15f, 18f);
    }
}
