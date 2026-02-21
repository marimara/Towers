using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Runtime representation of a character instance.
///
/// Bridges the gap between:
/// - VNCharacter (ScriptableObject definition asset — reusable, shared across all instances)
/// - CharacterStats (MonoBehaviour on this GameObject — instance-specific stat values)
///
/// Responsibilities:
///   - Hold a reference to the VNCharacter definition asset.
///   - Provide a unified interface to access character identity and stats.
///   - Ensure CharacterStats component exists on this GameObject for runtime stat tracking.
///
/// This class does NOT:
///   - Manage character lifecycle (creation/destruction) — that's the responsibility of the scene/game manager.
///   - Handle saving/loading — the save system owns serialization.
///   - Provide game logic beyond stat access.
/// </summary>
public class CharacterInstance : MonoBehaviour
{
    // -------------------------------------------------------------------------
    // Inspector fields
    // -------------------------------------------------------------------------

    [Tooltip("The character definition asset for this instance. Defines display name, portrait, race, etc.")]
    [SerializeField]
    private VNCharacter _definition;

    // -------------------------------------------------------------------------
    // Cached components
    // -------------------------------------------------------------------------

    private CharacterStats _stats;

    // -------------------------------------------------------------------------
    // Lifecycle
    // -------------------------------------------------------------------------

    private void OnEnable()
    {
        // Ensure CharacterStats component exists
        _stats = GetComponent<CharacterStats>();
        if (_stats == null)
        {
            _stats = gameObject.AddComponent<CharacterStats>();

#if UNITY_EDITOR
            Debug.Log($"[CharacterInstance] Added CharacterStats component to '{gameObject.name}'.");
#endif
        }
    }

    // -------------------------------------------------------------------------
    // Public API — Identity
    // -------------------------------------------------------------------------

    /// <summary>
    /// Get the character definition asset (VNCharacter ScriptableObject).
    /// Returns null if not assigned.
    /// </summary>
    public VNCharacter Definition => _definition;

    /// <summary>
    /// Get the character's display name from the definition asset.
    /// Returns an empty string if no definition is assigned.
    /// </summary>
    public string DisplayName => _definition != null ? _definition.DisplayName : string.Empty;

    /// <summary>
    /// Get the character's portrait sprite from the definition asset.
    /// Returns null if no definition is assigned.
    /// </summary>
    public Sprite Portrait => _definition != null ? _definition.Portrait : null;

    /// <summary>
    /// Get the character's name text color from the definition asset.
    /// Returns white if no definition is assigned.
    /// </summary>
    public Color NameColor => _definition != null ? _definition.NameColor : Color.white;

    /// <summary>
    /// Get the character's race from the definition asset.
    /// Returns null if no definition is assigned.
    /// </summary>
    public Race Race => _definition != null ? _definition.Race : default;

    // -------------------------------------------------------------------------
    // Public API — Stats (delegation)
    // -------------------------------------------------------------------------

    /// <summary>
    /// Get the current value of a stat for this character.
    /// Automatically initialises the stat from its definition if never accessed.
    /// Returns 0 if CharacterStats is null or the stat is null.
    /// </summary>
    public int GetStat(StatDefinition stat)
    {
        if (_stats == null)
        {
            Debug.LogWarning($"[CharacterInstance] CharacterStats not found on '{gameObject.name}'.");
            return 0;
        }

        return _stats.GetStat(stat);
    }

    /// <summary>
    /// Set the current value of a stat for this character to an exact amount.
    /// Respects the stat's Min/Max constraints if StatDefinition.ClampToRange is enabled.
    /// Does nothing if CharacterStats is null or the stat is null.
    /// </summary>
    public void SetStat(StatDefinition stat, int value)
    {
        if (_stats == null)
        {
            Debug.LogWarning($"[CharacterInstance] CharacterStats not found on '{gameObject.name}'.");
            return;
        }

        _stats.SetStat(stat, value);
    }

    /// <summary>
    /// Modify the current value of a stat for this character by a signed delta.
    /// Respects the stat's Min/Max constraints if StatDefinition.ClampToRange is enabled.
    /// Does nothing if CharacterStats is null or the stat is null.
    /// </summary>
    public void ModifyStat(StatDefinition stat, int delta)
    {
        if (_stats == null)
        {
            Debug.LogWarning($"[CharacterInstance] CharacterStats not found on '{gameObject.name}'.");
            return;
        }

        _stats.ModifyStat(stat, delta);
    }

    /// <summary>
    /// Get a snapshot of all current stat values for this character.
    /// Pass this to your save system to persist state between sessions.
    /// The returned dictionary is a copy — mutating it does not affect this instance's state.
    /// Returns null if CharacterStats is null.
    /// </summary>
    public Dictionary<StatDefinition, int> GetAllStats()
    {
        if (_stats == null)
        {
            Debug.LogWarning($"[CharacterInstance] CharacterStats not found on '{gameObject.name}'.");
            return null;
        }

        return _stats.GetAllStats();
    }

    /// <summary>
    /// Load a snapshot of stat values, merging them into this character's stat store.
    /// Each entry is written with SetStat, overwriting any conflicting in-memory value.
    /// Null or empty data is accepted silently (no-op).
    /// Does nothing if CharacterStats is null.
    /// </summary>
    public void LoadStats(Dictionary<StatDefinition, int> data)
    {
        if (_stats == null)
        {
            Debug.LogWarning($"[CharacterInstance] CharacterStats not found on '{gameObject.name}'.");
            return;
        }

        _stats.LoadStats(data);
    }

    /// <summary>
    /// Clear all stat values for this character.
    /// Use at character creation time or when loading a save that will re-populate stats via LoadStats.
    /// Does nothing if CharacterStats is null.
    /// </summary>
    public void ResetAllStats()
    {
        if (_stats == null)
        {
            Debug.LogWarning($"[CharacterInstance] CharacterStats not found on '{gameObject.name}'.");
            return;
        }

        _stats.ResetAll();
    }

    // -------------------------------------------------------------------------
    // Editor validation
    // -------------------------------------------------------------------------

#if UNITY_EDITOR
    private void OnValidate()
    {
        // Warn if definition is not assigned
        if (_definition == null)
        {
            Debug.LogWarning($"[CharacterInstance] '{gameObject.name}' has no VNCharacter definition assigned.", this);
        }
    }
#endif
}
