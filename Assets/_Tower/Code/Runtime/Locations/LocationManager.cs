using System;
using UnityEngine;

/// <summary>
/// Singleton MonoBehaviour managing the current location in the game.
/// Tracks location state and fires events for presentation layer to handle.
/// </summary>
public class LocationManager : MonoBehaviour
{
    public static LocationManager Instance { get; private set; }

    public event Action<LocationData> OnLocationChanged;

    [SerializeField]
    private LocationData _startingLocation;

    private LocationData _currentLocation;

    private void Awake()
    {
        Instance = this;
        DontDestroyOnLoad(gameObject);
    }

    private void Start()
    {
        if (_startingLocation != null)
        {
            ChangeLocation(_startingLocation);
        }
    }

    public LocationData GetCurrentLocation()
    {
        return _currentLocation;
    }

    public void ChangeLocation(LocationData newLocation)
    {
        if (newLocation == null)
        {
            Debug.Log("[LocationManager] Attempted to change to null location.");
            return;
        }

        var previousLocation = _currentLocation;
        _currentLocation = newLocation;

        Debug.Log($"Location: {previousLocation?.DisplayName} → {newLocation.DisplayName}");

        OnLocationChanged?.Invoke(newLocation);
    }
}
