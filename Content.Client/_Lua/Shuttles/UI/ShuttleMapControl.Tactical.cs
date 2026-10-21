using System.Numerics;
using Content.Client._Lua.UserInterface;
using Robust.Client.Graphics;
using Robust.Shared.Collections;

namespace Content.Client.Shuttles.UI;

/// <summary>
/// Tactical look of the shuttle console map: dark backing, coordinate grid, compass and soft labels.
/// </summary>
public sealed partial class ShuttleMapControl
{
    private void DrawTacticalMapBackground(DrawingHandleScreen handle, Matrix3x2 mapTransform, Box2 viewBox)
    {
        var bounds = UIBox2.FromDimensions(Vector2.Zero, PixelSize);
        handle.DrawRect(bounds, Color.FromHex("#0B0F14"));
        handle.DrawRect(bounds, Color.FromHex("#243041").WithAlpha(0.7f), filled: false);

        DrawCoordinateGrid(handle, mapTransform, viewBox);
        DrawMapCompass(handle);
    }

    private void DrawCoordinateGrid(DrawingHandleScreen handle, Matrix3x2 mapTransform, Box2 viewBox)
    {
        if (MinimapScale <= 0f)
            return;

        var minorStep = GetNiceGridStep(46f / MinimapScale);
        var majorStep = minorStep * 4f;
        var minorColor = Color.FromHex("#7E8BA0").WithAlpha(0.08f);
        var majorColor = Color.FromHex("#8A96A8").WithAlpha(0.16f);
        var axisColor = Color.FromHex("#C5CEDB").WithAlpha(0.28f);
        var labelColor = Color.FromHex("#8A96A8").WithAlpha(0.7f);
        var labelShadow = Color.Black.WithAlpha(0.42f);

        DrawGridAxis(handle, mapTransform, viewBox.Left, viewBox.Right, viewBox.Bottom, viewBox.Top, minorStep, majorStep, minorColor, majorColor, axisColor, vertical: true);
        DrawGridAxis(handle, mapTransform, viewBox.Bottom, viewBox.Top, viewBox.Left, viewBox.Right, minorStep, majorStep, minorColor, majorColor, axisColor, vertical: false);
        DrawCoordinateLabels(handle, mapTransform, viewBox, majorStep, labelColor, labelShadow);
    }

    private static float GetNiceGridStep(float targetWorldStep)
    {
        targetWorldStep = MathF.Max(1f, targetWorldStep);
        var exponent = MathF.Floor(MathF.Log10(targetWorldStep));
        var magnitude = MathF.Pow(10f, exponent);
        var normalized = targetWorldStep / magnitude;
        var nice = normalized <= 1f ? 1f : normalized <= 2f ? 2f : normalized <= 5f ? 5f : 10f;
        return nice * magnitude;
    }

    private void DrawGridAxis(
        DrawingHandleScreen handle,
        Matrix3x2 mapTransform,
        float start,
        float end,
        float crossStart,
        float crossEnd,
        float minorStep,
        float majorStep,
        Color minorColor,
        Color majorColor,
        Color axisColor,
        bool vertical)
    {
        var first = MathF.Floor(start / minorStep) * minorStep;
        for (var value = first; value <= end; value += minorStep)
        {
            var color = MathF.Abs(value) < 0.01f
                ? axisColor
                : IsGridMajor(value, majorStep)
                    ? majorColor
                    : minorColor;

            var a = vertical ? new Vector2(value, crossStart) : new Vector2(crossStart, value);
            var b = vertical ? new Vector2(value, crossEnd) : new Vector2(crossEnd, value);
            LunaDraw.Line(handle, MapToScreen(a, mapTransform), MapToScreen(b, mapTransform), color);
        }
    }

    private void DrawCoordinateLabels(
        DrawingHandleScreen handle,
        Matrix3x2 mapTransform,
        Box2 viewBox,
        float majorStep,
        Color color,
        Color shadow)
    {
        var firstX = MathF.Floor(viewBox.Left / majorStep) * majorStep;
        for (var x = firstX; x <= viewBox.Right; x += majorStep)
        {
            var pos = MapToScreen(new Vector2(x, viewBox.Top), mapTransform) + new Vector2(4f, 5f);
            DrawSoftMapText(handle, x.ToString("0"), pos, 0.72f, color, shadow);
        }

        var firstY = MathF.Floor(viewBox.Bottom / majorStep) * majorStep;
        for (var y = firstY; y <= viewBox.Top; y += majorStep)
        {
            var pos = MapToScreen(new Vector2(viewBox.Left, y), mapTransform) + new Vector2(6f, -10f);
            DrawSoftMapText(handle, y.ToString("0"), pos, 0.72f, color, shadow);
        }
    }

    private static bool IsGridMajor(float value, float majorStep)
        => MathF.Abs(value / majorStep - MathF.Round(value / majorStep)) < 0.02f;

    private Vector2 MapToScreen(Vector2 worldPosition, Matrix3x2 mapTransform)
    {
        var adjusted = Vector2.Transform(worldPosition, mapTransform);
        return ScalePosition(adjusted with { Y = -adjusted.Y });
    }

    private void DrawMapCompass(DrawingHandleScreen handle)
    {
        var center = new Vector2(PixelWidth - 44f, 44f);
        const float radius = 24f;
        var ringColor = Color.FromHex("#8A96A8").WithAlpha(0.2f);
        var northColor = Color.FromHex("#F0C674").WithAlpha(0.86f);
        var eastColor = Color.FromHex("#7E8BA0").WithAlpha(0.82f);
        var headingColor = Color.FromHex("#5EC8E8").WithAlpha(0.9f);

        LunaDraw.Circle(handle, center, radius, ringColor, false);
        LunaDraw.Circle(handle, center, 2.5f, ringColor.WithAlpha(0.8f), true);
        DrawMapCompassNeedle(handle, center, new Vector2(0f, -1f), radius, "N", northColor);
        DrawMapCompassNeedle(handle, center, new Vector2(1f, 0f), radius * 0.82f, "E", eastColor);
        if (_shuttleEntity is { } shuttle && EntManager.EntityExists(shuttle))
        {
            var heading = _xformSystem.GetWorldRotation(shuttle);
            var direction = GetMapCompassDirection(heading);
            DrawMapArrowLine(handle, center - direction * 7f, center + direction * (radius * 0.62f), headingColor, 7f);
        }
    }

    private static Vector2 GetMapCompassDirection(Angle heading)
    {
        var radians = (float) heading.Theta + MathF.PI / 2f;
        return new Vector2(MathF.Cos(radians), -MathF.Sin(radians));
    }

    private void DrawMapCompassNeedle(DrawingHandleScreen handle, Vector2 center, Vector2 direction, float radius, string label, Color color)
    {
        var tip = center + direction * radius;
        var tail = center - direction * (radius * 0.35f);
        DrawMapArrowLine(handle, tail, tip, color.WithAlpha(0.64f), 6f);

        var labelSize = handle.GetDimensions(_font, label, 0.85f);
        var labelPos = tip + direction * 6f - labelSize * 0.5f;
        DrawSoftMapText(handle, label, labelPos, 0.85f, color, Color.Black.WithAlpha(0.46f));
    }

    private static void DrawMapArrowLine(DrawingHandleScreen handle, Vector2 start, Vector2 end, Color color, float headLength)
    {
        var direction = end - start;
        if (direction.LengthSquared() <= 0.001f)
            return;

        direction = Vector2.Normalize(direction);
        var side = new Vector2(-direction.Y, direction.X);
        LunaDraw.Line(handle, start, end, color);
        LunaDraw.Line(handle, end, end - direction * headLength + side * (headLength * 0.45f), color);
        LunaDraw.Line(handle, end, end - direction * headLength - side * (headLength * 0.45f), color);
    }

    private void DrawSoftMapText(DrawingHandleScreen handle, string text, Vector2 position, float scale, Color color, Color shadow)
    {
        handle.DrawString(_font, position + new Vector2(1f, 1f), text, scale, shadow);
        handle.DrawString(_font, position, text, scale, color);
    }

    private Box2 GetVisibleWorldBox()
    {
        var scale = MinimapScale;
        if (scale <= 0f)
            return Box2.CenteredAround(Offset, WorldRangeVector * 2f);

        var mid = MidPointVector;
        var minLocal = -mid;
        var maxLocal = new Vector2(PixelWidth, PixelHeight) - mid;
        var worldMinX = Offset.X + minLocal.X / scale;
        var worldMaxX = Offset.X + maxLocal.X / scale;
        var yA = Offset.Y - minLocal.Y / scale;
        var yB = Offset.Y - maxLocal.Y / scale;
        return new Box2(
            MathF.Min(worldMinX, worldMaxX),
            MathF.Min(yA, yB),
            MathF.Max(worldMinX, worldMaxX),
            MathF.Max(yA, yB));
    }

    private static void DrawMapObjectOutline(DrawingHandleScreen handle, ValueList<Vector2> mapObject, Color color)
    {
        var count = mapObject.Count;
        if (count < 2)
            return;

        for (var i = 0; i < count; i++)
            LunaDraw.Line(handle, mapObject[i], mapObject[(i + 1) % count], color);
    }

    private static void DrawBeaconGlyph(DrawingHandleScreen handle, Vector2 position, Color color)
    {
        LunaDraw.Circle(handle, position, 4.5f, color, false);
        LunaDraw.Circle(handle, position, 1.5f, color, true);
    }
}
