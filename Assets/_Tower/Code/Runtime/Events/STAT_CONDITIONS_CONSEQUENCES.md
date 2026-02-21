# Stat Conditions & Consequences — Integration Guide

## Overview

StatCondition and StatConsequence now provide **complete, working integration** with the character stats system. They:
- Query actual character stats from StatManager
- Work with any character in the scene (via VNCharacter reference)
- Fail safely with detailed warning logs
- Integrate seamlessly with EventManager's event pipeline

## StatCondition

### Purpose
Evaluates whether a character's stat meets a numeric threshold, blocking or allowing events based on the result.

### Fields
| Field | Type | Purpose |
|-------|------|---------|
| `Character` | VNCharacter | The character definition to check. The character instance in the scene is found automatically. |
| `Stat` | StatDefinition | Which stat to evaluate. |
| `Comparison` | ComparisonOperator | How to compare (==, >=, <=). |
| `Value` | int | The threshold to compare against. |

### How It Works

1. **Character Lookup:** Uses `FindObjectsOfType<CharacterInstance>()` to find a character instance whose `Definition` matches the assigned VNCharacter.

2. **Stat Query:** Calls `StatManager.Instance.GetStat(characterInstance, stat)` to read the character's current stat value.

3. **Comparison:** Applies the configured operator (e.g., `statValue >= threshold`).

4. **Guarding:** Fails safely if:
   - Character is null
   - Stat is null
   - StatManager singleton not found
   - Character instance not found in scene

### Example Usage

**Scenario:** An event should only trigger if the protagonist has Strength ≥ 80.

```
EventData "Strong Door Smash"
├─ AutoTrigger: true
└─ Conditions:
   └─ StatCondition
      ├─ Character: Protagonist (VNCharacter asset)
      ├─ Stat: Strength (StatDefinition asset)
      ├─ Comparison: GreaterOrEqual
      └─ Value: 80
```

**Execution Flow:**
1. Player enters location with this event
2. EventManager evaluates all events
3. StatCondition.Evaluate() is called:
   - Finds Protagonist CharacterInstance in scene
   - Queries Protagonist's Strength via StatManager
   - Checks if Strength ≥ 80
   - Returns true/false based on comparison
4. If true and priority is highest, event fires

### Debug Output

**Success Example:**
```
[EventManager] Event 'Strong Door Smash' passed all conditions.
[StatManager] Protagonist Strength: 75 (unchanged)
```

**Failure Examples:**
```
[StatCondition] Character is null — condition will always fail.
[StatCondition] Stat is null — condition will always fail.
[StatCondition] StatManager instance not found — returning false.
[StatCondition] Character 'Protagonist' instance not found in scene — returning false.
```

### Describe Output

For debugging and UI display:
```
"Protagonist's Strength >= 80"    // Valid
"[Character not assigned]'s Strength >= 80"    // Character null
"Protagonist's [Stat not assigned] >= 80"      // Stat null
```

---

## StatConsequence

### Purpose
Modifies a character's stat after an event completes, allowing stat changes to unblock subsequent conditions during the recheck phase.

### Fields
| Field | Type | Purpose |
|-------|------|---------|
| `Character` | VNCharacter | The character definition to modify. The character instance in the scene is found automatically. |
| `Stat` | StatDefinition | Which stat to modify. |
| `Delta` | int | Amount to add (positive) or subtract (negative). Respects min/max clamping. |

### How It Works

1. **Character Lookup:** Uses `FindObjectsOfType<CharacterInstance>()` to find a character instance whose `Definition` matches the assigned VNCharacter.

2. **Stat Modification:** Calls `StatManager.Instance.ModifyStat(characterInstance, stat, delta)` to apply the change.

3. **Clamping:** StatManager respects `StatDefinition.ClampToRange` to keep values within bounds.

4. **Guarding:** Skips safely if:
   - Character is null
   - Stat is null
   - StatManager singleton not found
   - Character instance not found in scene
   - Delta is 0 (no-op, still logged)

### Example Usage

**Scenario:** An event lowers the protagonist's Sanity by 5, which might unblock a "low sanity" condition.

```
EventData "Horrifying Revelation"
├─ DialogueGraph: scary_dialogue
└─ Consequences:
   └─ StatConsequence
      ├─ Character: Protagonist (VNCharacter asset)
      ├─ Stat: Sanity (StatDefinition asset)
      └─ Delta: -5
```

**Execution Flow:**
1. Event dialogue plays and ends
2. EventManager.FinalizeEvent() applies consequences
3. StatConsequence.Execute() is called:
   - Finds Protagonist CharacterInstance in scene
   - Calls StatManager.ModifyStat(protagonist, sanity, -5)
   - Sanity is reduced by 5 (respecting min/max bounds)
4. EventManager rechecks all events at current location
5. If another event has condition "Sanity ≤ 30" and was previously blocked, it now may fire

### Debug Output

**Success Example:**
```
[StatConsequence] Protagonist Sanity: 50 + -5 → 45
```

**Failure Examples:**
```
[StatConsequence] Character is null — consequence skipped.
[StatConsequence] Stat is null — consequence skipped.
[StatConsequence] StatManager instance not found — consequence skipped.
[StatConsequence] Character 'Protagonist' instance not found in scene — consequence skipped.
```

**No-Op Example (Delta = 0):**
```
// No log output — zero deltas are silently skipped
```

### Describe Output

For debugging and UI display:
```
"Modify Protagonist's Sanity by -5"    // Valid
"Modify [Character not assigned]'s Sanity by -5"    // Character null
"Modify Protagonist's [Stat not assigned] by -5"    // Stat null
```

---

## Integration with EventManager

The event pipeline handles stat conditions and consequences in this order:

```
Player enters location
        ↓
EventManager evaluates all events at location
├─ Filter 1: RequiredLocation matches
├─ Filter 2: Not already completed (if OneTime)
└─ Filter 3: All Conditions pass
   └─ StatCondition.IsMet() calls Evaluate()
              ↓
    Eligible events selected
              ↓
    Highest priority event auto-triggers (if AutoTrigger=true)
              ↓
    OnEventTriggered fires
              ↓
    Dialogue plays
              ↓
    OnEventCompleted fires
              ↓
    Consequences applied
    └─ StatConsequence.Apply() calls Execute()
              ↓
    Event marked as completed
              ↓
    RecheckEventsAtCurrentLocation
    └─ All conditions re-evaluated with updated stats
    └─ New eligible events may now fire
```

### Example Chain

**Setup:**
- Event A: "Strength Test" — requires Strength ≥ 80
  - Consequence: Strength += 10
- Event B: "Final Challenge" — requires Strength ≥ 85
  - AutoTrigger: true

**Initial State:** Protagonist has Strength 78

**Execution:**
1. Location change → Evaluate Event A
   - StatCondition: 78 >= 80? **NO** → Blocked
   - Evaluate Event B
   - StatCondition: 78 >= 85? **NO** → Blocked

2. Player triggers Event A manually
   - Dialogue plays
   - Consequence applies: Strength 78 + 10 = 88
   - Recheck at location
   - Evaluate Event B
   - StatCondition: 88 >= 85? **YES** → Eligible!
   - AutoTrigger = true → Event B fires automatically

---

## Common Patterns

### Pattern 1: Gating Events by Stat Threshold

```
Event: "Expert Negotiation"
Condition: Character Charisma >= 75
Effect: Player gains Favor
```

### Pattern 2: Progressive Story Unlocks

```
Event A: "First Training"
└─ Consequence: Strength += 10

Event B: "Intermediate Training"
└─ Condition: Strength >= 50
└─ Consequence: Strength += 15

Event C: "Master Training"
└─ Condition: Strength >= 65
└─ Consequence: Strength += 20
```

### Pattern 3: Negative Consequences

```
Event: "Cursed Artifact"
└─ Consequence: Max Health -= 10 (creates permanent debuff)
```

### Pattern 4: Mutual Stat Checks (Multiple Characters)

```
Event: "Character A confronts Character B"
Conditions:
├─ StatCondition (Character A, Charisma >= 60)
└─ StatCondition (Character B, Health >= 30)

If both true: Event triggers
```

---

## Setup Checklist

- [ ] Created StatManager singleton in scene
- [ ] Created StatDatabase with all stat definitions
- [ ] Created character prefabs with CharacterInstance component
- [ ] Assigned VNCharacter definitions to CharacterInstance components
- [ ] Created EventData assets with StatCondition/StatConsequence
- [ ] Assigned VNCharacter and StatDefinition fields in Inspector
- [ ] Set ComparisonOperator and Value for conditions
- [ ] Set Delta for consequences
- [ ] Tested event triggering in play mode
- [ ] Verified log output for successful evaluations
- [ ] Verified warning logs for missing references

---

## Troubleshooting

### Condition Always Returns False

**Symptom:** StatCondition never evaluates to true, even with matching values.

**Check:**
1. Is StatManager singleton created and in scene?
   ```csharp
   Debug.Assert(StatManager.Instance != null, "StatManager not found");
   ```

2. Is character instance in scene?
   ```csharp
   var instance = FindObjectOfType<CharacterInstance>();
   Debug.Assert(instance != null && instance.Definition == myCharacter, "Character not found");
   ```

3. Is stat definition assigned?
   ```csharp
   Debug.Assert(condition.Stat != null, "Stat definition not assigned");
   ```

4. Check the Inspector logs for detailed error messages

### Consequence Not Applying

**Symptom:** StatConsequence runs but stat value doesn't change.

**Check:**
1. Is StatManager singleton created and in scene?
2. Is character instance in scene?
3. Is stat definition assigned?
4. Is Delta non-zero?
5. Check if stat hit min/max clamp bounds
   ```csharp
   // If value = 100, MaxValue = 100, Delta = +5, final = 100 (clamped)
   ```

### Character Not Found

**Symptom:** Warning "Character instance not found in scene"

**Fix:**
1. Verify CharacterInstance component exists on a GameObject
2. Verify CharacterInstance.Definition is assigned to correct VNCharacter
3. Ensure GameObject is active in scene (inactive objects not found by FindObjectsOfType)
4. Check that VNCharacter reference in condition/consequence matches the instance's Definition

### Log Spam

**Symptom:** Same warning logged every frame

**Cause:** StatCondition.Evaluate() is called repeatedly during every EventManager evaluation.

**This is expected behavior** — conditions are re-evaluated frequently to check for state changes.

To reduce noise, filter logs or check conditions only when relevant (e.g., only when location changes).

---

## Performance Notes

### Character Lookup
```csharp
FindObjectsOfType<CharacterInstance>()  // O(n) where n = character instances in scene
```

**Optimization:** Cache character instances if you have many characters:
```csharp
private static Dictionary<VNCharacter, CharacterInstance> _characterCache = new();

// Build cache on scene load
foreach (var instance in FindObjectsOfType<CharacterInstance>())
    _characterCache[instance.Definition] = instance;

// Use cache in conditions/consequences
var instance = _characterCache[Character];
```

### Stat Queries
```csharp
StatManager.GetStat()  // O(1) dictionary lookup in CharacterStats
```

**This is fast** — no optimization needed for typical scenarios.

---

## Testing Example

```csharp
[TestMethod]
public void StatCondition_WithMatchingStat_ReturnsTrue()
{
    // Setup
    var character = Resources.Load<VNCharacter>("Test/TestCharacter");
    var strength = Resources.Load<StatDefinition>("Stats/Strength");

    var instance = new GameObject().AddComponent<CharacterInstance>();
    instance.Definition = character;

    StatManager.Instance.SetStat(instance, strength, 100);

    // Create condition
    var condition = new StatCondition
    {
        Character = character,
        Stat = strength,
        Comparison = ComparisonOperator.GreaterOrEqual,
        Value = 80
    };

    // Act
    bool result = condition.IsMet();

    // Assert
    Assert.IsTrue(result);
}
```
