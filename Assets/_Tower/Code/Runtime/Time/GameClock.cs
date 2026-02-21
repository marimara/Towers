using System;
using UnityEngine;

/// <summary>
/// Singleton MonoBehaviour that manages in-game time for a narrative RPG.
///
/// Responsibilities:
///   - Track current in-game day and hour (0-23)
///   - Provide read-only access to CurrentDay and CurrentHour
///   - Advance time via AdvanceHours() with automatic day overflow
///   - Advance time via AdvanceByAction() with configurable default cost
///   - Fire OnTimeChanged event whenever time changes
///   - Persist across scene loads via DontDestroyOnLoad
///
/// What this class does NOT do:
///   - No UI rendering
///   - No save/load (save system calls Getters to snapshot state)
///   - No gameplay logic (other systems subscribe to events)
///   - No time simulation (only advances on explicit calls)
/// </summary>
public class GameClock : MonoBehaviour
{
    // -------------------------------------------------------------------------
    // Singleton
    // -------------------------------------------------------------------------

    public static GameClock Instance { get; private set; }

    // -------------------------------------------------------------------------
    // State
    // -------------------------------------------------------------------------

    private int _currentDay = 1;
    private int _currentHour = 0;

    // -------------------------------------------------------------------------
    // Events
    // -------------------------------------------------------------------------

    /// <summary>
    /// Fired whenever the in-game time changes (day or hour).
    /// Subscribers can use this to update UI, trigger time-based events, etc.
    /// </summary>
    public event Action<int, int> OnTimeChanged;

    // -------------------------------------------------------------------------
    // Properties - Read-only access to time
    // -------------------------------------------------------------------------

    /// <summary>
    /// The current in-game day (starting at 1).
    /// Use this for day-based conditions and time tracking.
    /// </summary>
    public int CurrentDay => _currentDay;

    /// <summary>
    /// The current in-game hour (0-23).
    /// 0 = midnight, 12 = noon, 23 = late night.
    /// Use this for time-of-day conditions and scheduling.
    /// </summary>
    public int CurrentHour => _currentHour;

    /// <summary>
    /// The current in-game period (morning, afternoon, evening, night, late night).
    /// Derived from CurrentHour; cached for efficiency.
    /// Use this for period-based conditions and ambiance changes.
    /// </summary>
    public TimePeriod CurrentPeriod => GetPeriodForHour(_currentHour);

    // -------------------------------------------------------------------------
    // Unity Lifecycle
    // -------------------------------------------------------------------------

    private void Awake()
    {
        // Singleton instantiation with duplicate detection
        if (Instance != null && Instance != this)
        {
            Debug.LogWarning($"[GameClock] Multiple instances detected. Destroying duplicate on '{name}'.", this);
            Destroy(gameObject);
            return;
        }

        Instance = this;
        DontDestroyOnLoad(gameObject);

#if UNITY_EDITOR
        Debug.Log($"[GameClock] Initialized. Starting time: Day {_currentDay}, Hour {_currentHour}");
#endif
    }

    private void OnDestroy()
    {
        if (Instance == this)
            Instance = null;
    }

    // -------------------------------------------------------------------------
    // Public API - Time Advancement
    // -------------------------------------------------------------------------

    /// <summary>
    /// Advance in-game time by the specified number of hours.
    /// Automatically handles day overflow (e.g., hour 25 → day 2, hour 1).
    /// Fires OnTimeChanged event if time actually changed.
    /// </summary>
    /// <param name="amount">Number of hours to advance (can be negative).</param>
    public void AdvanceHours(int amount)
    {
        if (amount == 0)
            return;  // No-op for zero advance

        // Store old time for comparison
        int previousDay = _currentDay;
        int previousHour = _currentHour;

        // Add hours to current hour
        _currentHour += amount;

        // Handle day overflow
        // Positive overflow: hour >= 24
        while (_currentHour >= 24)
        {
            _currentHour -= 24;
            _currentDay++;
        }

        // Negative overflow: hour < 0
        while (_currentHour < 0)
        {
            _currentHour += 24;
            _currentDay--;
        }

        // Fire event if time changed
        bool dayChanged = _currentDay != previousDay;
        bool hourChanged = _currentHour != previousHour;

        if (dayChanged || hourChanged)
        {
#if UNITY_EDITOR
            Debug.Log($"[GameClock] Time advanced: Day {previousDay}, Hour {previousHour} → Day {_currentDay}, Hour {_currentHour}");
#endif
            OnTimeChanged?.Invoke(_currentDay, _currentHour);
        }
    }

    /// <summary>
    /// Advance in-game time by the cost of an action.
    /// Use this when game actions consume time (e.g., talking to NPC takes 1 hour).
    /// Fires OnTimeChanged event if time actually changed.
    /// </summary>
    /// <param name="actionCost">Number of hours the action costs (default: 1). Can be 0 for free actions.</param>
    public void AdvanceByAction(int actionCost = 1)
    {
        AdvanceHours(actionCost);
    }

    // -------------------------------------------------------------------------
    // Public API - Quick Snapshot/Restore (tuple-based)
    // -------------------------------------------------------------------------

    /// <summary>
    /// Get a snapshot of the current time state as a tuple (day, hour).
    /// No serialization — just returns the raw values.
    /// Use this for quick state capture without struct overhead.
    /// </summary>
    /// <returns>Tuple of (day, hour)</returns>
    public (int day, int hour) GetTimeSnapshot()
    {
        return (_currentDay, _currentHour);
    }

    /// <summary>
    /// Restore time state from day and hour values.
    /// No deserialization — just raw state restore.
    /// Fires OnTimeChanged event if time was different.
    /// </summary>
    /// <param name="day">Day to restore (must be >= 1)</param>
    /// <param name="hour">Hour to restore (0-23, will be clamped)</param>
    public void LoadTimeSnapshot(int day, int hour)
    {
        SetTime(day, hour);
    }

    // -------------------------------------------------------------------------
    // Public API - Time Access (for save system)
    // -------------------------------------------------------------------------

    /// <summary>
    /// Get the current time as a tuple.
    /// Use this in save systems to snapshot state.
    /// </summary>
    /// <returns>Tuple of (day, hour)</returns>
    public (int day, int hour) GetCurrentTime()
    {
        return (_currentDay, _currentHour);
    }

    /// <summary>
    /// Get the current time as a formatted string.
    /// Useful for debugging and UI that will be implemented elsewhere.
    /// </summary>
    /// <returns>String like "Day 5, Hour 14"</returns>
    public string GetTimeAsString()
    {
        return $"Day {_currentDay}, Hour {_currentHour:D2}";
    }

    // -------------------------------------------------------------------------
    // Save System Integration
    // -------------------------------------------------------------------------

    /// <summary>
    /// Snapshot the current time state for saving.
    /// Call this when preparing to save the game.
    /// </summary>
    /// <returns>Snapshot object with current day and hour</returns>
    public TimeSnapshot GetSnapshot()
    {
        return new TimeSnapshot { day = _currentDay, hour = _currentHour };
    }

    /// <summary>
    /// Restore time state from a snapshot.
    /// Call this when loading a saved game.
    /// Fires OnTimeChanged event if time was different.
    /// </summary>
    /// <param name="snapshot">Snapshot to restore from (nullable)</param>
    public void LoadSnapshot(TimeSnapshot? snapshot)
    {
        if (!snapshot.HasValue)
        {
            Debug.LogWarning("[GameClock] Cannot load null snapshot. Time unchanged.");
            return;
        }

        var snap = snapshot.Value;

        int previousDay = _currentDay;
        int previousHour = _currentHour;

        _currentDay = snap.day;
        _currentHour = snap.hour;

        // Validate loaded time
        if (_currentHour < 0 || _currentHour > 23)
        {
            Debug.LogWarning($"[GameClock] Loaded invalid hour {_currentHour}. Clamping to 0-23.");
            _currentHour = Mathf.Clamp(_currentHour, 0, 23);
        }

        if (_currentDay < 1)
        {
            Debug.LogWarning($"[GameClock] Loaded invalid day {_currentDay}. Resetting to 1.");
            _currentDay = 1;
        }

        // Fire event if time changed
        if (_currentDay != previousDay || _currentHour != previousHour)
        {
#if UNITY_EDITOR
            Debug.Log($"[GameClock] Time loaded: Day {_currentDay}, Hour {_currentHour}");
#endif
            OnTimeChanged?.Invoke(_currentDay, _currentHour);
        }
    }

    // -------------------------------------------------------------------------
    // Utility
    // -------------------------------------------------------------------------

    /// <summary>
    /// Set the time directly (for testing or dev commands).
    /// Fires OnTimeChanged event if time changed.
    /// </summary>
    /// <param name="day">New day (must be >= 1)</param>
    /// <param name="hour">New hour (0-23, will be clamped)</param>
    public void SetTime(int day, int hour)
    {
        int previousDay = _currentDay;
        int previousHour = _currentHour;

        _currentDay = Mathf.Max(1, day);
        _currentHour = Mathf.Clamp(hour, 0, 23);

        if (_currentDay != previousDay || _currentHour != previousHour)
        {
#if UNITY_EDITOR
            Debug.Log($"[GameClock] Time set: Day {_currentDay}, Hour {_currentHour}");
#endif
            OnTimeChanged?.Invoke(_currentDay, _currentHour);
        }
    }

    /// <summary>
    /// <summary>
    /// Reset time to initial state (Day 1, Hour 0).
    /// Useful for new game start or testing.
    /// Fires OnTimeChanged event.
    /// </summary>
    public void Reset()
    {
        SetTime(1, 0);
    }

    // -------------------------------------------------------------------------
    // Time Period Helper
    // -------------------------------------------------------------------------

    /// <summary>
    /// Resolve a given hour to its corresponding time period.
    /// Pure logic with no side effects.
    /// </summary>
    /// <param name="hour">Hour to resolve (0-23)</param>
    /// <returns>TimePeriod corresponding to the hour</returns>
    private static TimePeriod GetPeriodForHour(int hour)
    {
        // Hour ranges for each period:
        // LateNight = 3–5   (3 AM to 5:59 AM)
        // Morning   = 6–11  (6 AM to 11:59 AM)
        // Afternoon = 12–17 (12 PM to 5:59 PM)
        // Evening   = 18–21 (6 PM to 9:59 PM)
        // Night     = 22–2  (10 PM to 2:59 AM)

        return hour switch
        {
            >= 3 and <= 5 => TimePeriod.LateNight,
            >= 6 and <= 11 => TimePeriod.Morning,
            >= 12 and <= 17 => TimePeriod.Afternoon,
            >= 18 and <= 21 => TimePeriod.Evening,
            >= 22 or (>= 0 and <= 2) => TimePeriod.Night,
            _ => TimePeriod.Night,  // Fallback (shouldn't reach with valid 0-23 hours)
        };
    }
}

/// <summary>
/// Serializable snapshot of in-game time.
/// Use this for save/load operations.
/// </summary>
[System.Serializable]
public struct TimeSnapshot
{
    public int day;
    public int hour;
}
