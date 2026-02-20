using System;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// ScriptableObject representing a location in the visual novel RPG.
/// Contains all data for a location including NPCs, actions, events, and presentation.
///
/// ID is auto-generated as a GUID the first time the asset is created or validated
/// via OnValidate — no manual entry required or possible from the Inspector.
/// </summary>
[CreateAssetMenu(menuName = "Game/Location Data")]
public class LocationData : ScriptableObject
{
    // -------------------------------------------------------------------------
    // Identity
    // -------------------------------------------------------------------------

    /// <summary>
    /// Stable, auto-generated GUID that uniquely identifies this location.
    /// Read-only at runtime. Assigned automatically; do not set this by hand.
    /// </summary>
    [SerializeField, HideInInspector]
    private string _id;

    /// <summary>Read-only accessor for the auto-generated ID.</summary>
    public string Id => _id;

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

    // -------------------------------------------------------------------------
    // ID generation
    // -------------------------------------------------------------------------

    /// <summary>
    /// Ensures the ID is always set. Called by Unity in the Editor when the
    /// asset is created or any field changes — guarantees no asset ships without
    /// a valid ID without any manual action from the designer.
    /// </summary>
    private void OnValidate()
    {
        EnsureIdAssigned();
    }

    /// <summary>
    /// Assigns a new GUID if the ID is missing. Safe to call multiple times.
    /// </summary>
    private void EnsureIdAssigned()
    {
        if (!string.IsNullOrEmpty(_id))
            return;

        _id = Guid.NewGuid().ToString("N"); // 32 hex chars, no hyphens

#if UNITY_EDITOR
        UnityEditor.EditorUtility.SetDirty(this);
#endif
    }
}
