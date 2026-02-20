using UnityEngine;

/// <summary>
/// ScriptableObject representing an action available at a location.
/// </summary>
[CreateAssetMenu(menuName = "Game/Location Action")]
public class LocationAction : ScriptableObject
{
    [Tooltip("Unique identifier for this action.")]
    public string Id;

    [Tooltip("Display name shown to the player.")]
    public string DisplayName;

    [Tooltip("Icon sprite for this action.")]
    public Sprite Icon;

    [Tooltip("If true, this action triggers an event.")]
    public bool TriggersEvent;

    [Tooltip("Event triggered when this action is performed.")]
    public EventData LinkedEvent;
}
