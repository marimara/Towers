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
    // Relationship effects
    // -------------------------------------------------------------------------

    [Tooltip("Enable relationship modification when this choice is selected.")]
    public bool HasRelationshipEffect;

    [FoldoutGroup("Relationship Effect")]
    [ShowIf(nameof(HasRelationshipEffect))]
    [Tooltip("Relationship value change applied when this choice is selected.")]
    public int RelationshipDelta;

    [FoldoutGroup("Relationship Effect")]
    [ShowIf(nameof(HasRelationshipEffect))]
    [Tooltip("If true, applies relationship delta between current node speaker and the other active character.")]
    public bool AutoApplyBetweenCurrentSpeakers;

    [FoldoutGroup("Relationship Effect")]
    [ShowIf(nameof(ShowOverrides))]
    [Tooltip("Optional override for the 'From' character. Used when AutoApplyBetweenCurrentSpeakers is false.")]
    public VNCharacter FromOverride;

    [FoldoutGroup("Relationship Effect")]
    [ShowIf(nameof(ShowOverrides))]
    [Tooltip("Optional override for the 'To' character. Used when AutoApplyBetweenCurrentSpeakers is false.")]
    public VNCharacter ToOverride;

    private bool ShowOverrides() => HasRelationshipEffect && !AutoApplyBetweenCurrentSpeakers;

    // Phase 5 hook — uncomment when conditions are implemented
    // public string ConditionId;
    // public string[] ConditionArgs;
}
