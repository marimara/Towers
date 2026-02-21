# GameClock — Quick Reference

## Minimal Setup

```csharp
// 1. Scene Setup: Create "GameClock" GameObject with GameClock component
// That's it! Singleton handles the rest.

// 2. Access time
int day = GameClock.Instance.CurrentDay;
int hour = GameClock.Instance.CurrentHour;

// 3. Advance time
GameClock.Instance.AdvanceHours(2);          // +2 hours
GameClock.Instance.AdvanceByAction();        // +1 hour (default)
GameClock.Instance.AdvanceByAction(3);       // +3 hours

// 4. Listen to changes
GameClock.Instance.OnTimeChanged += (day, hour) =>
{
    Debug.Log($"Time changed to Day {day}, Hour {hour}");
};
```

---

## API Cheat Sheet

### Read State
```csharp
int day = GameClock.Instance.CurrentDay;
int hour = GameClock.Instance.CurrentHour;
var (day, hour) = GameClock.Instance.GetCurrentTime();
string timeStr = GameClock.Instance.GetTimeAsString();  // "Day 5, Hour 14"
```

### Advance Time
```csharp
GameClock.Instance.AdvanceHours(amount);        // +/- hours (handles overflow)
GameClock.Instance.AdvanceByAction(cost = 1);   // Semantic: action costs time
```

### Set Time
```csharp
GameClock.Instance.SetTime(day, hour);          // Jump to specific time
GameClock.Instance.Reset();                     // Day 1, Hour 0
```

### Save/Load
```csharp
TimeSnapshot snap = GameClock.Instance.GetSnapshot();
GameClock.Instance.LoadSnapshot(snap);
```

### Events
```csharp
GameClock.Instance.OnTimeChanged += (day, hour) => { /* ... */ };
GameClock.Instance.OnTimeChanged -= HandleTimeChanged;
```

---

## Common Patterns

### Pattern 1: Time-Based Events
```csharp
void Start()
{
    GameClock.Instance.OnTimeChanged += (day, hour) =>
    {
        if (hour == 6) EventManager.Instance.TriggerEvent("Dawn");
        if (hour == 18) EventManager.Instance.TriggerEvent("Dusk");
        if (hour == 0) EventManager.Instance.TriggerEvent("Midnight");
    };
}
```

### Pattern 2: Action Time Cost
```csharp
void TalkToNPC(int hours = 1)
{
    DialogueRunner.Instance.PlayDialogue(npc);
    GameClock.Instance.AdvanceByAction(hours);
}

void RestAtInn(int hours = 8)
{
    GameClock.Instance.AdvanceByAction(hours);
}
```

### Pattern 3: Daily Reset
```csharp
private int _lastSeenDay = -1;

void Start()
{
    GameClock.Instance.OnTimeChanged += OnTimeChanged;
}

void OnTimeChanged(int day, int hour)
{
    if (day != _lastSeenDay)
    {
        _lastSeenDay = day;
        ResetDailyState();  // Triggers at midnight
    }
}
```

### Pattern 4: Time-Based Conditions
```csharp
bool IsMorning() => GameClock.Instance.CurrentHour >= 6 && CurrentHour < 12;
bool IsNight() => GameClock.Instance.CurrentHour >= 20 || CurrentHour < 6;
bool IsWeekend() => GameClock.Instance.CurrentDay % 7 >= 5;  // Days 5-6
```

### Pattern 5: Save/Load
```csharp
[System.Serializable]
class SaveData
{
    public TimeSnapshot time;
}

void Save()
{
    var save = new SaveData { time = GameClock.Instance.GetSnapshot() };
    // Write save to disk...
}

void Load()
{
    var save = LoadFromDisk();
    GameClock.Instance.LoadSnapshot(save.time);
}
```

---

## Hour Reference

```
0  = Midnight
1  = 1 AM
6  = 6 AM (typical wakeup)
12 = Noon
18 = 6 PM (typical dinner)
20 = 8 PM
23 = 11 PM (late night)
```

---

## Time Math

| Operation | Before | After |
|-----------|--------|-------|
| Hour 22 + 5h | D1 H22 | D2 H3 |
| Hour 0 - 1h | D5 H0 | D4 H23 |
| Hour 12 + 12h | D1 H12 | D2 H0 |
| Hour 23 + 25h | D1 H23 | D3 H24→0 |

---

## Common Questions

**Q: How do I pause time?**
A: Just don't call AdvanceHours(). Pause is at game level, not clock level.

**Q: Can I have fractional hours?**
A: No. Time is integer hours. Design around this (e.g., 30-min actions = 0.5 hours round up/down).

**Q: How do I check if it's a specific day of the week?**
A: `int dayOfWeek = GameClock.Instance.CurrentDay % 7;` (0=Sunday, 6=Saturday, adjust as needed)

**Q: Can days go negative?**
A: No. Days are clamped to >= 1.

**Q: Do I need to unsubscribe from OnTimeChanged?**
A: Yes! Always unsubscribe in OnDestroy to avoid memory leaks.

---

## One-Liners

```csharp
// Get current time as string
Debug.Log(GameClock.Instance.GetTimeAsString());

// Check if it's morning
if (GameClock.Instance.CurrentHour >= 6 && GameClock.Instance.CurrentHour < 12) { }

// Advance by 1 hour
GameClock.Instance.AdvanceHours(1);

// Get time tuple
var (day, hour) = GameClock.Instance.GetCurrentTime();

// Save/restore time
TimeSnapshot snap = GameClock.Instance.GetSnapshot();
// ... later ...
GameClock.Instance.LoadSnapshot(snap);

// Reset to start
GameClock.Instance.Reset();

// Jump to specific time
GameClock.Instance.SetTime(10, 14);  // Day 10, 2 PM
```

---

## Testing in Console

```csharp
// Test advancement
GameClock.Instance.AdvanceHours(5);
Debug.Log(GameClock.Instance.GetTimeAsString());

// Test overflow
GameClock.Instance.SetTime(1, 22);
GameClock.Instance.AdvanceHours(5);  // D2 H3
Debug.Log(GameClock.Instance.GetTimeAsString());

// Test event
int callCount = 0;
GameClock.Instance.OnTimeChanged += (d, h) => callCount++;
GameClock.Instance.AdvanceHours(1);
Debug.Log($"Event fired {callCount} times");
```

---

## Integration Checklist

- [ ] Create "GameClock" GameObject with GameClock component
- [ ] Systems subscribe to OnTimeChanged
- [ ] Call AdvanceHours/AdvanceByAction when appropriate
- [ ] Save system uses GetSnapshot/LoadSnapshot
- [ ] Unsubscribe in OnDestroy
- [ ] Test day overflow
- [ ] Test negative advancement
- [ ] Test save/load roundtrip

---

**That's it! Simple, minimal, production-ready.**
