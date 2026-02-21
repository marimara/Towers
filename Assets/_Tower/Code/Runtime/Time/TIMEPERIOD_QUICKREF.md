# TimePeriod — Quick Reference

## Five Time Periods

```
LateNight  = 3–5 AM    (pre-dawn, quiet, eerie)
Morning    = 6–11 AM   (sunrise to midday, active)
Afternoon  = 12–5 PM   (peak activity, brightest)
Evening    = 6–9 PM    (sunset, social, calm)
Night      = 10 PM–2 AM (darkness, sleep, secrets)
```

## Basic Usage

```csharp
// Get current period
TimePeriod period = GameClock.Instance.CurrentPeriod;

// Check specific period
if (period == TimePeriod.Morning)
    Debug.Log("Good morning!");

// Check multiple periods
if (period is TimePeriod.Evening or TimePeriod.Night)
    Debug.Log("It's getting late!");

// Switch on period
switch (period)
{
    case TimePeriod.LateNight:
        SetLighting(0.2f);
        break;
    case TimePeriod.Morning:
        SetLighting(0.8f);
        break;
    // ... etc
}
```

## Common Checks

```csharp
// Daytime (morning + afternoon)
bool isDaytime = period is TimePeriod.Morning or TimePeriod.Afternoon;

// Nighttime (night + late night)
bool isNighttime = period is TimePeriod.Night or TimePeriod.LateNight;

// Quiet hours
bool isQuiet = period is TimePeriod.Night or TimePeriod.LateNight;

// Social hours
bool isSocial = period is TimePeriod.Morning or TimePeriod.Afternoon or TimePeriod.Evening;

// Business hours (morning + afternoon)
bool isBusiness = period is TimePeriod.Morning or TimePeriod.Afternoon;
```

## Period Characteristics

| Period | Time | Vibe | NPCs | Use Case |
|--------|------|------|------|----------|
| **LateNight** | 3–5 AM | Eerie, secret | Few awake | Mysterious events |
| **Morning** | 6–11 AM | Bright, active | Waking up | Training, work start |
| **Afternoon** | 12–5 PM | Busy, social | Peak activity | Markets, gatherings |
| **Evening** | 6–9 PM | Calm, transition | Dinner time | Social meetings |
| **Night** | 10 PM–2 AM | Dark, sleep | Sleeping | Sleep, secrets |

## Hour to Period Map

```
Hour → Period
0–2  → Night
3–5  → LateNight
6–11 → Morning
12–17 → Afternoon
18–21 → Evening
22–23 → Night
```

## Common Patterns

### Pattern 1: NPC Availability
```csharp
bool IsAvailable()
{
    return GameClock.Instance.CurrentPeriod is TimePeriod.Morning
        or TimePeriod.Afternoon
        or TimePeriod.Evening;  // Not available at night
}
```

### Pattern 2: Location Access
```csharp
bool CanEnterTavern()
{
    return GameClock.Instance.CurrentPeriod is TimePeriod.Evening
        or TimePeriod.Night;  // Closed during day
}
```

### Pattern 3: Ambiance Update
```csharp
void UpdateAmbiance()
{
    var period = GameClock.Instance.CurrentPeriod;

    float lighting = period switch
    {
        TimePeriod.LateNight => 0.2f,
        TimePeriod.Morning => 0.8f,
        TimePeriod.Afternoon => 1.0f,
        TimePeriod.Evening => 0.6f,
        TimePeriod.Night => 0.3f,
        _ => 0.5f
    };

    SetLighting(lighting);
}
```

### Pattern 4: Event Triggers
```csharp
void OnTimeChanged(int day, int hour)
{
    var period = GameClock.Instance.CurrentPeriod;

    if (period == TimePeriod.Evening)
        EventManager.Instance.TriggerEvent("Sunset");

    if (period == TimePeriod.Night && hour == 22)
        EventManager.Instance.TriggerEvent("NightfallBells");
}
```

## One-Liners

```csharp
// Check morning
if (GameClock.Instance.CurrentPeriod == TimePeriod.Morning) { }

// Check if daytime
if (GameClock.Instance.CurrentPeriod is TimePeriod.Morning or TimePeriod.Afternoon) { }

// Get period and use
var period = GameClock.Instance.CurrentPeriod;

// All periods available
void CheckAllPeriods()
{
    foreach (TimePeriod period in System.Enum.GetValues(typeof(TimePeriod)))
        Debug.Log(period);
}
```

## Integration

### With GameClock
```csharp
int day = GameClock.Instance.CurrentDay;
int hour = GameClock.Instance.CurrentHour;
TimePeriod period = GameClock.Instance.CurrentPeriod;  // NEW!
```

### With Events
```csharp
GameClock.Instance.OnTimeChanged += (day, hour) =>
{
    var period = GameClock.Instance.CurrentPeriod;
    // React to period change...
};
```

### With NPCs
```csharp
public class NPC
{
    public bool CanTalk()
    {
        // Only talk during social hours
        return GameClock.Instance.CurrentPeriod
            is TimePeriod.Morning or TimePeriod.Afternoon or TimePeriod.Evening;
    }
}
```

## Testing

```csharp
// Test period for each hour
for (int hour = 0; hour < 24; hour++)
{
    GameClock.Instance.SetTime(1, hour);
    Debug.Log($"Hour {hour}: {GameClock.Instance.CurrentPeriod}");
}

// Verify specific hours
GameClock.Instance.SetTime(1, 8);   // Should be Morning
GameClock.Instance.SetTime(1, 15);  // Should be Afternoon
GameClock.Instance.SetTime(1, 23);  // Should be Night
```

## FAQ

**Q: How many periods are there?**
A: 5 periods: LateNight, Morning, Afternoon, Evening, Night

**Q: Can I add more periods?**
A: Yes, add enum values and update the switch in GetPeriodForHour()

**Q: What's the performance cost?**
A: O(1) — instant switch expression

**Q: Can periods span multiple days?**
A: No, periods are hour-based within a single day

**Q: Can I check period without GameClock?**
A: No, must use GameClock.Instance.CurrentPeriod

**Q: Do periods fire an event when they change?**
A: They use the existing OnTimeChanged event (when hour changes)

---

**See Also:** TIMEPERIOD_GUIDE.md for full documentation
