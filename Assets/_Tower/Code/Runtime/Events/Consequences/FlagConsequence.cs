using System;
using UnityEngine;

/// <summary>
/// Consequence that sets a named boolean flag in the game state to a specified value.
///
/// Writes flag state via <see cref="FlagManager.SetFlag"/>.
///
/// Pairs naturally with <see cref="FlagCondition"/>: an event can set a flag as
/// a consequence so that a subsequent event whose condition checks that same flag
/// becomes eligible immediately after the recheck in <see cref="EventManager"/>.
/// </summary>
[Serializable]
public sealed class FlagConsequence : EventConsequence
{
    // -------------------------------------------------------------------------
    // Data
    // -------------------------------------------------------------------------

    [Tooltip("The flag definition to set. The flag's DisplayName is used in logs and descriptions.")]
    public FlagDefinition Flag;

    [Tooltip("The value to write to the flag.\n" +
             "• true  → set the flag\n" +
             "• false → clear the flag")]
    public bool Value = true;

    // -------------------------------------------------------------------------
    // Execute
    // -------------------------------------------------------------------------

    protected override void Execute()
    {
        if (Flag == null)
        {
            Debug.LogWarning("[FlagConsequence] Flag is null — consequence will not write anything.");
            return;
        }

        if (string.IsNullOrEmpty(Flag.Id))
        {
            Debug.LogWarning($"[FlagConsequence] Flag '{Flag.DisplayName}' has no ID — consequence will not write anything.");
            return;
        }

        var manager = FlagManager.Instance;

        if (manager == null)
        {
            Debug.LogWarning($"[FlagConsequence] FlagManager instance not found. Flag '{Flag.DisplayName}' = {Value} was not written.");
            return;
        }

        manager.SetFlag(Flag.Id, Value);
    }

    // -------------------------------------------------------------------------
    // Debug
    // -------------------------------------------------------------------------

    public override string Describe() =>
        Flag != null
            ? $"Set Flag '{Flag.DisplayName}' = {Value}"
            : "[Flag not assigned]";
}
