using Content.Shared._LuaM.Movement; // LuaM
using Content.Shared.Gravity;
using Content.Shared.Movement.Components;
using Content.Shared.Throwing;
using Robust.Shared.Audio.Systems;

namespace Content.Shared.Movement.Systems;

public sealed partial class SharedJumpAbilitySystem : EntitySystem
{
    [Dependency] private ThrowingSystem _throwing = default!;
    [Dependency] private SharedAudioSystem _audio = default!;
    [Dependency] private SharedGravitySystem _gravity = default!;
    [Dependency] private DashImpactSystem _dashImpact = default!; // LuaM

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<JumpAbilityComponent, GravityJumpEvent>(OnGravityJump);
    }

    private void OnGravityJump(Entity<JumpAbilityComponent> entity, ref GravityJumpEvent args)
    {
        if (_gravity.IsWeightless(args.Performer))
            return;

        var xform = Transform(args.Performer);
        var throwing = xform.LocalRotation.ToWorldVec() * entity.Comp.JumpDistance;
        var direction = xform.Coordinates.Offset(throwing); // to make the character jump in the direction he's looking


        _throwing.TryThrow(args.Performer, direction, entity.Comp.JumpThrowSpeed);
        _dashImpact.StartImpact(args.Performer); // LuaM

        _audio.PlayPredicted(entity.Comp.JumpSound, args.Performer, args.Performer);
        args.Handled = true;
    }
}