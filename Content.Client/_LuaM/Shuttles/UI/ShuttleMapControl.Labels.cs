using System.Numerics;
using Content.Shared._Mono.Company;
using Content.Shared.Shuttles.UI.MapObjects;
using Robust.Client.Graphics;
using Robust.Client.ResourceManagement;
using Robust.Shared.Map;

namespace Content.Client.Shuttles.UI;

/// <summary>
/// Smaller sector map labels; hovered, selected or searched objects keep the full size.
/// </summary>
public sealed partial class ShuttleMapControl
{
    private const int MapLabelSmallFontSize = 7;
    private const float MapLabelHoverRadius = 18f;

    private Font? _mapLabelSmallFont;
    private EntityUid? _selectedMapGrid;
    private NetEntity? _selectedMapBeacon;
    private string _mapSearch = string.Empty;

    private Font MapLabelSmallFont => _mapLabelSmallFont ??= new VectorFont(
        IoCManager.Resolve<IResourceCache>().GetResource<FontResource>("/Fonts/NotoSans/NotoSans-Regular.ttf"),
        MapLabelSmallFontSize);

    /// <summary>
    /// Keeps the label of this object at full size, null clears the selection.
    /// </summary>
    public void SelectMapObject(IMapObject? mapObject)
    {
        _selectedMapGrid = mapObject is GridMapObject grid ? grid.Entity : null;
        _selectedMapBeacon = mapObject is ShuttleBeaconObject beacon ? beacon.Entity : null;
    }

    private bool IsSelectedMapObject(IMapObject mapObject)
    {
        return mapObject switch
        {
            GridMapObject grid => grid.Entity == _selectedMapGrid,
            ShuttleBeaconObject beacon => beacon.Entity == _selectedMapBeacon,
            _ => false,
        };
    }

    /// <summary>
    /// Mouse position inside the map, or null when the mouse is elsewhere.
    /// </summary>
    private Vector2? GetMapLabelMouse()
    {
        var mousePos = _inputs.MouseScreenPosition;
        if (mousePos.Window == WindowId.Invalid)
            return null;

        var local = GetLocalPosition(mousePos);
        return UIBox2.FromDimensions(Vector2.Zero, PixelSize).Contains(local) ? local : null;
    }

    /// <summary>
    /// Whether the mouse is over the object marker or where its full size label would be.
    /// </summary>
    private bool IsMapLabelHovered(DrawingHandleScreen handle, Vector2 markerPos, string label, Vector2? mouse)
    {
        if (mouse is not { } mousePos)
            return false;

        if ((mousePos - markerPos).Length() <= MapLabelHoverRadius * UIScale)
            return true;

        var size = handle.GetDimensions(_font, label, 1f);
        var topLeft = markerPos + new Vector2(-size.X / 2f, size.Y * UIScale);
        return UIBox2.FromDimensions(topLeft, size).Contains(mousePos);
    }

    /// <summary>
    /// Labels that match the object search keep the full size.
    /// </summary>
    public void SetMapSearch(string search)
    {
        _mapSearch = search.Trim();
    }

    private bool MatchesMapSearch(string label)
    {
        return _mapSearch.Length > 0 && label.Contains(_mapSearch, StringComparison.CurrentCultureIgnoreCase);
    }

    /// <summary>
    /// Color of the grid's company, the same one the radar uses for its label.
    /// </summary>
    private bool TryGetCompanyColor(EntityUid grid, out Color color)
    {
        color = default;
        if (!EntManager.TryGetComponent(grid, out CompanyComponent? company)
            || string.IsNullOrEmpty(company.CompanyName)
            || !PrototypeManager.TryIndex<CompanyPrototype>(company.CompanyName, out var prototype))
            return false;

        color = prototype.Color;
        return true;
    }

    private static void DrawSoftMapText(DrawingHandleScreen handle, Font font, string text, Vector2 position, float scale, Color color, Color shadow)
    {
        handle.DrawString(font, position + new Vector2(1f, 1f), text, scale, shadow);
        handle.DrawString(font, position, text, scale, color);
    }
}
