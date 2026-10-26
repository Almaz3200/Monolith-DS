using System.Numerics;
using Content.Shared._LuaM.Shuttles;
using Content.Shared.Shuttles.Components;
using Content.Shared.Shuttles.Systems;
using Robust.Shared.Map;
using Robust.Shared.Map.Components;
using Robust.Shared.Physics.Components;

namespace Content.Server._LuaM.Shuttles;

public sealed partial class RadarIdentificationSystem : EntitySystem
{
    [Dependency] private FriendlyIffSystem _friendlyIff = default!;
    [Dependency] private RadarStealthFieldSystem _stealthField = default!;
    [Dependency] private SharedShuttleSystem _shuttles = default!;
    [Dependency] private SharedTransformSystem _transform = default!;

    // -- то же, что радар подписывает настоящим именем
    public bool IsIdentified(EntityUid grid, MapCoordinates viewer, EntityUid? viewerEntity)
    {
        if (!_shuttles.CanDraw(grid))
            return false;

        if (TryComp<IFFComponent>(grid, out var iff) &&
            (iff.Flags & (IFFFlags.HideLabel | IFFFlags.HideLabelAlways)) != 0 &&
            !_friendlyIff.IsFriendly(viewerEntity, grid))
        {
            return false;
        }

        return !_stealthField.IsHiddenFrom(grid, viewer, viewerEntity);
    }

    public bool CanTarget(EntityUid console, EntityUid grid, EntityUid actor)
    {
        var consoleXform = Transform(console);
        if (grid == consoleXform.GridUid ||
            !HasComp<MapGridComponent>(grid) ||
            !TryComp<RadarConsoleComponent>(console, out var radar) ||
            Transform(grid).MapID != consoleXform.MapID)
        {
            return false;
        }

        var viewer = _transform.GetMapCoordinates(consoleXform);
        if (!IsIdentified(grid, viewer, actor))
            return false;

        // -- радар уводится ПКМ куда угодно по карте, дальность ограничена только у радаров с MaxIffRange
        if (radar.MaxIffRange is not { } maxRange ||
            TryComp<FriendlyIffComponent>(grid, out var friendly) && friendly.AlwaysVisible && _friendlyIff.IsFriendly(actor, grid))
        {
            return true;
        }

        return Vector2.Distance(GetCenter(grid), viewer.Position) <= maxRange;
    }

    public Vector2 GetCenter(EntityUid grid)
    {
        var localCenter = TryComp<PhysicsComponent>(grid, out var physics) ? physics.LocalCenter : Vector2.Zero;
        return _transform.ToMapCoordinates(new EntityCoordinates(grid, localCenter)).Position;
    }
}
