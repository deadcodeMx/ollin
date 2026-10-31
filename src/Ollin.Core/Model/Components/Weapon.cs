using System.Numerics;
using Ollin.Core.Hud;
using Ollin.Core.Input;

namespace Ollin.Core.Model.Components;

/// <summary>
/// Arma de primera persona sin código: dispara un rayo desde la cámara y daña la
/// salud del primer objetivo alcanzado (las paredes bloquean el disparo).
/// </summary>
public sealed class Weapon : Component, ITickable
{
    public string Name { get; set; } = "Pistola";

    public int Slot { get; set; } = 1;

    public float Damage { get; set; } = 34f;

    public float Range { get; set; } = 30f;

    public float Cooldown { get; set; } = 0.3f;

    /// <summary>-1 = munición infinita.</summary>
    public int Ammo { get; set; } = -1;

    public bool Automatic { get; set; } = true;

    public bool ShowAmmoOnHud { get; set; }

    public bool IsProjectile { get; set; }

    public float ProjectileSpeed { get; set; } = 18f;

    public float ProjectileSize { get; set; } = 0.25f;

    public string? ProjectileTexture { get; set; }

    public int MagazineSize { get; set; }

    public int ReserveAmmo { get; set; } = -1;

    public float ReloadSeconds { get; set; } = 1.2f;

    public bool AutoReload { get; set; } = true;

    private float _cooldown;
    private float _reloadTimer;
    private bool _reloading;

    public void OnUpdate(float deltaTime)
    {
        if (_cooldown > 0f)
        {
            _cooldown -= deltaTime;
        }

        if (_reloading)
        {
            _reloadTimer -= deltaTime;

            if (_reloadTimer <= 0f)
            {
                FinishReload();
            }
        }

        if (Owner?.Scene is not { } scene)
        {
            return;
        }

        if (Owner.GetComponent<WeaponGroup>() is { } group && group.ActiveSlot != Slot)
        {
            return;
        }

        if (InputState.WasKeyPressed(KeyCode.R) && !_reloading && MagazineSize > 0 && Ammo < MagazineSize)
        {
            StartReload();
        }

        var pressed = Automatic
            ? InputState.IsMouseButtonDown(0)
            : InputState.WasMouseButtonPressed(0);

        if (!pressed || _cooldown > 0f || _reloading)
        {
            return;
        }

        if (Ammo == 0)
        {
            StartReload();
            return;
        }

        var camera = Owner.GetComponent<Camera3D>() ?? scene.GetMainCamera3D();

        if (camera is null)
        {
            return;
        }

        Fire(scene, camera);
        _cooldown = MathF.Max(0.05f, Cooldown);

        if (Ammo > 0)
        {
            Ammo--;

            if (Ammo == 0 && AutoReload)
            {
                StartReload();
            }
        }

        if (ShowAmmoOnHud)
        {
            HudHost.SetLabel("Munición", Ammo < 0 ? "∞" : Ammo.ToString());
        }
    }

    public void StartReload()
    {
        if (MagazineSize <= 0 || Ammo >= MagazineSize || ReserveAmmo == 0 || _reloading)
        {
            return;
        }

        _reloading = true;
        _reloadTimer = MathF.Max(0.1f, ReloadSeconds);
        HudHost.ShowMessage("Recargando…", ReloadSeconds);
    }

    private void FinishReload()
    {
        _reloading = false;
        var needed = MagazineSize - System.Math.Max(0, Ammo);
        var available = ReserveAmmo < 0 ? needed : System.Math.Min(needed, ReserveAmmo);
        Ammo = System.Math.Max(0, Ammo) + available;

        if (ReserveAmmo > 0)
        {
            ReserveAmmo -= available;
        }
    }

    /// <summary>Dispara una vez (también disponible para scripts).</summary>
    public bool Fire(Scenes.Scene scene, Camera3D camera)
    {
        if (IsProjectile)
        {
            Projectile.Spawn(
                scene,
                new Vector2(camera.Eye.X, camera.Eye.Z),
                new Vector2(camera.Forward.X, camera.Forward.Z),
                Damage,
                Owner?.Id ?? Guid.Empty,
                ProjectileSpeed,
                ProjectileSize,
                ProjectileTexture);
            return true;
        }

        var origin = camera.Eye;
        var direction = camera.Forward;
        var nearest = Range;
        Entity? target = null;

        foreach (var entity in scene.Entities)
        {
            if (!entity.Active || entity == Owner || entity.GetComponent<Health>() is not { IsDead: false })
            {
                continue;
            }

            var center = new Vector3(entity.Transform.Position.X, 0.9f, entity.Transform.Position.Y);
            var toCenter = center - origin;
            var along = Vector3.Dot(toCenter, direction);

            if (along <= 0f || along > nearest)
            {
                continue;
            }

            var closest = origin + direction * along;
            var radius = 1.1f;

            if (Vector3.DistanceSquared(closest, center) > radius * radius)
            {
                continue;
            }

            if (WallDistance(scene, origin, direction, along) is { } wallDistance && wallDistance < along)
            {
                continue;
            }

            target = entity;
            nearest = along;
        }

        if (target is null)
        {
            return false;
        }

        target.GetComponent<Health>()?.TakeDamage(Damage);
        return true;
    }

    private static float? WallDistance(Scenes.Scene scene, Vector3 origin, Vector3 direction, float maxDistance)
    {
        float? nearest = null;

        foreach (var entity in scene.Entities)
        {
            if (!entity.Active || entity.GetComponent<Wall3D>() is not { Enabled: true, Visible: true, Collides: true } wall)
            {
                continue;
            }

            if (RayBox(origin, direction, wall.Bounds.Min, wall.Bounds.Max, out var distance) &&
                distance <= maxDistance &&
                (nearest is null || distance < nearest))
            {
                nearest = distance;
            }
        }

        return nearest;
    }

    private static bool RayBox(Vector3 origin, Vector3 direction, Vector3 min, Vector3 max, out float distance)
    {
        distance = 0f;
        var near = 0f;
        var far = float.PositiveInfinity;

        for (var axis = 0; axis < 3; axis++)
        {
            var o = axis == 0 ? origin.X : axis == 1 ? origin.Y : origin.Z;
            var d = axis == 0 ? direction.X : axis == 1 ? direction.Y : direction.Z;
            var lo = axis == 0 ? min.X : axis == 1 ? min.Y : min.Z;
            var hi = axis == 0 ? max.X : axis == 1 ? max.Y : max.Z;

            if (MathF.Abs(d) < 0.000001f)
            {
                if (o < lo || o > hi) return false;
                continue;
            }

            var t0 = (lo - o) / d;
            var t1 = (hi - o) / d;

            if (t0 > t1) (t0, t1) = (t1, t0);

            near = MathF.Max(near, t0);
            far = MathF.Min(far, t1);

            if (near > far) return false;
        }

        distance = near;
        return true;
    }

    public override Component CloneFor(Entity newOwner) => new Weapon
    {
        Enabled = Enabled,
        Damage = Damage,
        Range = Range,
        Cooldown = Cooldown,
        Ammo = Ammo,
        Automatic = Automatic,
        ShowAmmoOnHud = ShowAmmoOnHud,
        Name = Name,
        Slot = Slot,
        IsProjectile = IsProjectile,
        ProjectileSpeed = ProjectileSpeed,
        ProjectileSize = ProjectileSize,
        ProjectileTexture = ProjectileTexture,
        MagazineSize = MagazineSize,
        ReserveAmmo = ReserveAmmo,
        ReloadSeconds = ReloadSeconds,
        AutoReload = AutoReload,
        Owner = newOwner,
    };
}
