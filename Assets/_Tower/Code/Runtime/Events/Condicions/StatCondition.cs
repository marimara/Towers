using System;
using UnityEngine;

/// <summary>
/// Condition that checks whether a player stat satisfies a numeric threshold
/// using a <see cref="ComparisonOperator"/>.
///
/// Requires a <c>StatManager</c> singleton (or equivalent service) that exposes
/// <c>int GetStat(StatType type)</c>. Wire this up when the stat system is implemented.
/// </summary>
[Serializable]
public sealed class StatCondition : EventCondition
{
    // -------------------------------------------------------------------------
    // Data
    // -------------------------------------------------------------------------

    [Tooltip("Which player stat to evaluate.")]
    public StatType StatType;

    [Tooltip("How to compare the runtime stat value against the threshold.")]
    public ComparisonOperator Comparison;

    [Tooltip("Threshold the stat is compared against.")]
    public int Value;

    // -------------------------------------------------------------------------
    // Evaluate
    // -------------------------------------------------------------------------

    protected override bool Evaluate()
    {
        // TODO: replace with your StatManager singleton once implemented.
        // int statValue = StatManager.Instance.GetStat(StatType);
        // return ApplyComparison(statValue, Value);

        Debug.LogWarning($"[StatCondition] StatManager not yet implemented. Stat '{StatType}' check skipped — returning false.");
        return false;
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

    public override string Describe() =>
        $"Stat {StatType} {OperatorSymbol(Comparison)} {Value}";
}
