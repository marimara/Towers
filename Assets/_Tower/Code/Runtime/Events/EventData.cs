using System;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// ScriptableObject describing a discrete game event: a story beat, encounter,
/// or triggered interaction that the event system can evaluate and fire.
///
/// Responsibilities:
///   - Carry all designer-authored data for one event (identity, dialogue,
///     conditions, and consequences).
///   - Generate its own stable ID so assets never ship un-identified.
///
/// The event system reads this data; no game logic runs inside this class.
/// </summary>
[CreateAssetMenu(menuName = "Game/Event Data")]
public class EventData : ScriptableObject
{
    // -------------------------------------------------------------------------
    // Identity
    // -------------------------------------------------------------------------

    /// <summary>
    /// Stable, auto-generated GUID that uniquely identifies this event.
    /// Read-only at runtime. Assigned automatically on creation; do not edit by hand.
    /// </summary>
    [SerializeField, HideInInspector]
    private string _id;

    /// <summary>Read-only accessor for the auto-generated ID.</summary>
    public string Id => _id;

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

    [Tooltip("All conditions must pass for this event to be eligible. " +
             "An empty list means the event is always eligible (subject to location and OneTime checks).")]
    [SerializeReference]
    public List<EventCondition> Conditions = new();

    // -------------------------------------------------------------------------
    // Consequences
    // -------------------------------------------------------------------------

    [Tooltip("Side-effects applied by EventManager after this event completes. " +
             "Executed in list order. An empty list means no consequences.")]
    [SerializeReference]
    public List<EventConsequence> Consequences = new();

    // -------------------------------------------------------------------------
    // ID generation
    // -------------------------------------------------------------------------

    /// <summary>
    /// Called by Unity in the Editor on asset creation and on every Inspector change.
    /// Guarantees no asset ships without a valid ID — no manual step needed.
    /// </summary>
    private void OnValidate()
    {
        EnsureIdAssigned();
    }

    /// <summary>
    /// Assigns a new GUID to <see cref="_id"/> if it is currently empty.
    /// Safe to call multiple times; will never overwrite an existing ID.
    /// </summary>
    private void EnsureIdAssigned()
    {
        if (!string.IsNullOrEmpty(_id))
            return;

        _id = Guid.NewGuid().ToString("N"); // 32 lowercase hex chars, no hyphens

#if UNITY_EDITOR
        UnityEditor.EditorUtility.SetDirty(this);
#endif
    }
}
