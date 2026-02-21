# GameClock System — Complete Reference

## Overview

GameClock is a production-ready singleton MonoBehaviour that manages in-game time for narrative RPGs. It tracks day and hour, handles time advancement with automatic day overflow, and fires events when time changes.

**Key Features:**
- ✅ Singleton pattern with duplicate detection
- ✅ Persistent across scenes (DontDestroyOnLoad)
- ✅ Read-only access to CurrentDay and CurrentHour
- ✅ Automatic day overflow on hour advancement
- ✅ C# events for time changes (no UI code)
- ✅ Action-based time advancement
- ✅ Save/load integration hooks
- ✅ Zero dependencies (no external assets)

---

## Quick Start

### 1. Scene Setup

Create a GameObject called "GameClock" and add the GameClock component:

```
Hierarchy:
├─ GameClock (GameObject)
│  └─ GameClock (Component)
```

That's it. The singleton automatically handles DontDestroyOnLoad.

### 2. Access Time

```csharp
// Read current time
int day = GameClock.Instance.CurrentDay;      // 1, 2, 3, ...
int hour = GameClock.Instance.CurrentHour;    // 0-23

// Get formatted string
string time = GameClock.Instance.GetTimeAsString();  // "Day 5, Hour 14"
```

### 3. Advance Time

```csharp
// Advance by hours
GameClock.Instance.AdvanceHours(2);  // Current hour + 2, overflow to next day if needed

// Advance by action cost
GameClock.Instance.AdvanceByAction();         // +1 hour (default)
GameClock.Instance.AdvanceByAction(3);        // +3 hours (custom cost)
```

### 4. Subscribe to Time Changes

```csharp
void Start()
{
    GameClock.Instance.OnTimeChanged += HandleTimeChanged;
}

void HandleTimeChanged(int day, int hour)
{
    Debug.Log($"Time changed: Day {day}, Hour {hour}");
}

void OnDestroy()
{
    GameClock.Instance.OnTimeChanged -= HandleTimeChanged;
}
```

---

## API Reference

### Properties

#### `public int CurrentDay`

The current in-game day (starting at 1).

```csharp
int day = GameClock.Instance.CurrentDay;  // 1, 2, 3, ...
```

**Notes:**
- Read-only (use AdvanceHours, SetTime, or LoadSnapshot to change)
- Starts at 1 (Day 0 doesn't exist)
- Increments with day overflow

#### `public int CurrentHour`

The current in-game hour (0-23, representing 24-hour format).

```csharp
int hour = GameClock.Instance.CurrentHour;  // 0-23
```

**Hour Reference:**
- 0 = Midnight
- 1-5 = Early morning
- 6-11 = Morning
- 12 = Noon
- 13-17 = Afternoon
- 18-23 = Evening/Night

**Notes:**
- Read-only (use AdvanceHours, SetTime, or LoadSnapshot to change)
- Automatically wraps to next day when advancing past 23
- Automatically wraps to previous day when going below 0

---

### Methods

#### `public void AdvanceHours(int amount)`

Advance in-game time by the specified number of hours. Automatically handles day overflow.

```csharp
// Forward time
GameClock.Instance.AdvanceHours(2);   // Add 2 hours
GameClock.Instance.AdvanceHours(25);  // Add 25 hours (handles overflow)

// Rewind time (for debugging/cheats)
GameClock.Instance.AdvanceHours(-3);  // Subtract 3 hours
```

**Behavior:**
- Hour 24 → Hour 0, Day + 1
- Hour 25 → Hour 1, Day + 1
- Hour -1 → Hour 23, Day - 1
- Zero advance (amount = 0) is a no-op
- Fires OnTimeChanged event (unless amount = 0)

**Parameters:**
- `amount` (int): Number of hours to advance (positive or negative)

**Returns:** void

**Fires:** OnTimeChanged(day, hour) if time changed

#### `public void AdvanceByAction(int actionCost = 1)`

Advance in-game time by the cost of an action. Shorthand for AdvanceHours() with semantic meaning.

```csharp
// Typical action costs
GameClock.Instance.AdvanceByAction();    // 1 hour (default)
GameClock.Instance.AdvanceByAction(1);   // 1 hour (explicit)
GameClock.Instance.AdvanceByAction(2);   // 2 hours
GameClock.Instance.AdvanceByAction(3);   // 3 hours (long action)

// Free action
GameClock.Instance.AdvanceByAction(0);   // 0 hours (no time cost)
```

**Behavior:**
- Identical to AdvanceHours(actionCost)
- Used semantically when time cost represents game action
- Zero cost (0) is a valid no-op

**Parameters:**
- `actionCost` (int, default: 1): Number of hours the action costs

**Returns:** void

**Fires:** OnTimeChanged(day, hour) if time changed

#### `public (int day, int hour) GetCurrentTime()`

Get the current time as a tuple. Useful for save systems.

```csharp
var (day, hour) = GameClock.Instance.GetCurrentTime();
Debug.Log($"Current time: Day {day}, Hour {hour}");
```

**Returns:** Tuple of (int day, int hour)

**Example:**
```csharp
(int saveDay, int saveHour) = GameClock.Instance.GetCurrentTime();
// saveDay = 5
// saveHour = 14
```

#### `public string GetTimeAsString()`

Get the current time as a formatted string. Useful for debugging and simple logging.

```csharp
string time = GameClock.Instance.GetTimeAsString();
Debug.Log(time);  // "Day 5, Hour 14"
```

**Format:** `"Day {day}, Hour {hour:D2}"`

**Returns:** Formatted string

**Notes:**
- Hour is zero-padded (e.g., "Hour 02" not "Hour 2")
- Useful for console logging, not designed for UI display
- UI systems should read CurrentDay/CurrentHour directly

#### `public TimeSnapshot GetSnapshot()`

Snapshot the current time for saving. Used by save systems to capture state.

```csharp
TimeSnapshot snapshot = GameClock.Instance.GetSnapshot();
// Later...
GameClock.Instance.LoadSnapshot(snapshot);
```

**Returns:** TimeSnapshot struct with current day and hour

**Usage in Save System:**
```csharp
class SaveData
{
    public TimeSnapshot gameTime;
    // ... other save data ...
}

void SaveGame()
{
    var save = new SaveData();
    save.gameTime = GameClock.Instance.GetSnapshot();
    // Serialize and write save.gameTime
}
```

#### `public void LoadSnapshot(TimeSnapshot snapshot)`

Restore time from a snapshot. Used by save systems when loading.

```csharp
TimeSnapshot snapshot = /* loaded from file */;
GameClock.Instance.LoadSnapshot(snapshot);
```

**Behavior:**
- Restores day and hour to snapshot values
- Validates loaded data (clamps hour to 0-23, day to >= 1)
- Fires OnTimeChanged event if time changed
- Safe to call with invalid data (validates and logs warnings)

**Parameters:**
- `snapshot` (TimeSnapshot): Snapshot to restore from

**Returns:** void

**Fires:** OnTimeChanged(day, hour) if time changed

**Example:**
```csharp
class SaveData
{
    public TimeSnapshot gameTime;
}

void LoadGame(SaveData save)
{
    GameClock.Instance.LoadSnapshot(save.gameTime);
    // Time is now restored to saved state
}
```

#### `public void SetTime(int day, int hour)`

Set the time directly. Useful for testing, dev commands, or scene transitions.

```csharp
// Jump to specific time
GameClock.Instance.SetTime(10, 14);  // Day 10, Hour 14

// Test early morning
GameClock.Instance.SetTime(1, 3);    // Day 1, Hour 3

// Test late night
GameClock.Instance.SetTime(1, 23);   // Day 1, Hour 23
```

**Behavior:**
- Sets day to max(1, day) — day must be >= 1
- Clamps hour to 0-23
- Fires OnTimeChanged event if time changed
- Useful for developer commands and testing

**Parameters:**
- `day` (int): Target day (will be clamped to >= 1)
- `hour` (int): Target hour (will be clamped to 0-23)

**Returns:** void

**Fires:** OnTimeChanged(day, hour) if time changed

#### `public void Reset()`

Reset time to initial state (Day 1, Hour 0). Useful for new game start or testing.

```csharp
GameClock.Instance.Reset();  // Day 1, Hour 0
```

**Behavior:**
- Sets time to Day 1, Hour 0
- Fires OnTimeChanged event
- Equivalent to SetTime(1, 0)

**Returns:** void

**Fires:** OnTimeChanged(1, 0)

---

### Events

#### `public event Action<int, int> OnTimeChanged`

Fired whenever the in-game time changes (day or hour). Provides new day and hour as parameters.

```csharp
// Subscribe
GameClock.Instance.OnTimeChanged += (day, hour) =>
{
    Debug.Log($"Time is now Day {day}, Hour {hour}");
};

// Or named method
GameClock.Instance.OnTimeChanged += OnClockTick;

void OnClockTick(int day, int hour)
{
    Debug.Log($"Day {day}, Hour {hour}");
}
```

**Signature:** `Action<int, int>` where parameters are (day, hour)

**Fired When:**
- AdvanceHours() changes time
- AdvanceByAction() changes time
- SetTime() changes time
- LoadSnapshot() changes time
- Reset() changes time

**NOT Fired When:**
- AdvanceHours(0) — zero advance
- AdvanceByAction(0) — zero cost action
- SetTime() is called with same day/hour
- LoadSnapshot() with same day/hour

**Subscriber Responsibilities:**
- Don't hold onto day/hour values (read CurrentDay/CurrentHour if needed later)
- Keep event handlers lightweight (heavy logic goes to separate systems)
- Unsubscribe in OnDestroy to avoid memory leaks

---

## Usage Patterns

### Pattern 1: Time-Based Events

```csharp
public class DayStartManager : MonoBehaviour
{
    void Start()
    {
        GameClock.Instance.OnTimeChanged += HandleTimeChanged;
    }

    void HandleTimeChanged(int day, int hour)
    {
        // Trigger special events at specific times
        if (hour == 6)
            EventManager.Instance.TriggerEvent("DayStart");

        if (hour == 20)
            EventManager.Instance.TriggerEvent("DayEnd");
    }

    void OnDestroy()
    {
        GameClock.Instance.OnTimeChanged -= HandleTimeChanged;
    }
}
```

### Pattern 2: Action Time Cost

```csharp
public class ActionSystem : MonoBehaviour
{
    public void TalkToNPC(VNCharacter character, int cost = 1)
    {
        // Do dialogue...
        DialogueRunner.Instance.PlayDialogue(character);

        // Advance time by action cost
        GameClock.Instance.AdvanceByAction(cost);
    }

    public void LongAction(int hours)
    {
        // Do something that takes time...
        GameClock.Instance.AdvanceByAction(hours);
    }
}
```

### Pattern 3: Time-Based Conditions

```csharp
public class TimeBasedEvent : MonoBehaviour
{
    void Start()
    {
        GameClock.Instance.OnTimeChanged += CheckEventConditions;
    }

    void CheckEventConditions(int day, int hour)
    {
        // Only available during morning (6-12)
        if (hour >= 6 && hour < 12)
        {
            Debug.Log("Morning event available!");
        }

        // Only on Fridays (day % 7 == 5, assuming day 1 = Monday)
        if (day % 7 == 5)
        {
            Debug.Log("Friday event available!");
        }

        // Midnight event
        if (hour == 0)
        {
            Debug.Log("Midnight event triggered!");
        }
    }

    void OnDestroy()
    {
        GameClock.Instance.OnTimeChanged -= CheckEventConditions;
    }
}
```

### Pattern 4: Save/Load Integration

```csharp
[System.Serializable]
public class GameSaveData
{
    public TimeSnapshot gameTime;
    public int playerLevel;
    // ... other data ...
}

public class SaveSystem : MonoBehaviour
{
    public void SaveGame()
    {
        var save = new GameSaveData();
        save.gameTime = GameClock.Instance.GetSnapshot();
        save.playerLevel = Player.Instance.Level;

        SaveToFile(save);
    }

    public void LoadGame()
    {
        var save = LoadFromFile();

        GameClock.Instance.LoadSnapshot(save.gameTime);
        Player.Instance.SetLevel(save.playerLevel);
    }

    void SaveToFile(GameSaveData save)
    {
        string json = JsonUtility.ToJson(save);
        System.IO.File.WriteAllText("save.json", json);
    }

    GameSaveData LoadFromFile()
    {
        string json = System.IO.File.ReadAllText("save.json");
        return JsonUtility.FromJson<GameSaveData>(json);
    }
}
```

### Pattern 5: Day-Based Reset

```csharp
public class NPCDailyRoutine : MonoBehaviour
{
    private int _lastSeenDay = -1;

    void Start()
    {
        GameClock.Instance.OnTimeChanged += OnTimeChanged;
    }

    void OnTimeChanged(int day, int hour)
    {
        // Reset daily state when day changes
        if (day != _lastSeenDay)
        {
            _lastSeenDay = day;
            ResetDailyState();
        }
    }

    void ResetDailyState()
    {
        // Reset NPC mood, location, etc. at start of each day
        mood = 5;  // neutral
        location = homeLocation;
    }

    void OnDestroy()
    {
        GameClock.Instance.OnTimeChanged -= OnTimeChanged;
    }
}
```

---

## Data Structure

### TimeSnapshot

```csharp
[System.Serializable]
public struct TimeSnapshot
{
    public int day;
    public int hour;
}
```

**Purpose:** Serializable snapshot of time for save/load operations

**Serializable:** Yes (can be saved to JSON, binary, etc.)

**Example:**
```csharp
TimeSnapshot morning = new TimeSnapshot { day = 1, hour = 6 };
TimeSnapshot evening = new TimeSnapshot { day = 1, hour = 18 };
```

---

## Time Math Examples

### Adding Hours

```csharp
// Hour 22 + 5 hours = Hour 3, next day
GameClock.Instance.SetTime(1, 22);
GameClock.Instance.AdvanceHours(5);
// Result: Day 2, Hour 3

// Hour 0 + 24 hours = Hour 0, next day
GameClock.Instance.SetTime(5, 0);
GameClock.Instance.AdvanceHours(24);
// Result: Day 6, Hour 0
```

### Subtracting Hours

```csharp
// Hour 2 - 5 hours = Hour 21, previous day
GameClock.Instance.SetTime(5, 2);
GameClock.Instance.AdvanceHours(-5);
// Result: Day 4, Hour 21

// Hour 0 - 1 hour = Hour 23, previous day
GameClock.Instance.SetTime(1, 0);
GameClock.Instance.AdvanceHours(-1);
// Result: Day 0, Hour 23 (then clamped to Day 1, Hour 23)
```

### Time Differences

```csharp
int morningHour = 6;
int eveningHour = 18;
int hoursDifference = eveningHour - morningHour;  // 12 hours
```

---

## Limitations & Design Notes

### Limitations

1. **Time is Manual** — Clock doesn't advance automatically. Systems must call AdvanceHours() or AdvanceByAction().
   - *By design:* Turn-based RPGs don't need real-time time passage

2. **No Recurring Events** — No built-in daily/weekly schedule system.
   - *Solution:* Implement on top using OnTimeChanged event

3. **No Pausing** — No built-in pause mechanism (game handles pausing separately).
   - *By design:* Clock is just a state manager, not a game state manager

4. **No Negative Days** — Days can't go below 1.
   - *By design:* Day 1 is always the starting point. Pre-game events aren't tracked.

### Design Decisions

**Why Singleton?**
- Only one time source needed in the game
- Convenient global access from anywhere
- Prevents duplicate time tracking

**Why DontDestroyOnLoad?**
- Time should persist across scene loads (not reset on location change)
- Prevents time from being reset accidentally during scene transitions

**Why Events Instead of Polling?**
- Event-driven is more efficient than polling every frame
- Subscribers only run when time actually changes
- Decouples time system from dependent systems

**Why No UI Code?**
- Separation of concerns (model vs. view)
- Time system doesn't depend on UI framework
- Multiple UI systems can display time independently

**Why No Save System?**
- Save system is game-specific
- GameClock provides hooks (GetSnapshot, LoadSnapshot)
- Allows save system to own serialization format

---

## Testing

### Manual Testing in Play Mode

```csharp
// Test in Awake() or Start() with breakpoints
void TestGameClock()
{
    // Test initial state
    Assert.AreEqual(1, GameClock.Instance.CurrentDay);
    Assert.AreEqual(0, GameClock.Instance.CurrentHour);

    // Test advancement
    GameClock.Instance.AdvanceHours(5);
    Assert.AreEqual(1, GameClock.Instance.CurrentDay);
    Assert.AreEqual(5, GameClock.Instance.CurrentHour);

    // Test overflow
    GameClock.Instance.AdvanceHours(20);
    Assert.AreEqual(2, GameClock.Instance.CurrentDay);
    Assert.AreEqual(1, GameClock.Instance.CurrentHour);

    // Test action cost
    GameClock.Instance.Reset();
    GameClock.Instance.AdvanceByAction(3);
    Assert.AreEqual(3, GameClock.Instance.CurrentHour);

    // Test snapshot
    var snapshot = GameClock.Instance.GetSnapshot();
    GameClock.Instance.AdvanceHours(100);
    GameClock.Instance.LoadSnapshot(snapshot);
    Assert.AreEqual(3, GameClock.Instance.CurrentHour);

    Debug.Log("✓ All GameClock tests passed");
}
```

### Unit Testing

```csharp
[TestFixture]
public class GameClockTests
{
    [SetUp]
    public void Setup()
    {
        // Create GameClock instance
        var go = new GameObject("GameClock");
        go.AddComponent<GameClock>();
    }

    [Test]
    public void AdvanceHours_WithinDay_Updates Hour()
    {
        GameClock.Instance.AdvanceHours(5);
        Assert.AreEqual(5, GameClock.Instance.CurrentHour);
    }

    [Test]
    public void AdvanceHours_Overflow_IncrementsDay()
    {
        GameClock.Instance.SetTime(1, 20);
        GameClock.Instance.AdvanceHours(10);
        Assert.AreEqual(2, GameClock.Instance.CurrentDay);
        Assert.AreEqual(6, GameClock.Instance.CurrentHour);
    }

    [Test]
    public void OnTimeChanged_IsFired()
    {
        bool fired = false;
        (int day, int hour) receivedTime = (0, 0);

        GameClock.Instance.OnTimeChanged += (d, h) =>
        {
            fired = true;
            receivedTime = (d, h);
        };

        GameClock.Instance.AdvanceHours(5);

        Assert.IsTrue(fired);
        Assert.AreEqual((1, 5), receivedTime);
    }
}
```

---

## FAQ

**Q: What's the difference between AdvanceHours and AdvanceByAction?**
A: They're identical functionally. AdvanceByAction() is semantic sugar for when the advancement represents a game action with a time cost.

**Q: Can I pause time?**
A: No. GameClock is just a state manager. Implement pause at the game level by preventing AdvanceHours/AdvanceByAction calls.

**Q: Can I set a custom time of day?**
A: Yes, use SetTime(day, hour). Time is clamped to valid ranges.

**Q: What happens if I load an invalid snapshot?**
A: GameClock validates and clamps: hour to 0-23, day to >= 1. Logs warnings for invalid data.

**Q: Does time advance in real-time?**
A: No. Time is manual — only advances when you call AdvanceHours() or AdvanceByAction().

**Q: Can multiple systems subscribe to OnTimeChanged?**
A: Yes, use += to add listeners. Remember to -= in OnDestroy to avoid memory leaks.

**Q: How do I implement daily quests that reset each day?**
A: Subscribe to OnTimeChanged and track the last seen day. When day changes, reset daily state.

**Q: Can days go negative?**
A: No. Days are clamped to >= 1. The lowest time is Day 1, Hour 0 (midnight of day 1).

---

## See Also

- TimeCondition.cs — Event condition that checks in-game time
- EventManager.cs — Event system that can be tied to time changes
- LocationManager.cs — Location system that can react to time

---

## Checklist for Integration

- [ ] Create GameObject "GameClock" in your bootstrap/main scene
- [ ] Add GameClock component to that GameObject
- [ ] Don't instantiate GameClock in code (singleton handles it)
- [ ] Subscribe to OnTimeChanged in systems that care about time
- [ ] Call AdvanceHours() or AdvanceByAction() when appropriate
- [ ] Use GetSnapshot()/LoadSnapshot() in your save system
- [ ] Unsubscribe from OnTimeChanged in OnDestroy
- [ ] Test day overflow with SetTime(1, 20) → AdvanceHours(10)
- [ ] Test negative days with SetTime(1, 2) → AdvanceHours(-5)
- [ ] Test action costs with AdvanceByAction(2)

---

**Status: Production-Ready ✅**

This system is designed for production use. It's minimal, clean, and focuses on the core responsibility: managing in-game time.
