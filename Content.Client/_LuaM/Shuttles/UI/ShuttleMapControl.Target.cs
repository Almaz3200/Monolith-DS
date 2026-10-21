using System.Numerics;
using Content.Client._Lua.UserInterface;
using Robust.Client.Graphics;

namespace Content.Client.Shuttles.UI;

/// <summary>
/// Draws the console marker (the radar marker on NAV) on the sector map.
/// </summary>
public sealed partial class ShuttleMapControl
{
    private static readonly Color MapTargetColor = Color.FromHex("#99ff66");
    private const float MapTargetEdgeMargin = 18f;

    private Vector2? _mapTarget;
    private NetEntity? _mapTargetEntity;
    private bool _mapTargetHidden = true;

    public void SetMapTarget(Vector2? target, NetEntity? targetEntity, bool hidden)
    {
        _mapTarget = target;
        _mapTargetEntity = targetEntity;
        _mapTargetHidden = hidden;
    }

    private void DrawMapTarget(DrawingHandleScreen handle, Matrix3x2 mapTransform)
    {
        if (_mapTargetHidden || _mapTarget is not { } target)
            return;

        // The marker lives on the shuttle's own map.
        if (_shuttleEntity is not { } shuttle
            || !EntManager.TryGetComponent(shuttle, out TransformComponent? shuttleXform)
            || shuttleXform.MapID != ViewingMap)
            return;

        var targetLocal = Vector2.Transform(target, mapTransform);
        var targetUi = ScalePosition(targetLocal with { Y = -targetLocal.Y });

        var min = new Vector2(MapTargetEdgeMargin, MapTargetEdgeMargin);
        var max = new Vector2(PixelWidth - MapTargetEdgeMargin, PixelHeight - MapTargetEdgeMargin);
        var markerUi = Vector2.Clamp(targetUi, min, max);
        var outside = markerUi != targetUi;

        var shuttlePos = _xformSystem.GetWorldPosition(shuttleXform);
        if (_physicsQuery.TryGetComponent(shuttle, out var shuttlePhysics))
            shuttlePos = Maps.GetGridPosition((shuttle, shuttlePhysics), shuttlePos, _xformSystem.GetWorldRotation(shuttleXform));

        var alpha = outside ? 0.6f : 1f;
        LunaDraw.Circle(handle, markerUi, 7f, MapTargetColor.WithAlpha(alpha), false);
        LunaDraw.Circle(handle, markerUi, 2f, MapTargetColor.WithAlpha(alpha), true);
        if (!outside)
        {
            LunaDraw.Line(handle, markerUi + new Vector2(0f, -12f), markerUi + new Vector2(0f, -8f), MapTargetColor);
            LunaDraw.Line(handle, markerUi + new Vector2(0f, 8f), markerUi + new Vector2(0f, 12f), MapTargetColor);
            LunaDraw.Line(handle, markerUi + new Vector2(-12f, 0f), markerUi + new Vector2(-8f, 0f), MapTargetColor);
            LunaDraw.Line(handle, markerUi + new Vector2(8f, 0f), markerUi + new Vector2(12f, 0f), MapTargetColor);
        }

        var targetEntity = EntManager.GetEntity(_mapTargetEntity);
        var name = targetEntity != EntityUid.Invalid && EntManager.TryGetComponent(targetEntity, out MetaDataComponent? meta)
            ? meta.EntityName
            : Loc.GetString("shuttle-console-target-name");

        var distance = Vector2.Distance(target, shuttlePos);
        var displayedDistance = distance < 50f ? $"{distance:0.0}" : distance < 1000 ? $"{distance:0}" : $"{distance / 1000:0.0}k";
        var label = Loc.GetString("shuttle-console-iff-label", ("name", name), ("distance", displayedDistance));

        // Keep the label inside the map when the marker sits on the edge.
        var labelSize = handle.GetDimensions(_font, label, 1f);
        var labelPos = markerUi + new Vector2(14f, -labelSize.Y * 0.5f);
        if (labelPos.X + labelSize.X > PixelWidth - 4f)
            labelPos.X = markerUi.X - 14f - labelSize.X;
        labelPos.Y = Math.Clamp(labelPos.Y, 4f, MathF.Max(4f, PixelHeight - labelSize.Y - 4f));

        DrawSoftMapText(handle, label, labelPos, 1f, MapTargetColor.WithAlpha(0.92f), Color.Black.WithAlpha(0.5f));
    }
}
