using System;
using UnityEngine;

/// <summary>
/// Condition that checks whether the relationship from one <see cref="VNCharacter"/>
/// to another has reached (or surpassed) a required tier index.
///
/// Tier index is resolved through <see cref="RelationshipSystem"/> and
/// <see cref="RelationshipTierConfig"/>: a higher index means a stronger relationship.
/// The check passes when the current tier index is greater than or equal to the
/// required tier index — meaning "at least this tier or better".
///
/// Both <see cref="From"/> and <see cref="To"/> must be assigned.
/// <see cref="RequiredTierName"/> must exactly match a tier name defined in
/// the <see cref="RelationshipTierConfig"/> assigned to <see cref="RelationshipSystem"/>.
/// </summary>
[Serializable]
public sealed class RelationshipTierCondition : EventCondition
{
    // -------------------------------------------------------------------------
    // Data
    // -------------------------------------------------------------------------

    [Tooltip("The character whose feelings are being evaluated (the relationship source).")]
    public VNCharacter From;

    [Tooltip("The character the relationship points toward (the relationship target).")]
    public VNCharacter To;

    [Tooltip("Name of the minimum tier required for this condition to pass " +
             "(e.g. \"Friend\", \"Ally\"). Must exactly match a tier in RelationshipTierConfig.")]
    public string RequiredTierName;

    // -------------------------------------------------------------------------
    // Evaluate
    // -------------------------------------------------------------------------

    protected override bool Evaluate()
    {
        if (From == null || To == null)
        {
            Debug.LogWarning("[RelationshipTierCondition] From or To character is null — condition will always fail.");
            return false;
        }

        if (string.IsNullOrEmpty(RequiredTierName))
        {
            Debug.LogWarning("[RelationshipTierCondition] RequiredTierName is empty — condition will always fail.");
            return false;
        }

        var system = RelationshipSystem.Instance;
        if (system == null)
        {
            Debug.LogWarning("[RelationshipTierCondition] RelationshipSystem instance not found.");
            return false;
        }

        // Resolve current tier name from the live relationship value.
        string currentTierName = system.GetRelationshipTier(From, To);

        // Resolve both tier names to indices for an ordinal comparison.
        int currentIndex  = system.GetTierIndex(currentTierName);
        int requiredIndex = system.GetTierIndex(RequiredTierName);

        if (currentIndex < 0)
        {
            Debug.LogWarning($"[RelationshipTierCondition] Current tier '{currentTierName}' not found in TierConfig.");
            return false;
        }

        if (requiredIndex < 0)
        {
            Debug.LogWarning($"[RelationshipTierCondition] Required tier '{RequiredTierName}' not found in TierConfig.");
            return false;
        }

        // Pass when the current tier is at least as high as the required tier.
        return currentIndex >= requiredIndex;
    }

    // -------------------------------------------------------------------------
    // Debug
    // -------------------------------------------------------------------------

    public override string Describe()
    {
        string fromName = From != null ? (From.DisplayName ?? From.name) : "?";
        string toName   = To   != null ? (To.DisplayName   ?? To.name)   : "?";
        return $"Relationship {fromName} → {toName} >= tier '{RequiredTierName}'";
    }
}
