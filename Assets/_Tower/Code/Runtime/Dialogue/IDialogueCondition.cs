/// <summary>
/// Evaluates whether a dialogue choice or node should be available.
///
/// Register implementations in a ConditionRegistry (future work).
/// Reference from DialogueChoice.ConditionId (currently commented out).
///
/// Example implementations: HasFlag, HasItem, QuestIsActive, VariableEquals.
/// </summary>
public interface IDialogueCondition
{
    /// <summary>
    /// Returns true if the condition passes and the choice/node should be shown.
    /// </summary>
    bool Evaluate(DialogueContext context);
}
