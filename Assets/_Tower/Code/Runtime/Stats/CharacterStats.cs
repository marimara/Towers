using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// MonoBehaviour that tracks runtime stat values for a character.
///
/// Responsibilities:
///   - Store current stat values in a <see cref="Dictionary{TKey,TValue}"/> keyed by
///     <see cref="StatDefinition"/>.
///   - Provide safe read/write/modify API that respects each stat's Min/Max constraints.
///   - Auto-create entries from <see cref="StatDefinition.DefaultValue"/> on first access.
///   - Handle null inputs gracefully without crashing.
///
/// This component does NOT:
///   - Serialize stat values to save files — that is the save system's responsibility.
///   - Draw UI or respond to input.
///   - Depend on any game-mode-specific logic.
/// </summary>
public class CharacterStats : MonoBehaviour
{
    // -------------------------------------------------------------------------
    // Private state
    // -------------------------------------------------------------------------

    /// <summary>
    /// Current value of each stat. Lazily populated on first access.
    /// Key: StatDefinition asset. Value: current stat value.
    /// </summary>
    private Dictionary<StatDefinition, int> _stats = new();

    // -------------------------------------------------------------------------
    // Public API
    // -------------------------------------------------------------------------

    /// <summary>
    /// Get the current value of a stat.
    /// If the stat has never been accessed, initializes it from the definition's
    /// <see cref="StatDefinition.DefaultValue"/>.
    /// Returns 0 if <paramref name="stat"/> is null.
    /// </summary>
    /// <param name="stat">The stat definition to read. Can be null.</param>
    public int GetStat(StatDefinition stat)
    {
        if (stat == null)
        {
            Debug.LogWarning("[CharacterStats] GetStat called with null stat definition.");
            return 0;
        }

        // Lazy init — first access creates the entry.
        if (!_stats.ContainsKey(stat))
        {
            _stats[stat] = stat.DefaultValue;
#if UNITY_EDITOR
            Debug.Log($"[CharacterStats] '{gameObject.name}' initialized stat '{stat.DisplayName}' = {stat.DefaultValue}");
#endif
        }

        return _stats[stat];
    }

    /// <summary>
    /// Set the current value of a stat to an exact amount.
    /// Respects the stat's Min/Max constraints if <see cref="StatDefinition.ClampToRange"/>
    /// is enabled.
    /// Does nothing if <paramref name="stat"/> is null.
    /// </summary>
    /// <param name="stat">The stat definition to write. Can be null.</param>
    /// <param name="value">The new value to set.</param>
    public void SetStat(StatDefinition stat, int value)
    {
        if (stat == null)
        {
            Debug.LogWarning("[CharacterStats] SetStat called with null stat definition.");
            return;
        }

        int clamped = stat.ClampToRange
            ? Mathf.Clamp(value, stat.MinValue, stat.MaxValue)
            : value;

        int previous = GetStat(stat); // Lazy init if needed.
        _stats[stat] = clamped;

#if UNITY_EDITOR
        if (previous != clamped)
            Debug.Log($"[CharacterStats] '{gameObject.name}' {stat.DisplayName}: {previous} → {clamped}");
        else
            Debug.Log($"[CharacterStats] '{gameObject.name}' {stat.DisplayName} set to {clamped} (unchanged)");
#endif
    }

    /// <summary>
    /// Modify the current value of a stat by a signed delta.
    /// Respects the stat's Min/Max constraints if <see cref="StatDefinition.ClampToRange"/>
    /// is enabled.
    /// Does nothing if <paramref name="stat"/> is null.
    /// </summary>
    /// <param name="stat">The stat definition to modify. Can be null.</param>
    /// <param name="delta">Amount to add (positive) or subtract (negative).</param>
    public void ModifyStat(StatDefinition stat, int delta)
    {
        if (stat == null)
        {
            Debug.LogWarning("[CharacterStats] ModifyStat called with null stat definition.");
            return;
        }

        int current = GetStat(stat); // Lazy init if needed.
        int newValue = current + delta;

        int clamped = stat.ClampToRange
            ? Mathf.Clamp(newValue, stat.MinValue, stat.MaxValue)
            : newValue;

        _stats[stat] = clamped;

#if UNITY_EDITOR
        if (delta != 0)
            Debug.Log($"[CharacterStats] '{gameObject.name}' {stat.DisplayName}: {current} + {delta:+#;-#;0} → {clamped}");
#endif
    }

    /// <summary>
    /// Returns a snapshot of all current stat values.
    /// Pass this to your save system to persist state between sessions.
    /// The returned dictionary is a copy — mutating it does not affect the component's state.
    /// </summary>
    public Dictionary<StatDefinition, int> GetAllStats() => new(_stats);

    /// <summary>
    /// Loads a snapshot of stat values, merging them into the current store.
    /// Each entry in <paramref name="data"/> is written with <see cref="SetStat"/>,
    /// overwriting any conflicting in-memory value (saved state is authoritative).
    /// Null or empty <paramref name="data"/> is accepted silently (no-op).
    /// </summary>
    /// <param name="data">
    /// The stat snapshot produced by a previous <see cref="GetAllStats"/> call.
    /// Null stat definitions are skipped with a warning.
    /// </param>
    public void LoadStats(Dictionary<StatDefinition, int> data)
    {
        if (data == null || data.Count == 0)
        {
#if UNITY_EDITOR
            Debug.Log($"[CharacterStats] '{gameObject.name}' LoadStats called with null or empty data — nothing to merge.");
#endif
            return;
        }

        int merged  = 0;
        int skipped = 0;

        foreach (var entry in data)
        {
            if (entry.Key == null)
            {
                Debug.LogWarning($"[CharacterStats] '{gameObject.name}' LoadStats: skipping entry with null stat definition.");
                skipped++;
                continue;
            }

            SetStat(entry.Key, entry.Value);
            merged++;
        }

#if UNITY_EDITOR
        Debug.Log($"[CharacterStats] '{gameObject.name}' LoadStats complete — merged {merged} stat(s), skipped {skipped}.");
#endif
    }

    /// <summary>
    /// Clear all stat values. Use at character creation time or when loading
    /// a save that will re-populate stats via <see cref="LoadStats"/>.
    /// </summary>
    public void ResetAll()
    {
        int count = _stats.Count;
        _stats.Clear();

#if UNITY_EDITOR
        Debug.Log($"[CharacterStats] '{gameObject.name}' ResetAll — cleared {count} stat(s).");
#endif
    }

    // -------------------------------------------------------------------------
    // Character Initialization
    // -------------------------------------------------------------------------

    /// <summary>
    /// Initialize this character's stats from a VNCharacter asset.
    ///
    /// Behavior:
    ///   - Clears all existing stats
    ///   - For each StartingStatEntry in character.StartingStats, sets that stat to its starting value
    ///   - Any stat NOT listed falls back to StatDefinition.DefaultValue on first access (lazy init)
    ///   - Respects StatDefinition.ClampToRange constraints
    ///   - If a StatDefinition appears multiple times, last value wins (with warning)
    ///
    /// This method preserves the lazy-initialization pattern — stats not explicitly set
    /// will be created from defaults when first accessed.
    ///
    /// Does nothing if <paramref name="character"/> is null.
    /// </summary>
    /// <param name="character">The VNCharacter asset defining this character's starting stats.</param>
    public void InitializeFromCharacter(VNCharacter character)
    {
        if (character == null)
        {
            Debug.LogWarning("[CharacterStats] InitializeFromCharacter called with null VNCharacter.");
            return;
        }

        // Clear any existing stats
        ResetAll();

        // Apply each starting stat entry
        int appliedCount = 0;
        int duplicateCount = 0;
        var seenStats = new HashSet<StatDefinition>();

        if (character.StartingStats != null && character.StartingStats.Count > 0)
        {
            foreach (var entry in character.StartingStats)
            {
                // Skip null stat definitions
                if (entry.Stat == null)
                {
                    Debug.LogWarning($"[CharacterStats] InitializeFromCharacter: skipping null stat definition in '{character.DisplayName}'.");
                    continue;
                }

                // Check for duplicates — last value wins
                if (seenStats.Contains(entry.Stat))
                {
#if UNITY_EDITOR
                    Debug.LogWarning($"[CharacterStats] InitializeFromCharacter: duplicate stat definition '{entry.Stat.DisplayName}' in character '{character.DisplayName}' — last value ({entry.Value}) will be used.");
#endif
                    duplicateCount++;
                }
                else
                {
                    seenStats.Add(entry.Stat);
                }

                // Use SetStat to respect clamping constraints
                SetStat(entry.Stat, entry.Value);
                appliedCount++;
            }
        }

#if UNITY_EDITOR
        string message = $"[CharacterStats] '{gameObject.name}' initialized from character '{character.DisplayName}' — applied {appliedCount} starting stat(s)";
        if (duplicateCount > 0)
        {
            message += $" ({duplicateCount} duplicate(s) handled — last value won)";
        }
        Debug.Log(message + ".");
#endif
    }
}
