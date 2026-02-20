using Sirenix.OdinInspector;
using UnityEngine;

/// <summary>
/// A single player choice branching from a <see cref="DialogueNode"/>.
/// </summary>
[System.Serializable]
public class DialogueChoice
{
    [TextArea(2, 4)]
    public string Text;

    /// <summary>GUID of the <see cref="DialogueNode"/> to jump to when this choice is selected.</summary>
    public string NextNodeGuid;

    // -------------------------------------------------------------------------
    // Relationship requirements
    // -------------------------------------------------------------------------

    [FoldoutGroup("Relationship Requirements")]
    [Tooltip("If true, this choice is only available when relationship tier requirements are met.")]
    public bool RequiresRelationshipTier;

    [FoldoutGroup("Relationship Requirements")]
    [ShowIf(nameof(RequiresRelationshipTier))]
    [Tooltip("List of allowed tier names. Choice is available if current tier matches any in this list.")]
    public System.Collections.Generic.List<string> AllowedTiers = new();

    // -------------------------------------------------------------------------
    // Tier-based visibility conditions
    // -------------------------------------------------------------------------

    [FoldoutGroup("Tier Condition")]
    [Tooltip("If true, this choice visibility is determined by tier comparison logic.")]
    public bool RequiresTierCondition;

    [FoldoutGroup("Tier Condition")]
    [ShowIf(nameof(RequiresTierCondition))]
    [Tooltip("Logic operator for combining multiple tier conditions.")]
    public TierLogicOperator LogicOperator;

    [FoldoutGroup("Tier Condition")]
    [ShowIf(nameof(RequiresTierCondition))]
    [Tooltip("List of tier conditions. Combined using LogicOperator.")]
    [ListDrawerSettings(ShowFoldout = true)]
    public System.Collections.Generic.List<TierCondition> TierConditions = new();

    // -------------------------------------------------------------------------
    // Relationship effects
    // -------------------------------------------------------------------------

    [Tooltip("Relationship changes applied when this choice is selected.")]
    [ListDrawerSettings(ShowFoldout = true)]
    public System.Collections.Generic.List<RelationshipChange> RelationshipChanges = new();

    // Phase 5 hook — uncomment when conditions are implemented
    // public string ConditionId;
    // public string[] ConditionArgs;
}
