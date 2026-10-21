using System.Linq;
using Content.Shared.Research.Prototypes;
using Robust.Shared.Prototypes;
using Robust.Shared.Serialization;

namespace Content.Shared._LuaM.Research;

/// <summary>
/// Asks the research console to unlock every technology of a discipline together with all of their prerequisites,
/// or of every discipline when <see cref="Discipline"/> is null.
/// </summary>
[Serializable, NetSerializable]
public sealed class ConsoleUnlockAllTechnologiesMessage(ProtoId<TechDisciplinePrototype>? discipline) : BoundUserInterfaceMessage
{
    public ProtoId<TechDisciplinePrototype>? Discipline = discipline;
}

/// <summary>
/// Asks the research console to unlock a technology together with every prerequisite it is still missing.
/// </summary>
[Serializable, NetSerializable]
public sealed class ConsoleUnlockWithPrerequisitesMessage(ProtoId<TechnologyPrototype> technology) : BoundUserInterfaceMessage
{
    public ProtoId<TechnologyPrototype> Technology = technology;
}

/// <summary>
/// Works out what "research everything" unlocks. Shared so the console can show the price the server will charge.
/// </summary>
public static class ResearchUnlockAll
{
    /// <summary>
    /// Technologies to unlock, prerequisites always before the technologies that need them.
    /// Technologies that can never be unlocked here (hidden, from an unsupported discipline, or needing such a
    /// technology) are left out together with everything that depends on them.
    /// </summary>
    /// <param name="discipline">Only technologies of this discipline and their prerequisites, or all if null.</param>
    public static List<TechnologyPrototype> Plan(
        IPrototypeManager prototypes,
        IReadOnlyCollection<string> unlocked,
        IReadOnlyCollection<string> supportedDisciplines,
        ProtoId<TechDisciplinePrototype>? discipline)
    {
        var state = new PlanState(prototypes, unlocked, supportedDisciplines);

        var targets = prototypes.EnumeratePrototypes<TechnologyPrototype>()
            .Where(t => discipline == null || t.Discipline == discipline.Value)
            .OrderBy(t => t.Tier)
            .ThenBy(t => t.ID, StringComparer.Ordinal);

        foreach (var tech in targets)
        {
            state.TryAdd(tech);
        }

        return state.Result;
    }

    /// <summary>
    /// The technology and every prerequisite it is still missing, prerequisites first.
    /// Empty if it is already unlocked or can never be unlocked here.
    /// </summary>
    public static List<TechnologyPrototype> PlanFor(
        IPrototypeManager prototypes,
        IReadOnlyCollection<string> unlocked,
        IReadOnlyCollection<string> supportedDisciplines,
        TechnologyPrototype technology)
    {
        var state = new PlanState(prototypes, unlocked, supportedDisciplines);
        state.TryAdd(technology);
        return state.Result;
    }

    public static int TotalCost(IEnumerable<TechnologyPrototype> technologies)
    {
        return technologies.Sum(t => t.Cost);
    }

    private sealed class PlanState(
        IPrototypeManager prototypes,
        IReadOnlyCollection<string> unlockedTechnologies,
        IReadOnlyCollection<string> supported)
    {
        public readonly List<TechnologyPrototype> Result = new();

        private readonly HashSet<string> _unlocked = new(unlockedTechnologies);
        private readonly HashSet<string> _supportedDisciplines = new(supported);

        private readonly HashSet<string> _planned = new();
        private readonly HashSet<string> _unreachable = new();
        private readonly HashSet<string> _visiting = new();

        /// <summary>
        /// Plans the technology after its prerequisites. False if it can't be unlocked.
        /// </summary>
        public bool TryAdd(TechnologyPrototype tech)
        {
            if (_unlocked.Contains(tech.ID) || _planned.Contains(tech.ID))
                return true;

            // Same rules as SharedResearchSystem.IsTechnologyAvailable, a loop in prerequisites counts as unreachable.
            if (_unreachable.Contains(tech.ID)
                || tech.Hidden
                || !_supportedDisciplines.Contains(tech.Discipline)
                || !_visiting.Add(tech.ID))
                return false;

            var plannedBefore = Result.Count;
            foreach (var prereqId in tech.TechnologyPrerequisites)
            {
                if (prototypes.TryIndex(prereqId, out var prereq) && TryAdd(prereq))
                    continue;

                // Don't pay for prerequisites that only led here, others that need them plan them again.
                for (var i = Result.Count - 1; i >= plannedBefore; i--)
                {
                    _planned.Remove(Result[i].ID);
                    Result.RemoveAt(i);
                }

                _visiting.Remove(tech.ID);
                _unreachable.Add(tech.ID);
                return false;
            }

            _visiting.Remove(tech.ID);
            _planned.Add(tech.ID);
            Result.Add(tech);
            return true;
        }
    }
}
