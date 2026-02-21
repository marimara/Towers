using System;
using UnityEngine;

/// <summary>
/// Base class for ScriptableObjects that require globally unique identifiers.
///
/// Responsibilities:
///   - Auto-generate a stable GUID on creation
///   - Detect and regenerate if a duplicate ID is found
///   - Ensure no two assets in the project share the same ID
///   - Provide read-only public access to the ID
///
/// Subclasses inherit this automatically; no additional code needed.
/// ID regeneration happens transparently in the Editor on asset save.
/// </summary>
public abstract class UniqueIdScriptableObject : ScriptableObject
{
    // -------------------------------------------------------------------------
    // Private state
    // -------------------------------------------------------------------------

    /// <summary>
    /// Stable, auto-generated GUID that uniquely identifies this asset.
    /// Do not edit manually. Regenerated automatically if empty or duplicate.
    /// </summary>
    [SerializeField, HideInInspector]
    private string _id;

    // -------------------------------------------------------------------------
    // Public API
    // -------------------------------------------------------------------------

    /// <summary>
    /// The unique identifier for this asset (read-only).
    /// </summary>
    public string Id => _id;

    // -------------------------------------------------------------------------
    // ID Management
    // -------------------------------------------------------------------------

    /// <summary>
    /// Called by Unity in the Editor on asset creation and on every Inspector change.
    /// Ensures this asset has a valid, unique ID.
    /// </summary>
    protected virtual void OnValidate()
    {
        EnsureUniqueId();
    }

    /// <summary>
    /// Assigns a new GUID to this asset if empty.
    /// Regenerates if a duplicate ID is detected in the project.
    /// Safe to call multiple times.
    /// </summary>
    private void EnsureUniqueId()
    {
        // If ID is missing, generate one
        if (string.IsNullOrEmpty(_id))
        {
            _id = GenerateNewId();
            MarkDirty();

#if UNITY_EDITOR
            Debug.Log($"[UniqueIdScriptableObject] '{name}' assigned new ID: {_id}");
#endif
            return;
        }

        // Check for duplicates using UniqueIdUtility
        var duplicates = UniqueIdUtility.FindAllWithId(_id);
        if (duplicates.Count > 1)
        {
            string oldId = _id;
            _id = GenerateNewId();
            MarkDirty();

#if UNITY_EDITOR
            Debug.LogWarning($"[UniqueIdScriptableObject] Duplicate ID detected for '{name}'! " +
                           $"Asset was likely duplicated. Regenerated new ID.\n" +
                           $"Old ID: {oldId}\n" +
                           $"New ID: {_id}\n" +
                           $"Conflicted with {duplicates.Count - 1} other asset(s).",
                           this);
#endif
        }
    }

    /// <summary>
    /// Generates a new globally unique ID (32-char lowercase hex GUID).
    /// </summary>
    private static string GenerateNewId()
    {
        return Guid.NewGuid().ToString("N");
    }

    /// <summary>
    /// Marks this asset as dirty (Editor-only operation).
    /// </summary>
    private void MarkDirty()
    {
#if UNITY_EDITOR
        UnityEditor.EditorUtility.SetDirty(this);
#endif
    }
}
