# Stat Conditions & Consequences — Practical Examples

## Complete Working Examples

### Example 1: Simple Strength Gate

**Scenario:** A heavy door that only opens if the protagonist is strong enough.

#### Setup
```
Assets/Stats/
  └─ Strength.asset

Assets/Characters/
  └─ Protagonist.asset

Assets/Events/
  └─ HeavyDoor_Event.asset
```

#### Event Configuration (in Inspector)

```
EventData: "Heavy Door"
├─ DisplayName: "Heavy Door"
├─ DialogueGraph: heavy_door_dialogue
├─ AutoTrigger: false (player must trigger manually)
│
├─ Conditions (List):
│  └─ Element 0: StatCondition
│     ├─ Character: Protagonist
│     ├─ Stat: Strength
│     ├─ Comparison: GreaterOrEqual
│     └─ Value: 80
│
└─ Consequences (List):
   └─ (none - no stat change)
```

#### Dialogue (example script)
```
NODE: Start
  "This heavy door is sealed shut. You'll need great strength to force it open."

NODE: Check Strength
  IF Character.Strength >= 80:
    → go to "BreakDoor"
  ELSE:
    → go to "TooWeak"

NODE: BreakDoor
  "You put all your strength into it..."
  "The door creaks... and suddenly BURSTS OPEN!"
  → END

NODE: TooWeak
  "You're not strong enough. You need to train more."
  → END
```

#### How It Works
1. Player approaches the location with this event
2. EventManager evaluates: "Is Protagonist's Strength >= 80?"
3. If false → event blocked, not available to trigger
4. If true → event available
5. Player triggers the dialogue
6. Based on stat, shows appropriate outcome

---

### Example 2: Training Chain (Progressive Unlocking)

**Scenario:** Three training events that unlock progressively as strength increases.

#### Assets
```
Assets/Stats/
  └─ Strength.asset

Assets/Characters/
  └─ Protagonist.asset

Assets/Events/
  ├─ BasicTraining_Event.asset
  ├─ IntermediateTraining_Event.asset
  └─ MasterTraining_Event.asset
```

#### Event 1: Basic Training

```
EventData: "Basic Training"
├─ AutoTrigger: true
├─ OneTime: true
├─ RequiredLocation: TrainingHall
│
├─ Conditions: (none - always available)
│
└─ Consequences:
   └─ StatConsequence
      ├─ Character: Protagonist
      ├─ Stat: Strength
      └─ Delta: 15
```

#### Event 2: Intermediate Training

```
EventData: "Intermediate Training"
├─ AutoTrigger: true
├─ OneTime: true
├─ RequiredLocation: TrainingHall
│
├─ Conditions:
│  └─ StatCondition
│     ├─ Character: Protagonist
│     ├─ Stat: Strength
│     ├─ Comparison: GreaterOrEqual
│     └─ Value: 40
│
└─ Consequences:
   └─ StatConsequence
      ├─ Character: Protagonist
      ├─ Stat: Strength
      └─ Delta: 25
```

#### Event 3: Master Training

```
EventData: "Master Training"
├─ AutoTrigger: true
├─ OneTime: true
├─ RequiredLocation: TrainingHall
│
├─ Conditions:
│  └─ StatCondition
│     ├─ Character: Protagonist
│     ├─ Stat: Strength
│     ├─ Comparison: GreaterOrEqual
│     └─ Value: 65
│
└─ Consequences:
   └─ StatConsequence
      ├─ Character: Protagonist
      ├─ Stat: Strength
      └─ Delta: 40
```

#### Progression Flow

```
Initial State: Strength 0

Visit TrainingHall
  → Event 1 (Basic) available
  → Triggers automatically
  → Strength: 0 + 15 = 15

Recheck events
  → Event 2 (Intermediate) requires Strength >= 40
  → Still blocked
  → Stuck at Basic Training

Player: "That was good, but I'm not ready for intermediate."

Later, training outside TrainingHall increases Strength to 40

Visit TrainingHall again
  → Event 1 (Basic) already completed (OneTime)
  → Recheck events with Strength 40
  → Event 2 (Intermediate) eligible!
  → Triggers automatically
  → Strength: 40 + 25 = 65

Recheck events
  → Event 3 (Master) requires Strength >= 65
  → Now eligible!
  → Triggers automatically
  → Strength: 65 + 40 = 105 (clamped to MaxValue if 100)

Result: All three training events completed in sequence
```

**Key Feature:** The recheck mechanism allows Event 2 and 3 to trigger
automatically once their conditions are met, without manual triggers.

---

### Example 3: Conversation with Relationship Impact

**Scenario:** Talking to an NPC changes protagonist's charisma stat, affects future dialogue options.

#### Assets
```
Assets/Stats/
  ├─ Charisma.asset
  └─ Confidence.asset

Assets/Characters/
  ├─ Protagonist.asset
  └─ Mentor.asset

Assets/Events/
  ├─ FirstMeeting_Event.asset
  └─ SecondMeeting_Event.asset
```

#### Event 1: First Meeting (Always Available)

```
EventData: "First Meeting with Mentor"
├─ DisplayName: "First Meeting"
├─ AutoTrigger: false
├─ OneTime: true
├─ RequiredLocation: Castle
│
├─ Conditions: (none)
│
└─ Consequences:
   └─ StatConsequence
      ├─ Character: Protagonist
      ├─ Stat: Charisma
      └─ Delta: +10
        (Mentor's wisdom boosts protagonist's confidence)
```

#### Event 2: Second Meeting (Unlocked by Charisma Increase)

```
EventData: "Advanced Lesson from Mentor"
├─ DisplayName: "Advanced Lesson"
├─ AutoTrigger: true
├─ OneTime: true
├─ RequiredLocation: Castle
│
├─ Conditions:
│  └─ StatCondition
│     ├─ Character: Protagonist
│     ├─ Stat: Charisma
│     ├─ Comparison: GreaterOrEqual
│     └─ Value: 50
│        (Only available if charisma high enough)
│
└─ Consequences:
   └─ StatConsequence
      ├─ Character: Protagonist
      ├─ Stat: Charisma
      └─ Delta: +15
```

#### Dialogue (First Meeting)
```
NODE: Start
  "Welcome. I sense potential in you."

NODE: Choice
  [
    "Tell me your secrets." (requires Charisma >= 50 - UNAVAILABLE YET)
    "Teach me your ways." (always available)
  ]

  IF selected "Teach me":
    "Very well. Listen carefully..."
    → END (Charisma +10)
```

#### Dialogue (Second Meeting - Only Triggers After First)
```
NODE: Start
  "Ah, I see you've grown in confidence."
  "You're ready for the advanced teachings."
  "Today, I'll teach you the secrets of persuasion."
  → END (Charisma +15)
```

#### Execution Timeline

```
Initial: Charisma 40

Visit Castle
  → Event: "First Meeting" available
  → Player triggers: "Teach me your ways"
  → Charisma: 40 + 10 = 50
  → Recheck events
  → Event: "Advanced Lesson" NOW ELIGIBLE (requires >= 50)
  → AutoTrigger = true → fires immediately
  → Charisma: 50 + 15 = 65

Result:
  - Two events play back-to-back due to condition unlock
  - Total charisma gain: +25
  - Second meeting only available after first increases charisma
```

---

### Example 4: Sanity System (Negative Consequences)

**Scenario:** Horror events reduce sanity, potentially unlocking darker dialogue options.

#### Assets
```
Assets/Stats/
  ├─ Sanity.asset (Min: 0, Max: 100, Default: 100)
  └─ MentalHealth.asset (calculated)

Assets/Characters/
  └─ Protagonist.asset

Assets/Events/
  ├─ AncientTemple_Event.asset
  ├─ SecretsRevealed_Event.asset
  └─ MentalBreakdown_Event.asset
```

#### Event 1: Ancient Temple (Horror)

```
EventData: "Explore Ancient Temple"
├─ DisplayName: "Dark Secrets"
├─ AutoTrigger: false
├─ OneTime: true
│
├─ Conditions:
│  └─ StatCondition
│     ├─ Character: Protagonist
│     ├─ Stat: Sanity
│     ├─ Comparison: GreaterOrEqual
│     └─ Value: 0  (always passes, sanity always >= 0)
│
└─ Consequences:
   └─ StatConsequence
      ├─ Character: Protagonist
      ├─ Stat: Sanity
      └─ Delta: -30  (NEGATIVE - reduces sanity!)
```

#### Event 2: Secrets Revealed (Unlocked When Sanity Low)

```
EventData: "Eldritch Secrets"
├─ DisplayName: "The Truth"
├─ AutoTrigger: true
├─ OneTime: true
│
├─ Conditions:
│  └─ StatCondition
│     ├─ Character: Protagonist
│     ├─ Stat: Sanity
│     ├─ Comparison: LessOrEqual  ← Note: LessOrEqual, not GreaterOrEqual!
│     └─ Value: 40
│        (Only triggers when sanity drops low)
│
└─ Consequences:
   └─ StatConsequence
      ├─ Character: Protagonist
      ├─ Stat: Sanity
      └─ Delta: -20  (Even worse!)
```

#### Event 3: Mental Breakdown (Final State)

```
EventData: "Complete Breakdown"
├─ DisplayName: "Insanity"
├─ AutoTrigger: true
├─ OneTime: true
│
├─ Conditions:
│  └─ StatCondition
│     ├─ Character: Protagonist
│     ├─ Stat: Sanity
│     ├─ Comparison: LessOrEqual
│     └─ Value: 20  (Only when sanity is very low)
│
└─ Consequences:
   └─ StatConsequence
      ├─ Character: Protagonist
      ├─ Stat: Sanity
      └─ Delta: -20  (Clamped to min: 0)
```

#### Execution Timeline

```
Initial: Sanity 100

Play "Explore Ancient Temple"
  → Sanity: 100 - 30 = 70
  → Recheck events
  → Event "Secrets Revealed" requires Sanity <= 40
  → Still not met (70 > 40) → Blocked

Player explores more horror events...
  → Sanity drops to 38

Recheck events
  → Event "Secrets Revealed" eligible (38 <= 40)
  → AutoTrigger = true → fires automatically
  → Dialogue: "You understand the horrible truth..."
  → Sanity: 38 - 20 = 18
  → Recheck events
  → Event "Mental Breakdown" eligible (18 <= 20)
  → AutoTrigger = true → fires immediately
  → Dialogue: "Your mind cannot bear the weight of knowledge..."
  → Sanity: 18 - 20 = 0 (clamped, cannot go below 0)

Final: Sanity 0 (broken protagonist)
  → Game may enter special "broken" mode with different mechanics
  → Character locked into "insane" dialogue options
```

**Key Features:**
- LessOrEqual operator for "low stat" conditions
- Negative delta for stat damage
- Clamping prevents sanity from going below 0
- Chain of events with auto-triggering based on stat drops

---

### Example 5: Multi-Character Challenge

**Scenario:** Event requires checking multiple characters' stats before triggering.

#### Setup
```
Assets/Characters/
  ├─ Protagonist.asset
  └─ Companion.asset

Assets/Events/
  └─ BossChallenge_Event.asset
```

#### Multi-Condition Event

```
EventData: "Final Boss Battle"
├─ DisplayName: "The Final Confrontation"
├─ AutoTrigger: false
├─ OneTime: true
│
├─ Conditions: ← Multiple conditions, ALL must pass
│  ├─ Condition 0: StatCondition
│  │  ├─ Character: Protagonist
│  │  ├─ Stat: Strength
│  │  ├─ Comparison: GreaterOrEqual
│  │  └─ Value: 80
│  │
│  └─ Condition 1: StatCondition
│     ├─ Character: Companion
│     ├─ Stat: Strength
│     ├─ Comparison: GreaterOrEqual
│     └─ Value: 60
│        (Companion must also be trained)
│
└─ Consequences:
   ├─ Consequence 0: StatConsequence
   │  ├─ Character: Protagonist
   │  ├─ Stat: Strength
   │  └─ Delta: +20
   │
   └─ Consequence 1: StatConsequence
      ├─ Character: Companion
      ├─ Stat: Strength
      └─ Delta: +20
```

#### Execution Timeline

```
Initial:
  Protagonist Strength: 75
  Companion Strength: 50

Visit Boss Location
  → Check condition 1: Protagonist Strength >= 80? NO (75 < 80)
  → Event BLOCKED

Protagonist trains → Strength 80
Companion trains → Strength 55

Visit Boss Location
  → Check condition 1: Protagonist Strength >= 80? YES
  → Check condition 2: Companion Strength >= 60? NO (55 < 60)
  → Event still BLOCKED (all conditions must pass)

Companion trains more → Strength 60

Visit Boss Location
  → Check condition 1: Protagonist Strength >= 80? YES
  → Check condition 2: Companion Strength >= 60? YES
  → Event ELIGIBLE!
  → Player triggers: "Final Boss Battle"
  → Dialogue plays
  → Consequences apply to BOTH characters
  → Protagonist Strength: 80 + 20 = 100
  → Companion Strength: 60 + 20 = 80

Result: Both characters powered up after victory
```

**Key Feature:** Events can have multiple conditions on different characters,
all must pass (AND logic) for event to be eligible.

---

### Example 6: Branching Dialogue Based on Stats

**Scenario:** Same event, different dialogue paths based on character stats.

```
EventData: "Intimidate the Guard"
├─ DisplayName: "Persuasion"
├─ AutoTrigger: false
│
├─ Conditions: (none - event always available)
│
└─ Consequences:
   └─ StatConsequence
      ├─ Character: Guard
      ├─ Stat: Morale
      └─ Delta: -10  (Guard is intimidated)
```

#### Dialogue Branching
```
NODE: Confront
  "You face down the guard."

NODE: Check Stats
  IF Player.Strength >= 80 AND Player.Charisma >= 60:
    → "YourWay" (use both strength and charisma)
  ELSE IF Player.Strength >= 80:
    → "ThroughForce" (brute force)
  ELSE IF Player.Charisma >= 60:
    → "ThroughWords" (persuade)
  ELSE:
    → "Blocked" (can't pass)

NODE: YourWay
  "You look strong and speak with authority."
  "The guard steps aside without a word."
  → END

NODE: ThroughForce
  "Your imposing physique intimidates the guard."
  "He reluctantly lets you pass."
  → END

NODE: ThroughWords
  "Your silver tongue works magic on the guard."
  "He laughs and waves you through."
  → END

NODE: Blocked
  "The guard is unmoved."
  "You cannot pass without more power or charisma."
  → END (no consequence applied)
```

**Note:** This example shows that dialogue can check stats at ANY point,
not just at event level. The event condition just determines availability.

---

## Common Patterns Summary

| Pattern | Condition | Consequence | Result |
|---------|-----------|-------------|--------|
| **Gate** | Stat >= threshold | None | Event blocked until stat high enough |
| **Unlock** | Stat >= threshold | Stat += bonus | Achievement unlocked, progression |
| **Penalty** | Stat >= 0 (always) | Stat -= damage | Negative consequence for choice |
| **Bonus** | Stat >= threshold | Stat += bonus | Reward for achieving target stat |
| **Chain** | Stat >= threshold | Stat += bonus | One event unblocks next |
| **Multi-Char** | Multiple stats | Multiple changes | Team-based progression |
| **Branching** | (in dialogue) | Conditional | Different outcomes by stat |

---

## Testing These Examples

### In Play Mode
```csharp
// Debug current stats
var protagonist = FindObjectOfType<CharacterInstance>();
var strength = Resources.Load<StatDefinition>("Stats/Strength");
Debug.Log($"Strength: {protagonist.GetStat(strength)}");

// Manually test condition
var condition = new StatCondition { ... };
Debug.Log($"Condition met: {condition.IsMet()}");

// Manually trigger event
EventManager.Instance.TriggerEvent(myEventData);
```

### Expected Logs
```
[EventManager] Location changed to 'Training Hall'. Evaluating events.
[EventManager] Auto-triggering event 'Basic Training' (Priority 0).
[EventManager] Event 'Basic Training' triggered.
[StatManager] Protagonist Strength: 15 + 15 → 30
[EventManager] Event 'Basic Training' completed.
[EventManager] Evaluating events at 'Training Hall' (recheck).
[EventManager] Event 'Intermediate Training' blocked by condition...
```

---

## Next Steps

1. Create your stat assets (Strength, Charisma, etc.)
2. Create character assets (Protagonist, NPCs)
3. Create events with StatCondition/StatConsequence
4. Test in play mode
5. Verify logs show expected stat changes
6. Create dialogue scripts that branch on stats

See `QUICK_STAT_EVENT_REFERENCE.md` for quick setup checklist.
