# Quick Reference: Stat Conditions & Consequences

## TL;DR

StatCondition and StatConsequence are now **fully working**. They:
- ✅ Query real character stats
- ✅ Support any character in the scene
- ✅ Use StatDefinition assets (not enums)
- ✅ Handle missing references safely
- ✅ Integrate with EventManager's pipeline

---

## Setup (5 Minutes)

### 1. Create Assets
```
Assets/
├─ Data/
│  └─ Stats/
│     ├─ StatDatabase.asset (scene reference)
│     ├─ Strength.asset
│     ├─ Charisma.asset
│     └─ Health.asset
├─ Characters/
│  ├─ Protagonist.asset (VNCharacter)
│  └─ NPC_Guard.asset (VNCharacter)
└─ Events/
   ├─ StrengthTestEvent.asset
   └─ CharismaCheckEvent.asset
```

### 2. Scene Setup
```
Hierarchy:
├─ [StatManager] GameObject
│  └─ StatManager component
├─ Protagonist GameObject
│  ├─ CharacterInstance component
│  │  └─ Definition: Protagonist.asset
│  └─ CharacterStats component (auto-created)
└─ NPC_Guard GameObject
   ├─ CharacterInstance component
   │  └─ Definition: NPC_Guard.asset
   └─ CharacterStats component (auto-created)
```

### 3. Event Setup
In EventData Inspector:
```
Conditions:
├─ StatCondition
   ├─ Character: Protagonist
   ├─ Stat: Strength
   ├─ Comparison: GreaterOrEqual
   └─ Value: 80

Consequences:
├─ StatConsequence
   ├─ Character: Protagonist
   ├─ Stat: Strength
   └─ Delta: +10
```

---

## API Reference

### StatCondition

**In EventData:**
```csharp
var condition = new StatCondition
{
    Character = protagonistVNCharacter,
    Stat = strengthDefinition,
    Comparison = ComparisonOperator.GreaterOrEqual,
    Value = 80
};
```

**Runtime (via EventManager):**
```csharp
bool passes = condition.IsMet();  // Returns true/false
string description = condition.Describe();  // "Protagonist's Strength >= 80"
```

### StatConsequence

**In EventData:**
```csharp
var consequence = new StatConsequence
{
    Character = protagonistVNCharacter,
    Stat = healthDefinition,
    Delta = -5  // Damage
};
```

**Runtime (via EventManager):**
```csharp
consequence.Apply();  // Modifies stat via StatManager
string description = consequence.Describe();  // "Modify Protagonist's Health by -5"
```

---

## Common Scenarios

### Scenario 1: Strength Gate
**"Door opens only if strong enough"**

```
Event: "Heavy Door"
├─ Condition: Protagonist's Strength >= 80
└─ On Trigger: Play dialogue, open door
```

**Inspector Setup:**
- Condition Character: Protagonist
- Condition Stat: Strength (StatDefinition)
- Condition Comparison: GreaterOrEqual
- Condition Value: 80

### Scenario 2: Stat Boost Consequence
**"Training event increases strength"**

```
Event: "Train with Master"
├─ Dialogue: "You feel stronger!"
└─ Consequence: Protagonist's Strength += 15
```

**Inspector Setup:**
- Consequence Character: Protagonist
- Consequence Stat: Strength (StatDefinition)
- Consequence Delta: 15

### Scenario 3: Chain of Events
**"Training → Unlock Hard Door"**

```
Event A: "Training"
├─ Consequence: Strength += 20

Event B: "Hard Door"
├─ Condition: Strength >= 80
├─ AutoTrigger: true
└─ Consequence: Strength += 10

Event C: "Master Door"
├─ Condition: Strength >= 90
└─ (only accessible after B)
```

**Flow:**
1. Start: Strength 70
2. Do Training: Strength 90
3. Recheck events
4. Hard Door now eligible → Triggers
5. After Hard Door: Strength 100
6. Master Door now eligible → Could trigger next location change

### Scenario 4: NPC Interactions
**"Persuade NPC with high charisma"**

```
Event: "Negotiate with Guard"
├─ Condition: Protagonist's Charisma >= 70
└─ Result: Guard lets you pass
```

---

## Debugging

### Check Log Output
```
✅ Event evaluating conditions
[EventManager] Event 'Hard Door' passed all conditions.
[StatManager] Protagonist Strength: 85 (unchanged)

❌ Condition failing
[EventManager] Event 'Hard Door' blocked by condition: Protagonist's Strength >= 80
[StatCondition] Character 'Protagonist' instance not found in scene — returning false.

✅ Consequence applying
[StatManager] Protagonist Health: 100 + -10 → 90

❌ Consequence skipped
[StatConsequence] Character is null — consequence skipped.
```

### Verify Setup
```csharp
// In Play Mode console:

// Check StatManager exists
Debug.Log(StatManager.Instance != null ? "✅ StatManager found" : "❌ No StatManager");

// Check character instance
var instance = FindObjectOfType<CharacterInstance>();
Debug.Log(instance != null ? "✅ Character instance found" : "❌ No character");

// Check stat value
if (instance != null)
{
    var strength = Resources.Load<StatDefinition>("Stats/Strength");
    int value = instance.GetStat(strength);
    Debug.Log($"Strength = {value}");
}
```

---

## Warning Messages & Solutions

| Warning | Cause | Solution |
|---------|-------|----------|
| `Character is null` | No VNCharacter assigned | Assign VNCharacter in Inspector |
| `Stat is null` | No StatDefinition assigned | Assign StatDefinition in Inspector |
| `StatManager instance not found` | No StatManager in scene | Create GameObject with StatManager component |
| `Character 'X' instance not found in scene` | CharacterInstance not in scene | Add CharacterInstance to a GameObject with the VNCharacter |

---

## Performance

| Operation | Cost | Frequency |
|-----------|------|-----------|
| Character lookup | O(n) FindObjectsOfType | Once per condition/consequence |
| Stat query | O(1) Dictionary lookup | Once per condition/consequence |
| Event recheck | O(m) all events | After every event completes |

**Typical:** ~1-5ms per event evaluation (not noticeable)

**Optimization:** Cache character instances if you have 100+ characters in scene

---

## Comparison Operators

### Supported
- `Equal (==)` — Stat value equals threshold
- `GreaterOrEqual (>=)` — Stat value at or above threshold
- `LessOrEqual (<=)` — Stat value at or below threshold

### Examples
```csharp
Strength >= 80      // "Strong enough to break door"
Health <= 50        // "Weakened state"
Sanity == 0         // "Broken mind"
```

---

## Field Checklist

### StatCondition Fields
- [ ] Character assigned (VNCharacter)
- [ ] Stat assigned (StatDefinition)
- [ ] Comparison set (==, >=, <=)
- [ ] Value set (threshold)

### StatConsequence Fields
- [ ] Character assigned (VNCharacter)
- [ ] Stat assigned (StatDefinition)
- [ ] Delta set (can be negative)

---

## Example Event JSON (Internal)

```json
{
  "DisplayName": "Heavy Door",
  "Conditions": [
    {
      "_type": "StatCondition",
      "Character": { "guid": "protagonist_asset_guid" },
      "Stat": { "guid": "strength_definition_guid" },
      "Comparison": "GreaterOrEqual",
      "Value": 80
    }
  ],
  "Consequences": [
    {
      "_type": "StatConsequence",
      "Character": { "guid": "protagonist_asset_guid" },
      "Stat": { "guid": "strength_definition_guid" },
      "Delta": 10
    }
  ]
}
```

---

## Integration Diagram

```
Inspector (Designer)
  ↓
Event Asset (EventData)
  ├─ StatCondition (Inspector assigns VNCharacter + StatDefinition)
  └─ StatConsequence (Inspector assigns VNCharacter + StatDefinition)
  ↓
EventManager.EvaluateEventsForLocation()
  ├─ IsEligible()
  │  └─ condition.IsMet()
  │     └─ StatCondition.Evaluate()
  │        ├─ FindCharacterInstance(character)
  │        ├─ StatManager.GetStat(instance, stat)
  │        └─ Compare(value, operator, threshold)
  ↓
EventManager.FinalizeEvent()
  └─ consequence.Apply()
     └─ StatConsequence.Execute()
        ├─ FindCharacterInstance(character)
        └─ StatManager.ModifyStat(instance, stat, delta)
  ↓
EventManager.RecheckEventsAtCurrentLocation()
  └─ Re-evaluate all conditions with updated stats
```

---

## Quick Wins

✅ **Done:** StatCondition fully functional
✅ **Done:** StatConsequence fully functional
✅ **Done:** Character lookup automatic
✅ **Done:** Error handling comprehensive
✅ **Done:** EventManager integration complete

🚀 **Ready to use in production**

---

## See Also

- `README_STAT_SYSTEM.md` — Complete stat system guide
- `STAT_CONDITIONS_CONSEQUENCES.md` — Detailed integration guide
- `REFACTORING_SUMMARY.md` — Before/after comparison
