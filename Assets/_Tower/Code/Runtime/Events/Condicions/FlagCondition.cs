using System;
using UnityEngine;

/// <summary>
/// Condition that checks whether a named boolean flag in the game state
/// matches an expected value.
///
/// Reads flag state from <see cref="FlagManager.GetFlag"/>.
/// </summary>
[Serializable]
public sealed class FlagCondition : EventCondition
{
    // -------------------------------------------------------------------------
    // Data
    // -------------------------------------------------------------------------

    [Tooltip("Unique identifier of the boolean flag to check (e.g. \"met_elara\", \"quest_started\").")]
    public string FlagId;

    [Tooltip("The value the flag must hold for this condition to pass.\n" +
             "• true  → flag must be set\n" +
             "• false → flag must be cleared")]
    public bool ExpectedValue = true;

    // -------------------------------------------------------------------------
    // Evaluate
    // -------------------------------------------------------------------------

    protected override bool Evaluate()
    {
        if (string.IsNullOrEmpty(FlagId))
        {
            Debug.LogWarning("[FlagCondition] FlagId is empty — condition will always fail.");
            return false;
        }

        var manager = FlagManager.Instance;

        if (manager == null)
        {
            Debug.LogWarning($"[FlagCondition] FlagManager instance not found. Flag '{FlagId}' check skipped — returning false.");
            return false;
        }

        bool current = manager.GetFlag(FlagId);
        return current == ExpectedValue;
    }

    // -------------------------------------------------------------------------
    // Debug
    // -------------------------------------------------------------------------

    public override string Describe() =>
        $"Flag '{FlagId}' {(Negate ? "!=" : "==")} {ExpectedValue}";
}
