using System;
using UnityEngine;

/// <summary>
/// ScriptableObject documenting a single boolean flag in the game.
///
/// Responsibilities:
///   - Carry the identity and description of one flag (ID, name, purpose).
///   - Generate its own stable ID so flag definitions are never un-identified.
///
/// No runtime logic — this is a reference asset for designers to document
/// and organise flags without touching code.
/// </summary>
[CreateAssetMenu(menuName = "Game/Flag Definition")]
public class FlagDefinition : ScriptableObject
{
    // -------------------------------------------------------------------------
    // Identity
    // -------------------------------------------------------------------------

    /// <summary>
    /// Stable, auto-generated GUID that uniquely identifies this flag definition.
    /// Read-only at runtime. Assigned automatically on creation; do not edit by hand.
    /// </summary>
    [SerializeField, HideInInspector]
    private string _id;

    /// <summary>Read-only accessor for the auto-generated ID.</summary>
    public string Id => _id;

    [Tooltip("Human-readable name of this flag (e.g., \"Met Elara\", \"Quest Started\").")]
    public string DisplayName;

    // -------------------------------------------------------------------------
    // Documentation
    // -------------------------------------------------------------------------

    [Tooltip("Purpose and usage of this flag. When is it set? What does it control?")]
    [TextArea(3, 6)]
    public string Description;

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
