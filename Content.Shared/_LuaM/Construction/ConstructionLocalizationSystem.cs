using System.Diagnostics.CodeAnalysis;
using Content.Shared.Construction;
using Content.Shared.Construction.Prototypes;
using Robust.Shared.Prototypes;

namespace Content.Shared._LuaM.Construction;

/// <summary>
/// Localizes the names and descriptions of the construction recipes, the prototypes only have English text.
/// A recipe takes them from the locale keys <c>construction-ID</c> and <c>construction-ID-desc</c>,
/// then from the entity it builds, and keeps the text of the prototype if there is neither.
/// </summary>
public sealed partial class ConstructionLocalizationSystem : EntitySystem
{
    [Dependency] private IPrototypeManager _proto = default!;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<PrototypesReloadedEventArgs>(OnPrototypesReloaded);
        Localize();
    }

    private void OnPrototypesReloaded(PrototypesReloadedEventArgs args)
    {
        if (args.WasModified<ConstructionPrototype>() ||
            args.WasModified<ConstructionGraphPrototype>() ||
            args.WasModified<EntityPrototype>())
            Localize();
    }

    private void Localize()
    {
        foreach (var recipe in _proto.EnumeratePrototypes<ConstructionPrototype>())
        {
            var key = $"construction-{recipe.ID}";
            TryGetTarget(recipe, out var target);

            if (Loc.TryGetString(key, out var name))
                recipe.Name = name;
            else if (target != null)
                recipe.Name = target.Name;

            if (Loc.TryGetString($"{key}-desc", out var desc))
                recipe.Description = desc;
            else if (target != null)
                recipe.Description = target.Description;
        }
    }

    /// <summary>
    /// The entity the recipe builds, if its target node has a fixed one.
    /// </summary>
    private bool TryGetTarget(ConstructionPrototype recipe, [NotNullWhen(true)] out EntityPrototype? target)
    {
        target = null;
        if (!_proto.TryIndex<ConstructionGraphPrototype>(recipe.Graph, out var graph) ||
            !graph.Nodes.TryGetValue(recipe.TargetNode, out var node))
            return false;

        var id = node.Entity.GetId(null, null, new GraphNodeEntityArgs(EntityManager));
        return id != null && _proto.TryIndex(id, out target);
    }
}
