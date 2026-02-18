/// <summary>
/// Executes a side effect when a dialogue node is entered.
///
/// Register implementations in a CommandRegistry (future work).
/// Reference from DialogueNode.CommandIds (currently commented out).
///
/// Example implementations: SetFlag, GiveItem, StartQuest, PlaySound, TriggerAnimation.
/// </summary>
public interface IDialogueCommand
{
    /// <summary>
    /// Execute the command. Called by DialogueRunner before presenting the node.
    /// </summary>
    void Execute(DialogueContext context);
}
