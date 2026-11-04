namespace Ollin.Core.Diagnostics;

/// <summary>Métricas ligeras de frame para el HUD y el profiler.</summary>
public static class Profiler
{
    private static int _frames;
    private static float _accumulated;
    private static float _fps;
    private static float _frameMs;

    public static float Fps => _fps;
    public static float FrameMs => _frameMs;
    public static int FrameCount { get; private set; }
    public static int DrawCalls { get; set; }

    public static void Tick(float deltaSeconds)
    {
        FrameCount++;
        _frames++;
        _accumulated += MathF.Max(0f, deltaSeconds);
        if (_accumulated < 0.25f) return;
        _fps = _frames / _accumulated;
        _frameMs = _accumulated * 1000f / System.Math.Max(1, _frames);
        _frames = 0;
        _accumulated = 0f;
    }

    public static void Reset()
    {
        _frames = 0;
        _accumulated = 0f;
        _fps = 0f;
        _frameMs = 0f;
        FrameCount = 0;
        DrawCalls = 0;
    }
}
