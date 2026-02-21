using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// ScriptableObject representing a location in the visual novel RPG.
/// Contains all data for a location including NPCs, actions, events, and presentation.
///
/// ID is auto-generated and managed by the base UniqueIdScriptableObject class.
/// </summary>
[CreateAssetMenu(menuName = "Game/Location Data")]
public class LocationData : UniqueIdScriptableObject
{
    // -------------------------------------------------------------------------
    // Display
    // -------------------------------------------------------------------------

    [Tooltip("Display name shown to the player.")]
    public string DisplayName;

    [Tooltip("Background sprite for this location.")]
    public Sprite Background;

    [Tooltip("Background music for this location.")]
    public AudioClip Music;

    // -------------------------------------------------------------------------
    // Content
    // -------------------------------------------------------------------------

    [Tooltip("NPCs present at this location.")]
    public List<VNCharacter> NPCsPresent = new();

    [Tooltip("Actions available at this location.")]
    public List<LocationAction> AvailableActions = new();

    [Tooltip("Events that can occur at this location.")]
    public List<EventData> PossibleEvents = new();

    // -------------------------------------------------------------------------
    // Tags
    // -------------------------------------------------------------------------

    [Tooltip("Tags for categorising or filtering this location.")]
    public List<LocationTag> Tags = new();
}
