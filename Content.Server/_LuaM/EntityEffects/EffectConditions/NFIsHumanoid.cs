using Content.Shared.EntityEffects;
using Content.Shared.Humanoid;
using JetBrains.Annotations;
using Robust.Shared.Prototypes;

namespace Content.Server._LuaM.EntityEffects.EffectConditions;

[UsedImplicitly]
public sealed partial class NFIsHumanoid : EntityEffectCondition
{
    [DataField]
    public bool Invert;

    public override bool Condition(EntityEffectBaseArgs args)
    {
        return args.EntityManager.HasComponent<HumanoidAppearanceComponent>(args.TargetEntity) ^ Invert;
    }

    public override string GuidebookExplanation(IPrototypeManager prototype)
    {
        return Loc.GetString("reagent-effect-condition-guidebook-is-humanoid", ("invert", Invert));
    }
}
