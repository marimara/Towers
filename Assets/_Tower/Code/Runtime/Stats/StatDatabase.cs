using System.Collections.Generic;
using UnityEngine;

#if UNITY_EDITOR
using UnityEditor;
#endif

/// <summary>
/// ScriptableObject database of all stat definitions in the game.
///
/// Provides efficient O(1) lookup of StatDefinition assets by ID.
/// Automatically validates the database for duplicate IDs and missing definitions.
///
/// Responsibilities:
///   - Maintain a list of all StatDefinition assets used in the game.
///   - Build and cache a lookup dictionary for fast ID-based queries.
///   - Validate database integrity (no null entries, no duplicate IDs).
///   - Invalidate cache when definitions change in the Editor.
///
/// This is a designer-friendly asset — create one instance per game project
/// and assign all stat definitions to it via the Inspector.
/// </summary>
[CreateAssetMenu(menuName = "Game/Stat Database")]
public class StatDatabase : ScriptableObject
{
    // -------------------------------------------------------------------------
    // Inspector fields
    // -------------------------------------------------------------------------

    [Tooltip("All stat definitions used in this game. Assign StatDefinition assets here.")]
    [SerializeField]
    private List<StatDefinition> _allStats = new();

    // -------------------------------------------------------------------------
    // Runtime cache
    // -------------------------------------------------------------------------

    private Dictionary<string, StatDefinition> _statMap;

    // -------------------------------------------------------------------------
    // Public API
    // -------------------------------------------------------------------------

    /// <summary>
    /// Get a stat definition by its auto-generated ID.
    /// Returns null if the ID is not found in the database.
    /// </summary>
    /// <param name="id">The stat's unique ID.</param>
    public StatDefinition GetById(string id)
    {
        if (string.IsNullOrEmpty(id))
            return null;

        BuildLookupIfNeeded();

        if (_statMap == null || !_statMap.ContainsKey(id))
            return null;

        return _statMap[id];
    }

    /// <summary>
    /// Try to get a stat definition by its auto-generated ID.
    /// Returns true and outputs the definition if found; false otherwise.
    /// </summary>
    /// <param name="id">The stat's unique ID.</param>
    /// <param name="stat">Output: the StatDefinition, or null if not found.</param>
    public bool TryGetById(string id, out StatDefinition stat)
    {
        stat = GetById(id);
        return stat != null;
    }

    /// <summary>
    /// Check whether a stat definition is in this database.
    /// </summary>
    public bool Contains(StatDefinition stat)
    {
        if (stat == null)
            return false;

        return GetById(stat.Id) == stat;
    }

    /// <summary>
    /// Get a read-only view of all stat definitions in this database.
    /// </summary>
    public IReadOnlyList<StatDefinition> AllStats => _allStats.AsReadOnly();

    /// <summary>
    /// Invalidate the cached lookup dictionary.
    /// Call this after modifying _allStats in the Editor, or let OnValidate() handle it.
    /// </summary>
    public void InvalidateLookup()
    {
        _statMap = null;
    }

    // -------------------------------------------------------------------------
    // Caching & validation
    // -------------------------------------------------------------------------

    /// <summary>
    /// Build the cached ID → StatDefinition lookup dictionary if it hasn't been built yet.
    /// </summary>
    private void BuildLookupIfNeeded()
    {
        if (_statMap != null)
            return;

        _statMap = new Dictionary<string, StatDefinition>();

        if (_allStats == null || _allStats.Count == 0)
            return;

        foreach (var stat in _allStats)
        {
            if (stat == null)
            {
                Debug.LogWarning("[StatDatabase] Found null entry in _allStats — skipping.");
                continue;
            }

            if (string.IsNullOrEmpty(stat.Id))
            {
                Debug.LogWarning($"[StatDatabase] Stat '{stat.DisplayName}' has no ID — skipping. This should not happen.");
                continue;
            }

            if (_statMap.ContainsKey(stat.Id))
            {
                Debug.LogWarning($"[StatDatabase] Duplicate stat ID '{stat.Id}' detected for '{stat.DisplayName}'. " +
                                 "Only the first occurrence will be used. Ensure all StatDefinition assets have unique IDs.");
                continue;
            }

            _statMap[stat.Id] = stat;
        }
    }

#if UNITY_EDITOR
    /// <summary>
    /// Called by Unity whenever the Inspector changes or the asset is loaded.
    /// Validates the database and invalidates the lookup cache.
    /// </summary>
    private void OnValidate()
    {
        // Invalidate cache so next query rebuilds it with current data
        InvalidateLookup();

        // Validate each entry
        if (_allStats == null || _allStats.Count == 0)
            return;

        HashSet<string> seenIds = new();

        for (int i = 0; i < _allStats.Count; i++)
        {
            var stat = _allStats[i];

            if (stat == null)
            {
                Debug.LogWarning($"[StatDatabase] Null entry at index {i}.", this);
                continue;
            }

            if (string.IsNullOrEmpty(stat.Id))
            {
                Debug.LogWarning($"[StatDatabase] Stat at index {i} ('{stat.DisplayName}') has no ID. This should not happen.", this);
                continue;
            }

            if (seenIds.Contains(stat.Id))
            {
                Debug.LogWarning($"[StatDatabase] Duplicate stat ID '{stat.Id}' detected at index {i} ('{stat.DisplayName}'). " +
                                 "Stat definitions must have unique IDs.", this);
            }

            seenIds.Add(stat.Id);
        }
    }
#endif
}
