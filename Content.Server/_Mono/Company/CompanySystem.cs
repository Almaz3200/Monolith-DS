using Content.Shared._Mono.Company;
using Content.Shared.Access.Components;
using Content.Shared.Access.Systems;
using Content.Shared.GameTicking;
using Content.Shared.Inventory;
using Content.Shared.PDA;
using Content.Shared.Roles;
using Robust.Shared.Player;
using Robust.Shared.Prototypes;

namespace Content.Server._Mono.Company;

/// <summary>
/// This system handles assigning a company to players when they join.
/// </summary>
public sealed partial class CompanySystem : EntitySystem
{
    [Dependency] private IPrototypeManager _prototypeManager = default!;
    [Dependency] private SharedIdCardSystem _idCardSystem = default!;
    [Dependency] private InventorySystem _inventorySystem = default!;
    [Dependency] private CompanyManager _manager = default!;

    public override void Initialize()
    {
        base.Initialize();

        // Subscribe to player spawn event to add the company component
        SubscribeLocalEvent<PlayerSpawnCompleteEvent>(OnPlayerSpawnComplete);
    }

    private void OnPlayerSpawnComplete(PlayerSpawnCompleteEvent args)
    {
        // Add the company component with the player's saved company
        var companyComp = EnsureComp<CompanyComponent>(args.Mob);
        companyComp.CompanyName = GetSpawnCompany(args);

        // Ensure the component is networked to clients
        Dirty(args.Mob, companyComp);

        // Update the player's ID card with the company information
        UpdateIdCardCompany(args.Mob, companyComp.CompanyName);
    }

    private ProtoId<CompanyPrototype> GetSpawnCompany(PlayerSpawnCompleteEvent args)
    {
        if (args.JobId != null)
        {
            var jobCompany = _prototypeManager.Index<JobPrototype>(args.JobId).AssignedCompany;
            if (jobCompany != CompanyPrototype.NoCompany)
                return jobCompany;
        }

        var profileCompany = args.Profile.Company;
        if (!string.IsNullOrEmpty(profileCompany))
            return profileCompany;

        foreach (var companyProto in _prototypeManager.EnumeratePrototypes<CompanyPrototype>())
        {
            if (companyProto.Whitelisted && _manager.IsMember(args.Player.UserId, companyProto.ID))
                return companyProto.ID;
        }

        return CompanyPrototype.NoCompany;
    }

    /// <summary>
    /// Updates the player's ID card with their company information
    /// </summary>
    private void UpdateIdCardCompany(EntityUid playerEntity, ProtoId<CompanyPrototype> companyName)
    {
        // Try to get the player's ID card
        if (!_inventorySystem.TryGetSlotEntity(playerEntity, "id", out var idUid))
            return;

        var cardId = idUid.Value;

        // Check if it's a PDA with an ID card inside
        if (TryComp<PdaComponent>(idUid, out var pdaComponent) && pdaComponent.ContainedId != null)
            cardId = pdaComponent.ContainedId.Value;

        // Update the ID card with company information
        if (TryComp<IdCardComponent>(cardId, out var idCard))
        {
            _idCardSystem.TryChangeCompanyName(cardId, companyName, idCard);
        }
    }
}
