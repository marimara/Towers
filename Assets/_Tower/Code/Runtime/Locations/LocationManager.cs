using System;
using UnityEngine;

/// <summary>
/// Singleton MonoBehaviour managing the current location in the game.
/// Tracks location state and fires events for presentation layer to handle.
/// </summary>
public class LocationManager : MonoBehaviour
{
    // -------------------------------------------------------------------------
    // Singleton
    // -------------------------------------------------------------------------

    public static LocationManager Instance { get; private set; }

    // -------------------------------------------------------------------------
    // Events
    // -------------------------------------------------------------------------

    /// <summary>Fired when the location changes. Presentation layer should handle background/music updates.</summary>
    public event Action<LocationData> OnLocationChanged;

    // -------------------------------------------------------------------------
    // Private state
    // -------------------------------------------------------------------------

    private LocationData _currentLocation;

    // -------------------------------------------------------------------------
    // Unity lifecycle
    // -------------------------------------------------------------------------

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Debug.LogWarning($"[LocationManager] Multiple instances detected. Destroying duplicate on '{name}'.", this);
            Destroy(gameObject);
            return;
        }

        Instance = this;
        DontDestroyOnLoad(gameObject);
    }

    private void OnDestroy()
    {
        if (Instance == this)
            Instance = null;
    }

    // -------------------------------------------------------------------------
    // Public API
    // -------------------------------------------------------------------------

    /// <summary>
    /// Get the current location.
    /// Returns null if no location has been set.
    /// </summary>
    public LocationData GetCurrentLocation()
    {
        return _currentLocation;
    }

    /// <summary>
    /// Change to a new location.
    /// Fires OnLocationChanged event for presentation layer to handle.
    /// </summary>
    public void ChangeLocation(LocationData newLocation)
    {
        if (newLocation == null)
        {
            Debug.LogWarning("[LocationManager] Attempted to change to null location.");
            return;
        }

        var previousLocation = _currentLocation;
        _currentLocation = newLocation;

        Debug.Log($"[LocationManager] Changed location: {previousLocation?.DisplayName ?? "None"} → {newLocation.DisplayName}");

        OnLocationChanged?.Invoke(newLocation);
    }
}
