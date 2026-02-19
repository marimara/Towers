using System.Collections.Generic;
using Sirenix.OdinInspector;
using UnityEngine;
using UnityEngine.Serialization;

/// <summary>
/// A single node in a dialogue graph.
/// Identified by a stable GUID so references survive deletion and reordering.
/// EditorPosition is no longer stored here — see DialogueNodeEditorData (Editor only).
/// </summary>
[System.Serializable]
public class DialogueNode
{
    // -------------------------------------------------------------------------
    // Identity
    // -------------------------------------------------------------------------

    /// <summary>Stable unique identifier. Assigned once at creation, never changed.</summary>
    [ReadOnly, HorizontalGroup("Header", Width = 230)]
    [LabelText("GUID")]
    public string Guid;

    /// <summary>Human-readable label shown in the graph node title and Odin TableList.</summary>
    [HorizontalGroup("Header")]
    [LabelText("Name")]
    public string DisplayName;

    // -------------------------------------------------------------------------
    // Content
    // -------------------------------------------------------------------------

    [HorizontalGroup("Content", Width = 120)]
    [LabelText("Speaker")]
    [FormerlySerializedAs("ActorSide")]
    public VNCharacter Speaker;

    [TextArea(3, 6), HideLabel]
    public string Text;

    // -------------------------------------------------------------------------
    // Branching
    // -------------------------------------------------------------------------

    /// <summary>
    /// Choices presented to the player when this node is active.
    /// When non-empty, <see cref="NextNodeGuid"/> is ignored.
    /// </summary>
    [ListDrawerSettings(Expanded = true)]
    public List<DialogueChoice> Choices = new();

    /// <summary>
    /// GUID of the next node for linear (no-choice) flow.
    /// Null or empty signals end-of-dialogue.
    /// Active only when <see cref="Choices"/> is empty.
    /// </summary>
    [ShowIf(nameof(HasNoChoices))]
    [LabelText("Next Node GUID")]
    public string NextNodeGuid;

    // -------------------------------------------------------------------------
    // Phase 5 hooks — uncomment when conditions/commands are implemented
    // -------------------------------------------------------------------------
    // public List<string> CommandIds = new();

    // -------------------------------------------------------------------------
    // Helpers
    // -------------------------------------------------------------------------

    public bool IsLinear => Choices == null || Choices.Count == 0;

    private bool HasNoChoices() => IsLinear;
}
