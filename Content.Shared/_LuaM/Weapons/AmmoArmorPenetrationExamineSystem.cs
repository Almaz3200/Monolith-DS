using System.Diagnostics.CodeAnalysis;
using Content.Shared.Examine;
using Content.Shared.Projectiles;
using Robust.Shared.Prototypes;

namespace Content.Shared._LuaM.Weapons;

/// <summary>
/// Adds the armor penetration and pellet count of the bullet a cartridge fires to the cartridge's examine text.
/// </summary>
public sealed class AmmoArmorPenetrationExamineSystem : EntitySystem
{
    [Dependency] private IPrototypeManager _proto = default!;
    [Dependency] private IComponentFactory _factory = default!;

    /// <summary>
    /// Adds the examine lines for the projectile prototype a cartridge fires.
    /// Hitscan and other non-projectile ammo gets nothing.
    /// </summary>
    public void AddExamine(ExaminedEvent args, EntProtoId projectileProto)
    {
        if (!_proto.TryIndex(projectileProto, out var proto))
            return;

        proto.TryGetComponent<ProjectileSpreadComponent>(out var spread, _factory);

        if (!TryGetProjectile(proto, spread, out var projectile))
            return;

        var percent = (int) MathF.Round(projectile.ArmorPenetration * 100f);
        var color = percent switch
        {
            > 0 => "lightgreen",
            < 0 => "red",
            _ => "gray",
        };

        args.PushMarkup(Loc.GetString("ammo-armor-penetration-examine", ("color", color), ("percent", percent)));

        if (spread is { Count: > 1 })
            args.PushMarkup(Loc.GetString("ammo-pellet-count-examine", ("count", spread.Count)));
    }

    private bool TryGetProjectile(EntityPrototype proto, ProjectileSpreadComponent? spread, [NotNullWhen(true)] out ProjectileComponent? projectile)
    {
        if (proto.TryGetComponent(out projectile, _factory))
            return true;

        // Shot spreads may only define the pellet they shoot.
        if (spread != null && _proto.TryIndex(spread.Proto, out var pelletProto))
            return pelletProto.TryGetComponent(out projectile, _factory);

        projectile = null;
        return false;
    }
}
