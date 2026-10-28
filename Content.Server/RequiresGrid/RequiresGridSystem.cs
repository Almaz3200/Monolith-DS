using Content.Server.Destructible;

namespace Content.Server.RequiresGrid;

public sealed partial class RequiresGridSystem : EntitySystem
{
    [Dependency] private DestructibleSystem _destructible = default!;

    private readonly HashSet<EntityUid> _pendingDestroy = new(); // LuaM

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<RequiresGridComponent, EntParentChangedMessage>(OnEntParentChanged);
    }

    private void OnEntParentChanged(EntityUid owner, RequiresGridComponent component, EntParentChangedMessage args)
    {
        if (args.OldParent == null)
            return;

        if (args.Transform.GridUid != null)
            return;

        if (TerminatingOrDeleted(owner))
            return;

        // LuaM-start
        _pendingDestroy.Add(owner);
    }

    public override void Update(float frameTime)
    {
        base.Update(frameTime);

        if (_pendingDestroy.Count == 0)
            return;

        var pending = new List<EntityUid>(_pendingDestroy);
        _pendingDestroy.Clear();

        foreach (var owner in pending)
        {
            if (TerminatingOrDeleted(owner) || Transform(owner).GridUid != null)
                continue;

            _destructible.DestroyEntity(owner);
        }
    }
    // LuaM-end
}
