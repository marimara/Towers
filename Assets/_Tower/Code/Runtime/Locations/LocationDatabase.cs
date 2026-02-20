using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// ScriptableObject database containing all locations in the game.
/// Provides optimized O(1) lookup by location ID.
/// </summary>
[CreateAssetMenu(menuName = "Game/Location Database")]
public class LocationDatabase : ScriptableObject
{
    [Tooltip("All locations in the game.")]
    public List<LocationData> AllLocations = new();

    // -------------------------------------------------------------------------
    // Runtime lookup cache
    // -------------------------------------------------------------------------

    private Dictionary<string, LocationData> _locationMap;

    /// <summary>
    /// Build the ID→location dictionary. Called automatically on first lookup.
    /// </summary>
    private void BuildLookup()
    {
        _locationMap = new Dictionary<string, LocationData>(AllLocations.Count);
        
        foreach (var location in AllLocations)
        {
            if (location == null)
                continue;

            if (string.IsNullOrEmpty(location.Id))
            {
                Debug.LogWarning($"[LocationDatabase] Location '{location.name}' has no ID. Skipping.", this);
                continue;
            }

            if (!_locationMap.TryAdd(location.Id, location))
            {
                Debug.LogWarning($"[LocationDatabase] Duplicate location ID '{location.Id}'. Check database integrity.", this);
            }
        }
    }

    /// <summary>
    /// Get location by ID. Returns null if not found.
    /// Builds lookup cache on first call.
    /// </summary>
    public LocationData GetById(string id)
    {
        if (_locationMap == null)
            BuildLookup();

        if (string.IsNullOrEmpty(id))
            return null;

        _locationMap.TryGetValue(id, out var location);
        return location;
    }

    /// <summary>
    /// Try to get location by ID. Returns true if found.
    /// Builds lookup cache on first call.
    /// </summary>
    public bool TryGetById(string id, out LocationData location)
    {
        if (_locationMap == null)
            BuildLookup();

        if (string.IsNullOrEmpty(id))
        {
            location = null;
            return false;
        }

        return _locationMap.TryGetValue(id, out location);
    }

    /// <summary>
    /// Invalidate the lookup cache. Call after adding/removing locations at runtime.
    /// </summary>
    public void InvalidateLookup()
    {
        _locationMap = null;
    }
}
