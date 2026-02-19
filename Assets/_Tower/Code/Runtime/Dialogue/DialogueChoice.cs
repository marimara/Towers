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

    // Phase 5 hook — uncomment when conditions are implemented
    // public string ConditionId;
    // public string[] ConditionArgs;
}
