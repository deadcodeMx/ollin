using System.Numerics;
using System.Text.Json.Serialization;

namespace Ollin.Core.Model.Components;

/// <summary>Partículas 2D simples para polvo, chispas, humo o sangre.</summary>
public sealed class ParticleEmitter2D : Component, ITickable
{
    public bool Emitting { get; set; } = true;
    public float Rate { get; set; } = 20f;
    public float Lifetime { get; set; } = 0.8f;
    public float SpeedMin { get; set; } = 1f;
    public float SpeedMax { get; set; } = 3f;
    public float Spread { get; set; } = 360f;
    public float Size { get; set; } = 0.12f;
    public float Gravity { get; set; } = 0f;
    public Vector4 StartColor { get; set; } = new(1f, 0.8f, 0.3f, 1f);
    public Vector2 Offset { get; set; }

    [JsonIgnore] public List<Particle2D> Particles { get; } = new();
    private float _spawnAccumulator;

    public void OnUpdate(float deltaTime)
    {
        if (Owner?.Scene is not { } scene) return;
        var origin = scene.GetWorldPosition(Owner) + Offset;

        if (Emitting && Rate > 0f)
        {
            _spawnAccumulator += deltaTime * Rate;
            while (_spawnAccumulator >= 1f)
            {
                _spawnAccumulator -= 1f;
                var angle = (Random.Shared.NextSingle() * 2f - 1f) * Spread * MathF.PI / 360f;
                var speed = SpeedMin + Random.Shared.NextSingle() * (SpeedMax - SpeedMin);
                Particles.Add(new Particle2D
                {
                    Position = origin,
                    Velocity = new Vector2(MathF.Cos(angle), MathF.Sin(angle)) * speed,
                    Life = Lifetime,
                    MaxLife = Lifetime,
                    Color = StartColor,
                });
            }
        }

        for (var i = Particles.Count - 1; i >= 0; i--)
        {
            var particle = Particles[i];
            particle.Life -= deltaTime;
            if (particle.Life <= 0f) { Particles.RemoveAt(i); continue; }
            particle.Velocity += new Vector2(0f, -Gravity) * deltaTime;
            particle.Position += particle.Velocity * deltaTime;
            particle.Color.W = System.Math.Clamp(particle.Life / MathF.Max(0.001f, particle.MaxLife), 0f, 1f) * StartColor.W;
            Particles[i] = particle;
        }
    }

    public override Component CloneFor(Entity newOwner) => new ParticleEmitter2D
    {
        Enabled = Enabled, Emitting = Emitting, Rate = Rate, Lifetime = Lifetime, SpeedMin = SpeedMin,
        SpeedMax = SpeedMax, Spread = Spread, Size = Size, Gravity = Gravity, StartColor = StartColor,
        Offset = Offset, Owner = newOwner,
    };
}

public struct Particle2D
{
    public Vector2 Position;
    public Vector2 Velocity;
    public float Life;
    public float MaxLife;
    public Vector4 Color;
}

/// <summary>Estela de posiciones detrás de una entidad.</summary>
public sealed class Trail2D : Component, ITickable
{
    public float Lifetime { get; set; } = 0.35f;
    public float Interval { get; set; } = 0.03f;
    public float Width { get; set; } = 0.12f;
    public Vector4 Color { get; set; } = new(1f, 0.85f, 0.4f, 0.8f);
    [JsonIgnore] public List<TrailPoint2D> Points { get; } = new();
    private float _timer;

    public void OnUpdate(float deltaTime)
    {
        if (Owner?.Scene is not { } scene) return;
        _timer += deltaTime;
        if (_timer >= Interval)
        {
            _timer = 0f;
            Points.Add(new TrailPoint2D { Position = scene.GetWorldPosition(Owner), Life = Lifetime, MaxLife = Lifetime });
        }

        for (var i = Points.Count - 1; i >= 0; i--)
        {
            var point = Points[i];
            point.Life -= deltaTime;
            if (point.Life <= 0f) Points.RemoveAt(i); else Points[i] = point;
        }
    }

    public override Component CloneFor(Entity newOwner) => new Trail2D
    {
        Enabled = Enabled, Lifetime = Lifetime, Interval = Interval, Width = Width, Color = Color, Owner = newOwner,
    };
}

public struct TrailPoint2D
{
    public Vector2 Position;
    public float Life;
    public float MaxLife;
}
