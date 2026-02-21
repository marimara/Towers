# GameClock Null Check Fix

## Issue
```
error CS0019: Operator '==' cannot be applied to operands of type 'TimeSnapshot' and '<null>'
```

## Root Cause
`TimeSnapshot` is a **struct** (value type), not a **class** (reference type). Structs cannot be null, so the check `if (snapshot == null)` is invalid.

## Solution
Changed the `LoadSnapshot()` method signature to use a **nullable struct**:

```csharp
// Before:
public void LoadSnapshot(TimeSnapshot snapshot)
{
    if (snapshot == null)  // ❌ Error: can't compare struct to null
    {
        return;
    }
}

// After:
public void LoadSnapshot(TimeSnapshot? snapshot)
{
    if (!snapshot.HasValue)  // ✅ Correct: nullable struct check
    {
        Debug.LogWarning("[GameClock] Cannot load null snapshot. Time unchanged.");
        return;
    }

    var snap = snapshot.Value;
    _currentDay = snap.day;
    _currentHour = snap.hour;
}
```

## Changes Made

1. Changed parameter from `TimeSnapshot` to `TimeSnapshot?` (nullable)
2. Changed null check from `if (snapshot == null)` to `if (!snapshot.HasValue)`
3. Unwrap the nullable value with `var snap = snapshot.Value;`
4. Use `snap.day` and `snap.hour` instead of `snapshot.day` and `snapshot.hour`

## Usage
The API remains the same from the caller's perspective:

```csharp
// Can pass null
GameClock.Instance.LoadSnapshot(null);  // Logs warning, does nothing

// Can pass a value
var snap = GameClock.Instance.GetSnapshot();
GameClock.Instance.LoadSnapshot(snap);  // Loads successfully
```

## Error Status
✅ **FIXED** — Compilation should now succeed.
