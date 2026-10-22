using System.Numerics;
using Content.Shared._LuaM.CCVar;
using Content.Shared._LuaM.RpPanel;
using Content.Shared.CCVar;
using Robust.Client.Animations;
using Robust.Client.GameObjects;
using Robust.Shared.Animations;
using Robust.Shared.Configuration;

namespace Content.Client._LuaM.RpPanel;

public sealed partial class RpLungeSystem : EntitySystem
{
    [Dependency] private AnimationPlayerSystem _animation = default!;
    [Dependency] private IConfigurationManager _configuration = default!;

    private const string LungeKey = "rp-lunge";

    private const float LungeDistance = 0.22f;

    private const float LeanInTime = 0.35f;

    private const float LungeLength = 0.9f;

    public override void Initialize()
    {
        base.Initialize();
        SubscribeNetworkEvent<RpLungeEvent>(OnLunge);
    }

    private void OnLunge(RpLungeEvent ev)
    {
        var user = GetEntity(ev.User);

        if (!Exists(user) || !HasComp<SpriteComponent>(user))
            return;

        if (ev.LocalPos.LengthSquared() <= 0f)
            return;

        if (_configuration.GetCVar(CCVars.ReducedMotion) || !_configuration.GetCVar(LuaMCVars.RpLungeEnabled))
            return;

        _animation.Stop(user, LungeKey);
        _animation.Play(user, GetLungeAnimation(ev.LocalPos), LungeKey);
    }

    private static Animation GetLungeAnimation(Vector2 direction)
    {
        var peak = direction.Normalized() * LungeDistance;

        return new Animation
        {
            Length = TimeSpan.FromSeconds(LungeLength),
            AnimationTracks =
            {
                new AnimationTrackComponentProperty
                {
                    ComponentType = typeof(SpriteComponent),
                    Property = nameof(SpriteComponent.Offset),
                    InterpolationMode = AnimationInterpolationMode.Cubic,
                    KeyFrames =
                    {
                        new AnimationTrackProperty.KeyFrame(Vector2.Zero, 0f),
                        new AnimationTrackProperty.KeyFrame(peak, LeanInTime),
                        new AnimationTrackProperty.KeyFrame(Vector2.Zero, LungeLength - LeanInTime),
                    },
                },
            },
        };
    }
}
