using System.Numerics;
using Content.Client.Holopad;
using Content.Client.StatusIcon;
using Content.Shared.Holopad;
using Content.Shared.StatusIcon;
using Robust.Client.GameObjects;
using Robust.Client.Graphics;
using Robust.Client.UserInterface;
using Robust.Shared.Prototypes;
using Robust.Shared.Timing;
using Robust.Shared.Utility;

namespace Content.Client._LuaM.HoloComms;

/// <summary>
/// Draws the hologram of the other side the same way the holopads show it in the world:
/// the sprite goes into a render target first, then the hologram shader is put over the whole of it.
/// The one shown is framed like a portrait, from the head down to the hips.
/// With nobody to show it draws the holographic phone of the holopads instead.
/// The status icons of the hologram, like the SSD indicator of the one shown, go over it as they do in the world.
/// </summary>
public sealed partial class HoloCommsHologramView : Control
{
    [Dependency] private IEntityManager _entManager = default!;
    [Dependency] private IClyde _clyde = default!;
    [Dependency] private IPrototypeManager _prototype = default!;
    [Dependency] private IGameTiming _timing = default!;

    private static readonly ProtoId<ShaderPrototype> HologramShader = "Hologram";
    private static readonly TimeSpan RefreshInterval = TimeSpan.FromSeconds(0.5);

    // Same look as the holopad hologram, used for the phone before there is a hologram to take it from
    private static readonly Color DefaultColor1 = Color.FromHex("#65b8e2");
    private static readonly Color DefaultColor2 = Color.FromHex("#3a6981");
    private const float DefaultAlpha = 0.9f;
    private const float DefaultIntensity = 2f;
    private const float DefaultScrollRate = 0.125f;

    /// <summary>
    /// The part of the sprite height that is shown, a humanoid has the top of its head and its hips about here.
    /// </summary>
    private const float PortraitTop = 0.12f;
    private const float PortraitBottom = 0.66f;

    /// <summary>
    /// The part of the sprite width a humanoid takes, the rest of the sprite may go past the sides of the screen.
    /// </summary>
    private const float PortraitWidth = 0.6f;

    /// <summary>
    /// Status icons are drawn smaller than the portrait, at full scale they would cover the face.
    /// </summary>
    private const float IconScale = 0.4f;

    /// <summary>
    /// The phone takes this part of the screen.
    /// </summary>
    private const float PhoneFill = 0.45f;

    /// <summary>
    /// The holopad phone, used when the hologram has no default sprite of its own.
    /// </summary>
    private static readonly SpriteSpecifier.Rsi DefaultPhone =
        new(new ResPath("/Textures/Structures/Machines/holopad.rsi"), "icon_in_call");

    private readonly SpriteSystem _sprite;
    private readonly HolopadSystem _holopad;
    private readonly StatusIconSystem _statusIcon;
    private readonly SharedTransformSystem _xform;

    // Both are made on the first draw and freed when the view leaves the screen
    private ShaderInstance? _shader;
    private IRenderTexture? _target;
    private EntityUid? _hologram;
    private EntityUid? _shownEntity;
    private TimeSpan _nextRefresh;

    /// <summary>
    /// Show the phone when there is nobody on the hologram.
    /// </summary>
    public bool ShowPlaceholder { get; set; }

    public HoloCommsHologramView()
    {
        IoCManager.InjectDependencies(this);

        _sprite = _entManager.System<SpriteSystem>();
        _holopad = _entManager.System<HolopadSystem>();
        _statusIcon = _entManager.System<StatusIconSystem>();
        _xform = _entManager.System<SharedTransformSystem>();

        RectClipContent = true;
    }

    public void SetHologram(EntityUid? hologram)
    {
        if (_hologram == hologram)
            return;

        _hologram = hologram;
        _shownEntity = null;
        _nextRefresh = TimeSpan.Zero;
    }

    protected override void Draw(IRenderHandle handle)
    {
        base.Draw(handle);

        var size = PixelSize;
        if (size.X <= 0 || size.Y <= 0)
            return;

        var hologram = GetHologram();
        if (hologram == null && !ShowPlaceholder)
            return;

        EnsureTarget(size);
        var screen = handle.DrawingHandleScreen;
        float stripes;
        UIBox2? spriteBox = null;
        var scale = 1f;

        if (hologram != null && TryDrawSprite(handle, hologram.Value, size, out var drawnBox, out scale))
        {
            spriteBox = drawnBox;
            stripes = size.Y / scale; // One stripe per pixel of the sprite, like in the world
        }
        else if (ShowPlaceholder)
        {
            stripes = DrawPhone(handle, size);
        }
        else
        {
            return;
        }

        _shader ??= _prototype.Index(HologramShader).InstanceUnique();
        SetShaderParameters(_shader, hologram?.Comp, stripes);

        screen.UseShader(_shader);
        screen.DrawTextureRect(_target!.Texture, UIBox2.FromDimensions(Vector2.Zero, size));
        screen.UseShader(null);

        if (hologram != null && spriteBox != null)
            DrawStatusIcons(screen, hologram.Value, spriteBox.Value, size, scale);
    }

    protected override void ExitedTree()
    {
        base.ExitedTree();

        _target?.Dispose();
        _target = null;
        _shader?.Dispose();
        _shader = null;
    }

    private Entity<HolopadHologramComponent>? GetHologram()
    {
        if (_hologram is not { } uid ||
            !_entManager.TryGetComponent<HolopadHologramComponent>(uid, out var comp) ||
            comp.LinkedEntity == null)
        {
            return null;
        }

        // The hologram is hidden in the world, so nothing rebuilds its sprite unless asked to
        var now = _timing.CurTime;
        if (_shownEntity != comp.LinkedEntity || now >= _nextRefresh)
        {
            _shownEntity = comp.LinkedEntity;
            _nextRefresh = now + RefreshInterval;
            _holopad.RefreshHologram((uid, comp));
        }

        return (uid, comp);
    }

    private bool TryDrawSprite(IRenderHandle handle, EntityUid hologram, Vector2i size, out UIBox2 spriteBox, out float scale)
    {
        spriteBox = default;
        scale = 1f;

        if (!_entManager.TryGetComponent<SpriteComponent>(hologram, out var sprite) ||
            !_entManager.TryGetComponent<TransformComponent>(hologram, out var xform))
        {
            return false;
        }

        var bounds = _sprite.GetLocalBounds((hologram, sprite));
        if (bounds.Width <= 0f || bounds.Height <= 0f)
            return false;

        // The largest whole scale that fits the portrait keeps the pixels square
        var pixelsPerMeter = EyeManager.PixelsPerMeter;
        var spriteSize = bounds.Size * pixelsPerMeter;
        var fit = MathF.Min(size.X / (spriteSize.X * PortraitWidth), size.Y / (spriteSize.Y * (PortraitBottom - PortraitTop)));
        scale = MathF.Max(1f, MathF.Floor(fit));

        // The head goes to the top of the screen, whatever is below the hips stays under its bottom edge
        var drawnSize = spriteSize * scale;
        spriteBox = UIBox2.FromDimensions(
            new Vector2((size.X - drawnSize.X) / 2f, -drawnSize.Y * PortraitTop),
            drawnSize);

        // The offset of the hologram only lifts it above the holopad in the world
        var center = bounds.Center + sprite.Offset;
        var position = spriteBox.Center - new Vector2(center.X, -center.Y) * pixelsPerMeter * scale;

        _sprite.ForceUpdate(hologram);

        var drawScale = scale;
        handle.DrawingHandleScreen.RenderInRenderTarget(_target!, () =>
        {
            ResetTargetState(handle);
            handle.DrawEntity(hologram, position, new Vector2(drawScale), Angle.Zero, Angle.Zero, Direction.South, sprite, xform, _xform);
        }, Color.Transparent);

        return true;
    }

    /// <summary>
    /// Same places as in the world: the top corners, the base icons stacked down from there.
    /// The corners are those of the part of the sprite on the screen, the rest of it is cut off.
    /// </summary>
    private void DrawStatusIcons(DrawingHandleScreen screen, EntityUid hologram, UIBox2 spriteBox, Vector2i size, float scale)
    {
        var icons = _statusIcon.GetStatusIcons(hologram);
        if (icons.Count == 0)
            return;

        icons.Sort();
        var left = 0f;
        var right = 0f;
        scale = MathF.Max(1f, MathF.Floor(scale * IconScale));

        // The character takes the middle of its sprite, the icons go at its sides
        var shownWidth = MathF.Min(size.X, spriteBox.Width * PortraitWidth);
        spriteBox = UIBox2.FromDimensions(
            new Vector2((size.X - shownWidth) / 2f, MathF.Max(spriteBox.Top, 0f) + 4f),
            new Vector2(shownWidth, spriteBox.Height));

        foreach (var icon in icons)
        {
            var texture = _sprite.GetFrame(icon.Icon, _timing.RealTime);
            var iconSize = (Vector2) texture.Size * scale;
            var onRight = icon.LocationPreference == StatusIconLocationPreference.Right;
            var position = onRight
                ? new Vector2(spriteBox.Right - iconSize.X, spriteBox.Top + right)
                : new Vector2(spriteBox.Left, spriteBox.Top + left);

            screen.DrawTextureRect(texture, UIBox2.FromDimensions(position, iconSize));

            if (icon.Layer != StatusIconLayer.Base)
                continue;

            if (onRight)
                right += iconSize.Y;
            else
                left += iconSize.Y;
        }
    }

    /// <summary>
    /// The phone the holopads show while nobody stands at the other end.
    /// </summary>
    private float DrawPhone(IRenderHandle handle, Vector2i size)
    {
        var specifier = DefaultPhone;
        if (_hologram is { } uid &&
            _entManager.TryGetComponent<HolopadHologramComponent>(uid, out var comp) &&
            !string.IsNullOrEmpty(comp.RsiPath) && !string.IsNullOrEmpty(comp.RsiState))
        {
            specifier = new SpriteSpecifier.Rsi(new ResPath("/Textures") / comp.RsiPath, comp.RsiState);
        }

        var texture = _sprite.Frame0(specifier);
        var fit = MathF.Min(size.X, size.Y) * PhoneFill / MathF.Max(texture.Width, texture.Height);
        var scale = MathF.Max(1f, MathF.Floor(fit));
        var drawnSize = (Vector2) texture.Size * scale;
        var box = UIBox2.FromDimensions(((Vector2) size - drawnSize) / 2f, drawnSize);

        // Slowly pulses while waiting
        var pulse = 0.75f + 0.25f * MathF.Sin((float) _timing.RealTime.TotalSeconds * 3f);

        var screen = handle.DrawingHandleScreen;
        screen.RenderInRenderTarget(_target!, () =>
        {
            ResetTargetState(handle);
            screen.DrawTextureRect(texture, box, Color.White.WithAlpha(pulse));
        }, Color.Transparent);

        return size.Y / scale; // One stripe per pixel of the phone, like in the world
    }

    /// <summary>
    /// The UI draws with the window offset and clip, inside the target everything starts from its corner.
    /// The previous state comes back once the target is done.
    /// </summary>
    private static void ResetTargetState(IRenderHandle handle)
    {
        handle.SetScissor(null);
        handle.DrawingHandleScreen.SetTransform(Matrix3x2.Identity);
    }

    private void SetShaderParameters(ShaderInstance shader, HolopadHologramComponent? hologram, float stripes)
    {
        var color1 = hologram?.Color1 ?? DefaultColor1;
        var color2 = hologram?.Color2 ?? DefaultColor2;
        var scrollRate = hologram?.ScrollRate ?? DefaultScrollRate;

        shader.SetParameter("color1", new Vector3(color1.R, color1.G, color1.B));
        shader.SetParameter("color2", new Vector3(color2.R, color2.G, color2.B));
        shader.SetParameter("alpha", hologram?.Alpha ?? DefaultAlpha);
        shader.SetParameter("intensity", hologram?.Intensity ?? DefaultIntensity);
        shader.SetParameter("texHeight", stripes);
        shader.SetParameter("t", (float) _timing.CurTime.TotalSeconds * scrollRate);
    }

    private void EnsureTarget(Vector2i size)
    {
        if (_target != null && _target.Size == size)
            return;

        _target?.Dispose();
        _target = _clyde.CreateRenderTarget(size,
            new RenderTargetFormatParameters(RenderTargetColorFormat.Rgba8Srgb, true),
            name: nameof(HoloCommsHologramView));
    }
}
