using System;
using UnityEngine;

/// <summary>
/// Consequence that modifies a player stat by a signed delta after an event completes.
///
/// Requires a <c>StatManager</c> singleton (or equivalent service) that exposes
/// <c>void ModifyStat(StatType type, int delta)</c>. Wire this up when the stat
/// system is implemented.
///
/// Pairs naturally with <see cref="StatCondition"/>: an event can lower or raise a
/// stat as a consequence so that subsequent condition checks against that same stat
/// immediately reflect the change during the recheck in <see cref="EventManager"/>.
/// </summary>
[Serializable]
public sealed class StatConsequence : EventConsequence
{
    // -------------------------------------------------------------------------
    // Data
    // -------------------------------------------------------------------------

    [Tooltip("Which player stat to modify.")]
    public StatType Stat;

    [Tooltip("Amount to add to the stat. Positive values increase it; negative values decrease it.")]
    public int Delta;

    // -------------------------------------------------------------------------
    // Execute
    // -------------------------------------------------------------------------

    protected override void Execute()
    {
        // TODO: replace with your StatManager singleton once implemented.
        // StatManager.Instance.ModifyStat(Stat, Delta);

        Debug.LogWarning($"[StatConsequence] StatManager not yet implemented. " +
                         $"Stat '{Stat}' delta {Delta:+#;-#;0} was not applied.");
    }

    // -------------------------------------------------------------------------
    // Debug
    // -------------------------------------------------------------------------

    public override string Describe() =>
        $"Modify Stat {Stat} by {Delta:+#;-#;0}";
}
