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

    [Tooltip("The flag definition to check. The flag's DisplayName is used in logs and descriptions.")]
    public FlagDefinition Flag;

    [Tooltip("The value the flag must hold for this condition to pass.\n" +
             "• true  → flag must be set\n" +
             "• false → flag must be cleared")]
    public bool ExpectedValue = true;

    // -------------------------------------------------------------------------
    // Evaluate
    // -------------------------------------------------------------------------

    protected override bool Evaluate()
    {
        if (Flag == null)
        {
            Debug.LogWarning("[FlagCondition] Flag is null — condition will always fail.");
            return false;
        }

        if (string.IsNullOrEmpty(Flag.Id))
        {
            Debug.LogWarning($"[FlagCondition] Flag '{Flag.DisplayName}' has no ID — condition will always fail.");
            return false;
        }

        var manager = FlagManager.Instance;

        if (manager == null)
        {
            Debug.LogWarning($"[FlagCondition] FlagManager instance not found. Flag '{Flag.DisplayName}' check skipped — returning false.");
            return false;
        }

        bool current = manager.GetFlag(Flag.Id);
        return current == ExpectedValue;
    }

    // -------------------------------------------------------------------------
    // Debug
    // -------------------------------------------------------------------------

    public override string Describe() =>
        Flag != null
            ? $"Flag '{Flag.DisplayName}' {(Negate ? "!=" : "==")} {ExpectedValue}"
            : "[Flag not assigned]";
}
