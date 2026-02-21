using System;
using UnityEngine;

/// <summary>
/// ScriptableObject documenting a single character stat in the game.
///
/// Responsibilities:
///   - Carry the identity, description, and constraints of one stat
///     (ID, name, purpose, default/min/max values, clamping behaviour).
///   - Generate its own stable ID so stat definitions are never un-identified.
///
/// No runtime logic — this is a reference asset for designers to document
/// and configure stats without touching code.
/// </summary>
[CreateAssetMenu(menuName = "Game/Stat Definition")]
public class StatDefinition : ScriptableObject
{
    // -------------------------------------------------------------------------
    // Identity
    // -------------------------------------------------------------------------

    /// <summary>
    /// Stable, auto-generated GUID that uniquely identifies this stat definition.
    /// Read-only at runtime. Assigned automatically on creation; do not edit by hand.
    /// </summary>
    [SerializeField, HideInInspector]
    private string _id;

    /// <summary>Read-only accessor for the auto-generated ID.</summary>
    public string Id => _id;

    [Tooltip("Human-readable name of this stat (e.g., \"Strength\", \"Magic\", \"Sanity\").")]
    public string DisplayName;

    // -------------------------------------------------------------------------
    // Documentation
    // -------------------------------------------------------------------------

    [Tooltip("Purpose and usage of this stat. What does it affect? How is it used?")]
    [TextArea(3, 6)]
    public string Description;

    // -------------------------------------------------------------------------
    // Value constraints
    // -------------------------------------------------------------------------

    [Tooltip("Default value for this stat when a character is created.")]
    public int DefaultValue = 50;

    [Tooltip("Minimum allowed value for this stat.")]
    public int MinValue = 0;

    [Tooltip("Maximum allowed value for this stat.")]
    public int MaxValue = 100;

    [Tooltip("When true, stat values are automatically clamped to [MinValue, MaxValue] range. " +
             "When false, values can exceed the range (useful for temporary modifiers).")]
    public bool ClampToRange = true;

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
