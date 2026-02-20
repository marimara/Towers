using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// ScriptableObject database containing all flag definitions in the game.
/// Provides optimized O(1) lookup by flag ID using a lazily-built internal dictionary.
///
/// Warnings logged for:
///   - Null or empty IDs (entries are skipped).
///   - Duplicate IDs (all duplicates are logged; first one is kept).
/// </summary>
[CreateAssetMenu(menuName = "Game/Flag Database")]
public class FlagDatabase : ScriptableObject
{
    [Tooltip("All flag definitions in the game.")]
    public List<FlagDefinition> AllFlags = new();

    // -------------------------------------------------------------------------
    // Runtime lookup cache
    // -------------------------------------------------------------------------

    private Dictionary<string, FlagDefinition> _flagMap;

    /// <summary>
    /// Build the ID→flag dictionary. Called automatically on first lookup.
    /// Logs warnings for null entries, empty IDs, and duplicate IDs.
    /// </summary>
    private void BuildLookup()
    {
        _flagMap = new Dictionary<string, FlagDefinition>(AllFlags.Count);

        foreach (var flag in AllFlags)
        {
            if (flag == null)
                continue;

            if (string.IsNullOrEmpty(flag.Id))
            {
                Debug.LogWarning($"[FlagDatabase] Flag definition '{flag.name}' has no ID. Skipping.", this);
                continue;
            }

            if (!_flagMap.TryAdd(flag.Id, flag))
            {
                Debug.LogWarning($"[FlagDatabase] Duplicate flag ID '{flag.Id}'. Check database integrity.", this);
            }
        }
    }

    // -------------------------------------------------------------------------
    // Public API
    // -------------------------------------------------------------------------

    /// <summary>
    /// Get a flag definition by its ID. Returns null if not found.
    /// Builds lookup cache on first call.
    /// </summary>
    /// <param name="id">The flag ID to search for. Case-sensitive.</param>
    public FlagDefinition GetById(string id)
    {
        if (_flagMap == null)
            BuildLookup();

        if (string.IsNullOrEmpty(id))
            return null;

        _flagMap.TryGetValue(id, out var flag);
        return flag;
    }

    /// <summary>
    /// Try to get a flag definition by its ID. Returns true if found.
    /// Builds lookup cache on first call.
    /// </summary>
    /// <param name="id">The flag ID to search for. Case-sensitive.</param>
    /// <param name="flag">
    /// Set to the <see cref="FlagDefinition"/> if found; null otherwise.
    /// </param>
    public bool TryGetById(string id, out FlagDefinition flag)
    {
        if (_flagMap == null)
            BuildLookup();

        if (string.IsNullOrEmpty(id))
        {
            flag = null;
            return false;
        }

        return _flagMap.TryGetValue(id, out flag);
    }

    /// <summary>
    /// Check if a flag definition exists in the database by ID.
    /// Builds lookup cache on first call.
    /// </summary>
    /// <param name="flag">The flag definition to search for.</param>
    public bool Contains(FlagDefinition flag)
    {
        if (flag == null || string.IsNullOrEmpty(flag.Id))
            return false;

        if (_flagMap == null)
            BuildLookup();

        return _flagMap.ContainsKey(flag.Id);
    }

    /// <summary>
    /// Invalidate the lookup cache. Call after adding/removing flags at runtime.
    /// </summary>
    public void InvalidateLookup()
    {
        _flagMap = null;
    }
}
