using System;
using System.Collections.Generic;

/// <summary>
/// Decouples the dialogue runner's flow logic from all Unity UI concerns.
///
/// Implement this on a MonoBehaviour (e.g. VNDialoguePresenter) and assign it
/// to DialogueRunner. Swap implementations freely (VN, cutscene, minimap, etc.)
/// without touching the runner.
/// </summary>
public interface IDialoguePresenter
{
    /// <summary>
    /// Display a dialogue node's content (text, speaker name, actor highlighting).
    /// Called before ShowChoices or HideChoices.
    /// </summary>
    void PresentNode(DialogueNode node, SpeakerConfig config);

    /// <summary>
    /// Show the choice buttons for a branching node.
    /// <paramref name="onChosen"/> is called with the NextNodeGuid of the selected choice.
    /// </summary>
    void ShowChoices(List<DialogueChoice> choices, Action<string> onChosen);

    /// <summary>Hide the choice UI (used for linear nodes).</summary>
    void HideChoices();

    /// <summary>Called when the dialogue sequence ends. Hide/reset UI here.</summary>
    void OnDialogueEnd();
}
