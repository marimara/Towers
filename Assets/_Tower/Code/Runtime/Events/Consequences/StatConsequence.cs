using System;
using UnityEngine;

/// <summary>
/// Consequence that modifies a character's stat by a signed delta after an event completes.
///
/// Works with <see cref="StatManager"/> singleton and modifies the target character's stats.
/// Requires both a <see cref="VNCharacter"/> definition and <see cref="StatDefinition"/> to be assigned.
///
/// Design:
///   - Looks up the character instance in the scene by their VNCharacter definition.
///   - Calls StatManager.ModifyStat() to update the character's stat by the given delta.
///   - Pairs naturally with <see cref="StatCondition"/>: an event can lower or raise a
///     stat as a consequence so that subsequent condition checks against that same stat
///     immediately reflect the change during the recheck in <see cref="EventManager"/>.
/// </summary>
[Serializable]
public sealed class StatConsequence : EventConsequence
{
    // -------------------------------------------------------------------------
    // Data
    // -------------------------------------------------------------------------

    [Tooltip("The character whose stat to modify. This is matched against CharacterInstance components in the scene.")]
    public VNCharacter Character;

    [Tooltip("The stat definition to modify.")]
    public StatDefinition Stat;

    [Tooltip("Amount to add to the stat. Positive values increase it; negative values decrease it.")]
    public int Delta;

    // -------------------------------------------------------------------------
    // Execute
    // -------------------------------------------------------------------------

    protected override void Execute()
    {
        // --- Guard: Character reference ---
        if (Character == null)
        {
            Debug.LogWarning("[StatConsequence] Character is null — consequence skipped.");
            return;
        }

        // --- Guard: Stat reference ---
        if (Stat == null)
        {
            Debug.LogWarning("[StatConsequence] Stat is null — consequence skipped.");
            return;
        }

        // --- Guard: StatManager singleton ---
        var statManager = StatManager.Instance;
        if (statManager == null)
        {
            Debug.LogWarning("[StatConsequence] StatManager instance not found — consequence skipped.");
            return;
        }

        // --- Find the character instance in the scene ---
        var characterInstance = FindCharacterInstance(Character);
        if (characterInstance == null)
        {
            Debug.LogWarning($"[StatConsequence] Character '{Character.DisplayName}' instance not found in scene — consequence skipped.");
            return;
        }

        // --- Modify the stat ---
        if (Delta != 0)
        {
            statManager.ModifyStat(characterInstance, Stat, Delta);
        }
    }

    /// <summary>
    /// Find a CharacterInstance for the given VNCharacter definition using CharacterRegistry.
    /// Returns null if not found or registry unavailable.
    /// </summary>
    private static CharacterInstance FindCharacterInstance(VNCharacter definition)
    {
        if (definition == null)
            return null;

        // Query CharacterRegistry for the character instance
        if (CharacterRegistry.Instance == null)
        {
            Debug.LogWarning("[StatConsequence] CharacterRegistry not available — cannot find character instance.");
            return null;
        }

        return CharacterRegistry.Instance.GetInstance(definition);
    }

    // -------------------------------------------------------------------------
    // Debug
    // -------------------------------------------------------------------------

    public override string Describe()
    {
        string characterName = Character != null ? Character.DisplayName : "[Character not assigned]";
        string statName = Stat != null ? Stat.DisplayName : "[Stat not assigned]";
        return $"Modify {characterName}'s {statName} by {Delta:+#;-#;0}";
    }
}
