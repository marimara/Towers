using System;
using UnityEngine;

/// <summary>
/// Consequence that sets a named boolean flag in the game state to a specified value.
///
/// Requires a <c>FlagManager</c> singleton (or equivalent service) that exposes
/// <c>void SetFlag(string flagId, bool value)</c>. Wire this up when the flag
/// system is implemented.
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

    [Tooltip("Unique identifier of the boolean flag to set " +
             "(e.g. \"met_elara\", \"quest_started\"). Must match the ID used in FlagCondition.")]
    public string FlagId;

    [Tooltip("The value to write to the flag.\n" +
             "• true  → set the flag\n" +
             "• false → clear the flag")]
    public bool Value = true;

    // -------------------------------------------------------------------------
    // Execute
    // -------------------------------------------------------------------------

    protected override void Execute()
    {
        if (string.IsNullOrEmpty(FlagId))
        {
            Debug.LogWarning("[FlagConsequence] FlagId is empty — consequence will not write anything.");
            return;
        }

        // TODO: replace with your FlagManager singleton once implemented.
        // FlagManager.Instance.SetFlag(FlagId, Value);

        Debug.LogWarning($"[FlagConsequence] FlagManager not yet implemented. " +
                         $"Flag '{FlagId}' = {Value} was not written.");
    }

    // -------------------------------------------------------------------------
    // Debug
    // -------------------------------------------------------------------------

    public override string Describe() =>
        $"Set Flag '{FlagId}' = {Value}";
}
