namespace Ollin.Core.Model.Components;

/// <summary>Gira la entidad de forma constante. Útil para monedas y adornos.</summary>
public sealed class RotatorComponent : Component, ITickable
{
    /// <summary>Velocidad angular en grados por segundo (positivo = antihorario).</summary>
    public float DegreesPerSecond { get; set; } = 90f;

    public void OnUpdate(float deltaTime)
    {
        if (Owner is null)
        {
            return;
        }

        Owner.Transform.Rotation += DegreesPerSecond * (MathF.PI / 180f) * deltaTime;
    }

    public override Component CloneFor(Entity newOwner) => new RotatorComponent
    {
        Enabled = Enabled,
        DegreesPerSecond = DegreesPerSecond,
        Owner = newOwner,
    };
}
