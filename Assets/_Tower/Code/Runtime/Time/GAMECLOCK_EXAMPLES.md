# GameClock — Complete Examples

## Example 1: Basic Time Advancement

```csharp
using UnityEngine;

public class BasicTimeTest : MonoBehaviour
{
    void Start()
    {
        // Check initial time
        Debug.Log($"Starting time: {GameClock.Instance.GetTimeAsString()}");
        // Output: "Starting time: Day 1, Hour 00"

        // Advance 5 hours
        GameClock.Instance.AdvanceHours(5);
        Debug.Log($"After 5 hours: {GameClock.Instance.GetTimeAsString()}");
        // Output: "After 5 hours: Day 1, Hour 05"

        // Advance to next day
        GameClock.Instance.AdvanceHours(20);
        Debug.Log($"After 20 more hours: {GameClock.Instance.GetTimeAsString()}");
        // Output: "After 20 more hours: Day 2, Hour 01"

        // Rewind
        GameClock.Instance.AdvanceHours(-3);
        Debug.Log($"After -3 hours: {GameClock.Instance.GetTimeAsString()}");
        // Output: "After -3 hours: Day 1, Hour 22"
    }
}
```

---

## Example 2: Subscribing to Time Changes

```csharp
using UnityEngine;

public class TimeChangeListener : MonoBehaviour
{
    void Start()
    {
        GameClock.Instance.OnTimeChanged += HandleTimeChanged;
    }

    void HandleTimeChanged(int day, int hour)
    {
        Debug.Log($"[Event] Time changed: Day {day}, Hour {hour:D2}");

        // Trigger different events based on time
        if (hour == 6)
        {
            Debug.Log("Morning bells ring!");
            EventManager.Instance.TriggerEvent("DayStart");
        }

        if (hour == 12)
        {
            Debug.Log("The sun is at its peak!");
            EventManager.Instance.TriggerEvent("Noon");
        }

        if (hour == 20)
        {
            Debug.Log("Evening falls...");
            EventManager.Instance.TriggerEvent("DayEnd");
        }

        if (hour == 0)
        {
            Debug.Log("The day has ended. A new one begins.");
            EventManager.Instance.TriggerEvent("Midnight");
        }
    }

    void OnDestroy()
    {
        GameClock.Instance.OnTimeChanged -= HandleTimeChanged;
    }
}
```

---

## Example 3: Action-Based Time Costs

```csharp
using UnityEngine;

public class ActionSystem : MonoBehaviour
{
    // Define action costs
    private const int QUICK_ACTION = 0;      // Talk briefly
    private const int NORMAL_ACTION = 1;     // Standard conversation
    private const int LONG_ACTION = 3;       // Long event
    private const int SLEEP_ACTION = 8;      // Rest at inn

    public void QuickAction()
    {
        Debug.Log("Performing quick action (no time cost)");
        GameClock.Instance.AdvanceByAction(QUICK_ACTION);
    }

    public void TalkToNPC()
    {
        Debug.Log("Talking to NPC");
        // ... dialogue code ...
        GameClock.Instance.AdvanceByAction(NORMAL_ACTION);
        Debug.Log($"1 hour passed. Now: {GameClock.Instance.GetTimeAsString()}");
    }

    public void LongQuestStart()
    {
        Debug.Log("Starting long quest");
        // ... quest code ...
        GameClock.Instance.AdvanceByAction(LONG_ACTION);
        Debug.Log($"3 hours passed. Now: {GameClock.Instance.GetTimeAsString()}");
    }

    public void SleepAtInn()
    {
        Debug.Log("Resting at inn");
        GameClock.Instance.AdvanceByAction(SLEEP_ACTION);
        Debug.Log($"Slept 8 hours. Now: {GameClock.Instance.GetTimeAsString()}");
    }
}
```

---

## Example 4: Time-Based Conditions

```csharp
using UnityEngine;

public class TimeBasedEvents : MonoBehaviour
{
    void Start()
    {
        GameClock.Instance.OnTimeChanged += CheckEventConditions;
    }

    void CheckEventConditions(int day, int hour)
    {
        // Morning event (6 AM - noon)
        if (IsTimeInRange(hour, 6, 12))
        {
            Debug.Log("Morning event available!");
        }

        // Night event (8 PM - 6 AM)
        if (IsNight(hour))
        {
            Debug.Log("Spooky night event available!");
        }

        // Specific time events
        if (hour == 12)
        {
            Debug.Log("Lunch time!");
        }

        if (hour == 18)
        {
            Debug.Log("Dinner time!");
        }

        // Day of week events
        int dayOfWeek = day % 7;
        if (dayOfWeek == 0)
        {
            Debug.Log("Sunday - Market day!");
        }

        // Specific date events
        if (day == 7)
        {
            Debug.Log("Day 7 special event!");
        }
    }

    bool IsTimeInRange(int hour, int startHour, int endHour)
    {
        return hour >= startHour && hour < endHour;
    }

    bool IsNight(int hour)
    {
        return hour >= 20 || hour < 6;
    }

    void OnDestroy()
    {
        GameClock.Instance.OnTimeChanged -= CheckEventConditions;
    }
}
```

---

## Example 5: NPC Daily Routines

```csharp
using UnityEngine;

public class NPCDailyRoutine : MonoBehaviour
{
    private string _npcName = "Alice";
    private int _lastSeenDay = -1;
    private int _currentMood = 5;  // 1-10 scale
    private string _currentLocation = "Home";

    void Start()
    {
        GameClock.Instance.OnTimeChanged += OnTimeChanged;
        // Initialize to current day to prevent false reset
        _lastSeenDay = GameClock.Instance.CurrentDay;
    }

    void OnTimeChanged(int day, int hour)
    {
        // Reset daily state at start of new day
        if (day != _lastSeenDay)
        {
            _lastSeenDay = day;
            ResetDailyState();
        }

        // Update NPC behavior based on time
        UpdateRoutine(hour);
    }

    void ResetDailyState()
    {
        Debug.Log($"[{_npcName}] New day started! Resetting routine.");
        _currentMood = 5;  // Neutral mood
        _currentLocation = "Home";
        // Reset daily actions, dialogue options, etc.
    }

    void UpdateRoutine(int hour)
    {
        // 6-9 AM: Morning routine
        if (hour >= 6 && hour < 9)
        {
            _currentLocation = "Kitchen";
            _currentMood = 6;
        }
        // 9-12 PM: Work
        else if (hour >= 9 && hour < 12)
        {
            _currentLocation = "Marketplace";
            _currentMood = 7;
        }
        // 12-2 PM: Lunch
        else if (hour >= 12 && hour < 14)
        {
            _currentLocation = "Restaurant";
            _currentMood = 8;
        }
        // 2-6 PM: More work
        else if (hour >= 14 && hour < 18)
        {
            _currentLocation = "Marketplace";
            _currentMood = 6;
        }
        // 6-9 PM: Dinner
        else if (hour >= 18 && hour < 21)
        {
            _currentLocation = "Home";
            _currentMood = 7;
        }
        // 9 PM-6 AM: Sleep
        else
        {
            _currentLocation = "Home (Sleeping)";
            _currentMood = 0;  // Not interactible
        }

        Debug.Log($"[{_npcName}] Hour {hour}: At {_currentLocation} (Mood: {_currentMood}/10)");
    }

    public string GetLocation() => _currentLocation;
    public int GetMood() => _currentMood;

    void OnDestroy()
    {
        GameClock.Instance.OnTimeChanged -= OnTimeChanged;
    }
}
```

---

## Example 6: Save and Load

```csharp
using UnityEngine;
using System.IO;

[System.Serializable]
public class GameSaveData
{
    public TimeSnapshot gameTime;
    public int playerLevel;
    public string playerName;
    // ... other game data ...
}

public class SaveSystem : MonoBehaviour
{
    private const string SAVE_PATH = "Assets/Saves/savefile.json";

    public void SaveGame(string playerName, int playerLevel)
    {
        var save = new GameSaveData
        {
            gameTime = GameClock.Instance.GetSnapshot(),
            playerLevel = playerLevel,
            playerName = playerName
        };

        string json = JsonUtility.ToJson(save, prettyPrint: true);
        File.WriteAllText(SAVE_PATH, json);

        Debug.Log($"Game saved at {GameClock.Instance.GetTimeAsString()}");
    }

    public void LoadGame()
    {
        if (!File.Exists(SAVE_PATH))
        {
            Debug.LogWarning("Save file not found!");
            return;
        }

        string json = File.ReadAllText(SAVE_PATH);
        var save = JsonUtility.FromJson<GameSaveData>(json);

        // Restore time
        GameClock.Instance.LoadSnapshot(save.gameTime);
        Debug.Log($"Game loaded. Time: {GameClock.Instance.GetTimeAsString()}");

        // Restore player data
        Player.Instance.SetLevel(save.playerLevel);
        Player.Instance.SetName(save.playerName);
    }

    public void NewGame()
    {
        GameClock.Instance.Reset();  // Day 1, Hour 0
        Debug.Log("New game started");
    }
}
```

---

## Example 7: UI Display (For Designers)

```csharp
using UnityEngine;
using TMPro;

public class TimeDisplayUI : MonoBehaviour
{
    [SerializeField] private TextMeshProUGUI timeText;
    [SerializeField] private TextMeshProUGUI dayOfWeekText;
    [SerializeField] private TextMeshProUGUI timeOfDayText;

    void Start()
    {
        GameClock.Instance.OnTimeChanged += UpdateDisplay;
        UpdateDisplay(GameClock.Instance.CurrentDay, GameClock.Instance.CurrentHour);
    }

    void UpdateDisplay(int day, int hour)
    {
        // Display time
        timeText.text = $"Day {day} · {hour:D2}:00";

        // Display day of week
        int dayOfWeek = day % 7;
        string[] daysOfWeek = { "Sun", "Mon", "Tue", "Wed", "Thu", "Fri", "Sat" };
        dayOfWeekText.text = daysOfWeek[dayOfWeek];

        // Display time of day
        timeOfDayText.text = GetTimeOfDayName(hour);
    }

    string GetTimeOfDayName(int hour)
    {
        if (hour >= 5 && hour < 6) return "Dawn";
        if (hour >= 6 && hour < 12) return "Morning";
        if (hour >= 12 && hour < 13) return "Noon";
        if (hour >= 13 && hour < 18) return "Afternoon";
        if (hour >= 18 && hour < 20) return "Evening";
        if (hour >= 20 && hour < 23) return "Night";
        return "Late Night";
    }

    void OnDestroy()
    {
        GameClock.Instance.OnTimeChanged -= UpdateDisplay;
    }
}
```

---

## Example 8: Testing in Edit Mode

```csharp
using UnityEngine;

#if UNITY_EDITOR
using UnityEditor;

public class GameClockDebug : EditorWindow
{
    private int _testDay = 1;
    private int _testHour = 0;
    private int _advanceAmount = 1;

    [MenuItem("Window/GameClock Debug")]
    public static void ShowWindow()
    {
        GetWindow<GameClockDebug>("GameClock");
    }

    void OnGUI()
    {
        GUILayout.Label("GameClock Debug Panel", EditorStyles.boldLabel);

        if (GameClock.Instance == null)
        {
            GUILayout.Label("GameClock not found in scene!", EditorStyles.helpBox);
            return;
        }

        // Display current time
        GUILayout.Label($"Current Time: {GameClock.Instance.GetTimeAsString()}", EditorStyles.boldLabel);

        GUILayout.Space(10);

        // Jump to specific time
        GUILayout.Label("Jump to Time", EditorStyles.boldLabel);
        _testDay = EditorGUILayout.IntField("Day", _testDay);
        _testHour = EditorGUILayout.IntField("Hour", _testHour);

        if (GUILayout.Button("Set Time", GUILayout.Height(30)))
        {
            GameClock.Instance.SetTime(_testDay, _testHour);
        }

        GUILayout.Space(10);

        // Advance time
        GUILayout.Label("Advance Time", EditorStyles.boldLabel);
        _advanceAmount = EditorGUILayout.IntField("Hours", _advanceAmount);

        if (GUILayout.Button("Advance", GUILayout.Height(30)))
        {
            GameClock.Instance.AdvanceHours(_advanceAmount);
        }

        GUILayout.Space(10);

        // Shortcuts
        GUILayout.Label("Quick Actions", EditorStyles.boldLabel);

        if (GUILayout.Button("→ 1 Hour"))
            GameClock.Instance.AdvanceHours(1);

        if (GUILayout.Button("→ 6 Hours (Morning)"))
            GameClock.Instance.SetTime(GameClock.Instance.CurrentDay, 6);

        if (GUILayout.Button("→ 12 Hours (Noon)"))
            GameClock.Instance.SetTime(GameClock.Instance.CurrentDay, 12);

        if (GUILayout.Button("→ 18 Hours (Evening)"))
            GameClock.Instance.SetTime(GameClock.Instance.CurrentDay, 18);

        if (GUILayout.Button("→ Next Day"))
            GameClock.Instance.AdvanceHours(24);

        if (GUILayout.Button("Reset"))
            GameClock.Instance.Reset();
    }
}
#endif
```

---

## Example 9: Event Manager Integration

```csharp
using UnityEngine;

public class TimeEventManager : MonoBehaviour
{
    void Start()
    {
        GameClock.Instance.OnTimeChanged += OnTimeChanged;
    }

    void OnTimeChanged(int day, int hour)
    {
        // Dispatch time-based events to EventManager
        var timeEvent = new GameEvent { eventType = "TimeChanged", day = day, hour = hour };
        EventManager.Instance.DispatchEvent(timeEvent);

        // Specific hour events
        switch (hour)
        {
            case 6:
                EventManager.Instance.TriggerEvent("DayStart");
                break;
            case 12:
                EventManager.Instance.TriggerEvent("Noon");
                break;
            case 18:
                EventManager.Instance.TriggerEvent("Evening");
                break;
            case 20:
                EventManager.Instance.TriggerEvent("Night");
                break;
            case 0:
                EventManager.Instance.TriggerEvent("Midnight");
                break;
        }
    }

    void OnDestroy()
    {
        GameClock.Instance.OnTimeChanged -= OnTimeChanged;
    }
}
```

---

## Example 10: Condition System Integration

```csharp
using UnityEngine;

public class TimeCondition : MonoBehaviour
{
    public enum TimeOfDay { Early, Morning, Afternoon, Evening, Night }

    public bool IsTimeOfDay(TimeOfDay targetTime)
    {
        int hour = GameClock.Instance.CurrentHour;

        return targetTime switch
        {
            TimeOfDay.Early => hour >= 5 && hour < 6,
            TimeOfDay.Morning => hour >= 6 && hour < 12,
            TimeOfDay.Afternoon => hour >= 12 && hour < 18,
            TimeOfDay.Evening => hour >= 18 && hour < 21,
            TimeOfDay.Night => hour >= 21 || hour < 5,
            _ => false
        };
    }

    public bool IsDay(int targetDay)
    {
        return GameClock.Instance.CurrentDay == targetDay;
    }

    public bool IsDayOfWeek(int dayOfWeek)
    {
        return GameClock.Instance.CurrentDay % 7 == dayOfWeek;
    }

    public bool IsTimeRange(int startHour, int endHour)
    {
        int hour = GameClock.Instance.CurrentHour;
        if (startHour < endHour)
            return hour >= startHour && hour < endHour;
        else
            return hour >= startHour || hour < endHour;
    }
}
```

---

## Summary

These examples cover:
1. ✅ Basic time advancement
2. ✅ Event subscriptions
3. ✅ Action-based costs
4. ✅ Time-based conditions
5. ✅ NPC daily routines
6. ✅ Save/load integration
7. ✅ UI display
8. ✅ Edit-mode debugging
9. ✅ Event system integration
10. ✅ Condition system integration

All are production-ready and can be adapted to your specific game needs.
