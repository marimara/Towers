/// <summary>
/// Shared state bag passed through the dialogue runner to conditions and commands.
/// Immutable references to the session data; mutable variables live in <see cref="Variables"/>.
/// </summary>
public class DialogueContext
{
    /// <summary>The asset currently being played.</summary>
    public DialogueData CurrentDialogue { get; }

    /// <summary>The node currently being processed. Set by DialogueRunner before executing commands.</summary>
    public DialogueNode CurrentNode { get; internal set; }

    /// <summary>Runtime variable store — shared across the entire session.</summary>
    public DialogueVariables Variables { get; }

    public DialogueContext(DialogueData dialogue, DialogueVariables variables)
    {
        CurrentDialogue = dialogue;
        Variables       = variables;
    }
}
