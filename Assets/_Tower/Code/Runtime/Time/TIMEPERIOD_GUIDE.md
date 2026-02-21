# TimePeriod System — Complete Guide

## Overview

TimePeriod is an enum that divides the 24-hour day into five meaningful periods for narrative and gameplay. It works seamlessly with GameClock to provide period-based conditions and ambiance changes.

**Five Periods:**
- **LateNight** (3–5 AM) — Before sunrise
- **Morning** (6–11 AM) — Sunrise to midday
- **Afternoon** (12–5 PM) — Midday to late afternoon
- **Evening** (6–9 PM) — Sunset to night
- **Night** (10 PM–2 AM) — Night to pre-dawn

---

## Quick Start

### Basic Usage

```csharp
// Access current period
TimePeriod period = GameClock.Instance.CurrentPeriod;

if (period == TimePeriod.Morning)
    Debug.Log("The sun is rising!");

if (period == TimePeriod.Night)
    Debug.Log("It's nighttime. Time to sleep.");
```

### Period Checks

```csharp
// Check if it's daytime (morning + afternoon)
bool isDaytime = GameClock.Instance.CurrentPeriod is TimePeriod.Morning or TimePeriod.Afternoon;

// Check if it's nighttime
bool isNighttime = GameClock.Instance.CurrentPeriod is TimePeriod.Night or TimePeriod.LateNight;

// Check for evening activities
if (GameClock.Instance.CurrentPeriod == TimePeriod.Evening)
    TriggerDinnerEvent();
```

---

## Enum Definition

```csharp
public enum TimePeriod
{
    LateNight = 0,   // 3–5 AM
    Morning = 1,     // 6–11 AM
    Afternoon = 2,   // 12–5 PM
    Evening = 3,     // 6–9 PM
    Night = 4,       // 10 PM–2 AM
}
```

---

## Time Period Reference

### LateNight (3–5 AM)
**Hours:** 3, 4, 5
**Characteristics:**
- Pre-dawn quiet
- Very few NPCs awake
- Bakers and night guards active
- Perfect for: secret meetings, mysterious events, insomnia

**Example Events:**
- Secret meetings in shadows
- Bakers opening shops
- Guards changing shifts
- Supernatural phenomena

### Morning (6–11 AM)
**Hours:** 6, 7, 8, 9, 10, 11
**Characteristics:**
- Sunrise and early day
- NPCs waking up
- Work and training time
- Most social activity begins

**Example Events:**
- Training sessions
- Morning greetings
- Work starts
- Exploration begins

### Afternoon (12–5 PM)
**Hours:** 12, 13, 14, 15, 16, 17
**Characteristics:**
- Peak daylight
- Highest activity
- Social gatherings
- Peak commerce time

**Example Events:**
- Marketplace busiest
- Lunch meetings
- Quest progress
- Public gatherings
- Maximum foot traffic

### Evening (6–9 PM)
**Hours:** 18, 19, 20, 21
**Characteristics:**
- Sunset and transition
- Dinner time
- Social hour
- Winding down

**Example Events:**
- Dinner gatherings
- Evening strolls
- Social meetings
- Taverns open
- Wind-down activities

### Night (10 PM–2 AM)
**Hours:** 22, 23, 0, 1, 2
**Characteristics:**
- Full darkness
- Most NPCs sleeping
- Quiet and dangerous
- Secrets revealed
- Supernatural activity

**Example Events:**
- Sleep cycles
- Secret meetings
- Dangerous creatures
- Supernatural phenomena
- Locked locations (some)

---

## API Reference

### Property

#### `public TimePeriod CurrentPeriod`

Returns the current time period based on the current hour.

```csharp
TimePeriod period = GameClock.Instance.CurrentPeriod;
```

**Type:** TimePeriod enum
**Updated:** Automatically when hour changes
**Performance:** O(1) — instant calculation
**Caching:** Calculated on-demand (not cached)

---

## Usage Patterns

### Pattern 1: Period-Based Events

```csharp
void Start()
{
    GameClock.Instance.OnTimeChanged += (day, hour) =>
    {
        TimePeriod period = GameClock.Instance.CurrentPeriod;

        switch (period)
        {
            case TimePeriod.Morning:
                EventManager.Instance.TriggerEvent("DayStart");
                break;
            case TimePeriod.Evening:
                EventManager.Instance.TriggerEvent("Sunset");
                break;
            case TimePeriod.Night:
                EventManager.Instance.TriggerEvent("Nightfall");
                break;
        }
    };
}
```

### Pattern 2: NPC Availability

```csharp
public class NPCSchedule : MonoBehaviour
{
    public bool IsAvailable()
    {
        TimePeriod period = GameClock.Instance.CurrentPeriod;

        // This NPC is available during daytime and evening
        return period is TimePeriod.Morning
            or TimePeriod.Afternoon
            or TimePeriod.Evening;
    }

    public string GetActivity()
    {
        return GameClock.Instance.CurrentPeriod switch
        {
            TimePeriod.Morning => "Working",
            TimePeriod.Afternoon => "Taking a break",
            TimePeriod.Evening => "Heading home",
            TimePeriod.Night => "Sleeping",
            TimePeriod.LateNight => "Sleeping",
            _ => "Unknown"
        };
    }
}
```

### Pattern 3: Location-Based Access

```csharp
public class Location : MonoBehaviour
{
    public bool IsAccessible()
    {
        TimePeriod period = GameClock.Instance.CurrentPeriod;

        // Tavern is open evening and night
        if (locationName == "Tavern")
            return period is TimePeriod.Evening or TimePeriod.Night;

        // Market is open morning and afternoon
        if (locationName == "Market")
            return period is TimePeriod.Morning or TimePeriod.Afternoon;

        // Shrine is always accessible
        return true;
    }
}
```

### Pattern 4: Ambiance Changes

```csharp
public class AmbianceManager : MonoBehaviour
{
    void Start()
    {
        GameClock.Instance.OnTimeChanged += UpdateAmbiance;
        UpdateAmbiance(GameClock.Instance.CurrentDay, GameClock.Instance.CurrentHour);
    }

    void UpdateAmbiance(int day, int hour)
    {
        TimePeriod period = GameClock.Instance.CurrentPeriod;

        // Change lighting, music, and weather based on period
        switch (period)
        {
            case TimePeriod.LateNight:
                SetLighting(0.2f);  // Very dark
                PlayMusic("mysterious");
                break;
            case TimePeriod.Morning:
                SetLighting(0.8f);  // Bright
                PlayMusic("cheerful");
                break;
            case TimePeriod.Afternoon:
                SetLighting(1.0f);  // Full brightness
                PlayMusic("busy");
                break;
            case TimePeriod.Evening:
                SetLighting(0.6f);  // Dim
                PlayMusic("calm");
                break;
            case TimePeriod.Night:
                SetLighting(0.3f);  // Dark
                PlayMusic("eerie");
                break;
        }
    }

    void OnDestroy()
    {
        GameClock.Instance.OnTimeChanged -= UpdateAmbiance;
    }
}
```

### Pattern 5: Conditional Events

```csharp
public class EventCondition : MonoBehaviour
{
    public bool IsNightEvent()
    {
        return GameClock.Instance.CurrentPeriod == TimePeriod.Night;
    }

    public bool IsDaytimeEvent()
    {
        TimePeriod period = GameClock.Instance.CurrentPeriod;
        return period is TimePeriod.Morning or TimePeriod.Afternoon;
    }

    public bool IsPublicEvent()
    {
        // Events that only happen when many people are awake
        TimePeriod period = GameClock.Instance.CurrentPeriod;
        return period is TimePeriod.Morning
            or TimePeriod.Afternoon
            or TimePeriod.Evening;
    }
}
```

---

## Helper Method

### GetPeriodForHour (Private)

```csharp
private static TimePeriod GetPeriodForHour(int hour)
```

**Purpose:** Maps an hour (0-23) to its corresponding TimePeriod
**Access:** Private (used internally only)
**Implementation:** Switch expression for O(1) lookup
**Fallback:** Returns Night for invalid hours (shouldn't happen with 0-23 input)

**Logic:**
```
Hour 3-5   → LateNight
Hour 6-11  → Morning
Hour 12-17 → Afternoon
Hour 18-21 → Evening
Hour 22-2  → Night
```

---

## Common Period Checks

### Daytime vs Nighttime

```csharp
bool IsDaytime()
{
    return GameClock.Instance.CurrentPeriod is TimePeriod.Morning
        or TimePeriod.Afternoon;
}

bool IsNighttime()
{
    return GameClock.Instance.CurrentPeriod is TimePeriod.Night
        or TimePeriod.LateNight;
}
```

### Social Hours

```csharp
bool IsSocialHour()
{
    // Morning, afternoon, and evening are social
    return GameClock.Instance.CurrentPeriod is TimePeriod.Morning
        or TimePeriod.Afternoon
        or TimePeriod.Evening;
}
```

### Business Hours

```csharp
bool IsBusinessHour()
{
    // Morning and afternoon (typical business times)
    return GameClock.Instance.CurrentPeriod is TimePeriod.Morning
        or TimePeriod.Afternoon;
}
```

### Quiet Hours

```csharp
bool IsQuietHour()
{
    // Night and late night are quiet
    return GameClock.Instance.CurrentPeriod is TimePeriod.Night
        or TimePeriod.LateNight;
}
```

---

## Design Decisions

### Why Five Periods?

Five periods provide:
- ✅ Clear narrative transitions (dawn, day, dusk, night)
- ✅ Enough granularity for meaningful gameplay differences
- ✅ Not too many for complexity
- ✅ Natural human day rhythm

### Why Enum Instead of Ranges?

**Enums provide:**
- ✅ Type safety (can't use invalid values)
- ✅ IDE autocomplete
- ✅ Switch expression optimization
- ✅ Clear semantic meaning
- ✅ Easy to extend if needed

**vs. Hour ranges:**
- ❌ More error-prone
- ❌ Repeated range checks everywhere
- ❌ Less maintainable

### Why Private Helper Method?

The `GetPeriodForHour()` method is private because:
- ✅ Implementation detail
- ✅ Always called the same way (from CurrentPeriod property)
- ✅ Single source of truth for period logic
- ✅ Easy to test and modify
- ✅ No coupling to external code

### No UI Dependency

The TimePeriod enum:
- ✅ Pure data (no game logic)
- ✅ No UI references
- ✅ No hard-coded strings
- ✅ Extensible for any use case

---

## Testing

### Manual Testing

```csharp
// Test all periods by jumping to their hours
GameClock.Instance.SetTime(1, 3);   // LateNight
Assert.AreEqual(TimePeriod.LateNight, GameClock.Instance.CurrentPeriod);

GameClock.Instance.SetTime(1, 8);   // Morning
Assert.AreEqual(TimePeriod.Morning, GameClock.Instance.CurrentPeriod);

GameClock.Instance.SetTime(1, 15);  // Afternoon
Assert.AreEqual(TimePeriod.Afternoon, GameClock.Instance.CurrentPeriod);

GameClock.Instance.SetTime(1, 20);  // Evening
Assert.AreEqual(TimePeriod.Evening, GameClock.Instance.CurrentPeriod);

GameClock.Instance.SetTime(1, 23);  // Night
Assert.AreEqual(TimePeriod.Night, GameClock.Instance.CurrentPeriod);
```

### Period Boundaries

```csharp
// Test period boundaries
for (int hour = 0; hour < 24; hour++)
{
    GameClock.Instance.SetTime(1, hour);
    TimePeriod period = GameClock.Instance.CurrentPeriod;
    Debug.Log($"Hour {hour}: {period}");
}
```

---

## Hour to Period Reference Table

| Hour | Period | Notes |
|------|--------|-------|
| 0–2 | Night | Late night |
| 3–5 | LateNight | Pre-dawn |
| 6–11 | Morning | Sunrise to midday |
| 12–17 | Afternoon | Midday to late afternoon |
| 18–21 | Evening | Sunset to night |
| 22–23 | Night | Night |

---

## See Also

- `GameClock.cs` — Main time management system
- `README_GAMECLOCK.md` — GameClock documentation
- `TimeCondition.cs` — Event conditions based on time

---

## FAQ

**Q: Can I add more periods?**
A: Yes! Add new enum values and update the switch expression in GetPeriodForHour().

**Q: Can I change the hour ranges?**
A: Yes! Update the ranges in the switch expression and the documentation.

**Q: Why is the helper private?**
A: It's an implementation detail. The public API is the CurrentPeriod property.

**Q: Can periods change mid-hour?**
A: No. Periods only change when the hour changes (GameClock.OnTimeChanged fires).

**Q: Can I check the period without the GameClock?**
A: Yes, call `GameClock.Instance.CurrentPeriod` from anywhere.

**Q: What's the performance cost?**
A: O(1) — just a switch expression, instant.

**Q: Can multiple systems use periods simultaneously?**
A: Yes! Many systems can check CurrentPeriod independently.

---

**Status: Production-Ready ✅**

TimePeriod provides clean, type-safe period-based gameplay logic with zero UI coupling.
