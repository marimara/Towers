using Sirenix.OdinInspector;
using UnityEngine;

/// <summary>
/// A single tier condition for choice visibility.
/// </summary>
[System.Serializable]
public class TierCondition
{
    [Tooltip("Tier name to compare against from RelationshipTierConfig.")]
    public string RequiredTierName;

    [Tooltip("Comparison mode for tier check.")]
    public TierComparisonMode ComparisonMode;
}
