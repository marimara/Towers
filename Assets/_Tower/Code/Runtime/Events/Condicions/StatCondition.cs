using System;
using UnityEngine;

/// <summary>
/// Condition that checks whether a character's stat satisfies a numeric threshold
/// using a <see cref="ComparisonOperator"/>.
///
/// Works with <see cref="StatManager"/> singleton and queries the target character's stats.
/// Requires both a <see cref="VNCharacter"/> definition and <see cref="StatDefinition"/> to be assigned.
///
/// Design:
///   - Looks up the character instance in the scene by their VNCharacter definition.
///   - Queries the character's stat value via StatManager.
///   - Compares the stat value against the threshold using the configured operator.
///   - Guards against missing references, StatManager, or character instance gracefully.
/// </summary>
[Serializable]
public sealed class StatCondition : EventCondition
{
    // -------------------------------------------------------------------------
    // Data
    // -------------------------------------------------------------------------

    [Tooltip("The character whose stat to evaluate. This is matched against CharacterInstance components in the scene.")]
    public VNCharacter Character;

    [Tooltip("The stat definition to evaluate.")]
    public StatDefinition Stat;

    [Tooltip("How to compare the runtime stat value against the threshold.")]
    public ComparisonOperator Comparison;

    [Tooltip("Threshold the stat is compared against.")]
    public int Value;

    // -------------------------------------------------------------------------
    // Evaluate
    // -------------------------------------------------------------------------

    protected override bool Evaluate()
    {
        // --- Guard: Character reference ---
        if (Character == null)
        {
            Debug.LogWarning("[StatCondition] Character is null — condition will always fail.");
            return false;
        }

        // --- Guard: Stat reference ---
        if (Stat == null)
        {
            Debug.LogWarning("[StatCondition] Stat is null — condition will always fail.");
            return false;
        }

        // --- Guard: StatManager singleton ---
        var statManager = StatManager.Instance;
        if (statManager == null)
        {
            Debug.LogWarning("[StatCondition] StatManager instance not found — returning false.");
            return false;
        }

        // --- Find the character instance in the scene ---
        var characterInstance = FindCharacterInstance(Character);
        if (characterInstance == null)
        {
            Debug.LogWarning($"[StatCondition] Character '{Character.DisplayName}' instance not found in scene — returning false.");
            return false;
        }

        // --- Query the stat value ---
        int statValue = statManager.GetStat(characterInstance, Stat);

        // --- Apply the comparison ---
        return ApplyComparison(statValue, Comparison, Value);
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
            Debug.LogWarning("[StatCondition] CharacterRegistry not available — cannot find character instance.");
            return null;
        }

        return CharacterRegistry.Instance.GetInstance(definition);
    }

    // -------------------------------------------------------------------------
    // Helpers
    // -------------------------------------------------------------------------

    /// <summary>
    /// Apply the configured <see cref="ComparisonOperator"/> to two integers.
    /// Extracted so the logic can be unit-tested independently.
    /// </summary>
    public static bool ApplyComparison(int lhs, ComparisonOperator op, int rhs) => op switch
    {
        ComparisonOperator.Equal          => lhs == rhs,
        ComparisonOperator.GreaterOrEqual => lhs >= rhs,
        ComparisonOperator.LessOrEqual    => lhs <= rhs,
        _                                 => false,
    };

    // -------------------------------------------------------------------------
    // Debug
    // -------------------------------------------------------------------------

    private static string OperatorSymbol(ComparisonOperator op) => op switch
    {
        ComparisonOperator.Equal          => "==",
        ComparisonOperator.GreaterOrEqual => ">=",
        ComparisonOperator.LessOrEqual    => "<=",
        _                                 => "?",
    };

    public override string Describe()
    {
        string characterName = Character != null ? Character.DisplayName : "[Character not assigned]";
        string statName = Stat != null ? Stat.DisplayName : "[Stat not assigned]";
        return $"{characterName}'s {statName} {OperatorSymbol(Comparison)} {Value}";
    }
}
