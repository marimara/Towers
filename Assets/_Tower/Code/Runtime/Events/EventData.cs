using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// ScriptableObject describing a discrete game event: a story beat, encounter,
/// or triggered interaction that the event system can evaluate and fire.
///
/// Responsibilities:
///   - Carry all designer-authored data for one event (identity, dialogue,
///     conditions, and consequences).
///
/// ID is auto-generated and managed by the base UniqueIdScriptableObject class.
/// The event system reads this data; no game logic runs inside this class.
/// </summary>
[CreateAssetMenu(menuName = "Game/Event Data")]
public class EventData : UniqueIdScriptableObject
{
    // -------------------------------------------------------------------------
    // Display
    // -------------------------------------------------------------------------

    [Tooltip("Human-readable name displayed in the Editor and debug output.")]
    public string DisplayName;

    // -------------------------------------------------------------------------
    // Location
    // -------------------------------------------------------------------------

    [Tooltip("Location the player must currently be in for this event to be eligible. " +
             "Leave empty to allow the event at any location.")]
    public LocationData RequiredLocation;

    // -------------------------------------------------------------------------
    // Scheduling
    // -------------------------------------------------------------------------

    [Tooltip("Higher priority events are evaluated and triggered before lower ones " +
             "when multiple events are eligible at the same time.")]
    public int Priority;

    [Tooltip("If true, this event can only fire once per save file. " +
             "The event system is responsible for tracking completion.")]
    public bool OneTime;

    [Tooltip("If true, the event system may fire this event automatically when all " +
             "conditions pass, without requiring explicit player action.")]
    public bool AutoTrigger;

    // -------------------------------------------------------------------------
    // Content
    // -------------------------------------------------------------------------

    [Tooltip("Dialogue graph played when this event fires. " +
             "Leave empty for events that trigger effects only (no dialogue).")]
    public DialogueData DialogueGraph;

    // -------------------------------------------------------------------------
    // Conditions
    // -------------------------------------------------------------------------

    [SerializeReference]
    [Tooltip("Conditions that must all pass for this event to be eligible. " +
             "Use the 'Add Condition' button to add new conditions. " +
             "An empty list means the event is always eligible (subject to location and OneTime checks).")]
    public List<EventCondition> Conditions = new();

    // -------------------------------------------------------------------------
    // Consequences
    // -------------------------------------------------------------------------

    [SerializeReference]
    [Tooltip("Side-effects applied by EventManager after this event completes. " +
             "Use the 'Add Consequence' button to add new consequences. " +
             "Consequences are executed in list order. An empty list means no consequences.")]
    public List<EventConsequence> Consequences = new();
}
