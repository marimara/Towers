using UnityEngine;

/// <summary>
/// ScriptableObject representing an action available at a location.
///
/// When executed via Execute(), this action:
///   - Advances in-game time by TimeCost hours
///   - Triggers LinkedEvent if TriggersEvent is enabled
///
/// Time advancement is optional — set TimeCost to 0 for free actions.
/// </summary>
[CreateAssetMenu(menuName = "Game/Location Action")]
public class LocationAction : ScriptableObject
{
    // -------------------------------------------------------------------------
    // Data
    // -------------------------------------------------------------------------

    [Tooltip("Unique identifier for this action.")]
    public string Id;

    [Tooltip("Display name shown to the player.")]
    public string DisplayName;

    [Tooltip("Icon sprite for this action.")]
    public Sprite Icon;

    [Tooltip("Number of in-game hours this action costs. Set to 0 for free actions.")]
    [Min(0)]
    public int TimeCost = 1;

    [Tooltip("If true, this action triggers an event.")]
    public bool TriggersEvent;

    [Tooltip("Event triggered when this action is performed.")]
    public EventData LinkedEvent;

    // -------------------------------------------------------------------------
    // Public API - Execution
    // -------------------------------------------------------------------------

    /// <summary>
    /// Execute this action, advancing in-game time and optionally triggering events.
    /// Call this from LocationManager or ActionExecutor when the player chooses this action.
    /// </summary>
    public void Execute()
    {
        // Advance time if GameClock is available
        if (GameClock.Instance == null)
        {
            Debug.LogWarning($"[LocationAction '{Id}'] GameClock.Instance is null. Time advancement skipped.");
        }
        else
        {
            GameClock.Instance.AdvanceByAction(TimeCost);
        }

        // Trigger linked event if enabled
        if (TriggersEvent && LinkedEvent != null)
        {
            // Use EventManager to trigger the event
            if (EventManager.Instance == null)
            {
                Debug.LogWarning($"[LocationAction '{Id}'] EventManager.Instance is null. Event trigger skipped.");
            }
            else
            {
                EventManager.Instance.TriggerEvent(LinkedEvent);
            }
        }
    }
}
