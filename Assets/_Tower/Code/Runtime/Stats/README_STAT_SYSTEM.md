# Character Stats System Architecture

## Overview

The character stats system provides a complete data-driven framework for managing character attributes in a visual novel RPG. It bridges the gap between reusable character **definitions** (ScriptableObject assets) and runtime **character instances** (GameObject components).

## System Components

### 1. Core Assets & Definitions

#### **StatDefinition** (`StatDefinition.cs`)
A ScriptableObject that documents a single character stat.

**Responsibilities:**
- Store metadata: ID, DisplayName, Description
- Define value constraints: DefaultValue, MinValue, MaxValue, ClampToRange
- Auto-generate stable GUIDs on creation (via `OnValidate()`)

**Creation:** Create via Editor menu: `Assets > Create > Game > Stat Definition`

**Example Usage:**
```csharp
// Create a Strength stat
var strength = ScriptableObject.CreateInstance<StatDefinition>();
strength.DisplayName = "Strength";
strength.DefaultValue = 50;
strength.MinValue = 0;
strength.MaxValue = 100;
strength.ClampToRange = true;
```

#### **StatDatabase** (`StatDatabase.cs`)
A central registry of all stat definitions in the game.

**Responsibilities:**
- Maintain a list of all StatDefinition assets
- Build and cache O(1) lookup dictionary by stat ID
- Validate database integrity (no duplicates, no nulls)
- Invalidate cache when definitions change in Editor

**Creation:** Create via Editor menu: `Assets > Create > Game > Stat Database`

**API:**
```csharp
StatDefinition GetById(string id);                    // Returns null if not found
bool TryGetById(string id, out StatDefinition stat);  // Try pattern
bool Contains(StatDefinition stat);                   // Membership check
IReadOnlyList<StatDefinition> AllStats { get; }       // Snapshot view
void InvalidateLookup();                              // Clear cache
```

### 2. Runtime Components

#### **CharacterStats** (`CharacterStats.cs`)
A MonoBehaviour attached to character GameObjects that tracks runtime stat values.

**Responsibilities:**
- Store current stat values in a dictionary (lazily populated)
- Provide safe read/write/modify API respecting Min/Max constraints
- Auto-initialize stats from definitions on first access
- Support save/load via `GetAllStats()` and `LoadStats()`

**Lifecycle:**
- Automatically added by CharacterInstance if missing
- Does NOT serialize stat values (save system owns that)

**API:**
```csharp
int GetStat(StatDefinition stat);                          // Read with auto-init
void SetStat(StatDefinition stat, int value);              // Set with clamping
void ModifyStat(StatDefinition stat, int delta);           // Modify with clamping
Dictionary<StatDefinition, int> GetAllStats();             // Snapshot for saving
void LoadStats(Dictionary<StatDefinition, int> data);      // Restore from save
void ResetAll();                                           // Clear all stats
```

**Example:**
```csharp
var stats = character.GetComponent<CharacterStats>();
int strength = stats.GetStat(strengthDefinition);           // Lazy-init on first access
stats.ModifyStat(strengthDefinition, +5);                  // Modify respecting clamping
```

#### **CharacterInstance** (`CharacterInstance.cs`)
A MonoBehaviour that bridges VNCharacter definitions with runtime stats.

**Responsibilities:**
- Hold reference to VNCharacter definition asset
- Provide unified interface for accessing character identity and stats
- Ensure CharacterStats component exists on the GameObject
- Delegate stat operations to CharacterStats

**Design Pattern:**
```
VNCharacter (Asset)           CharacterInstance (Scene)      CharacterStats (Component)
├─ DisplayName          →      ├─ Definition reference  →    ├─ _stats dictionary
├─ Portrait                    ├─ GetStat()            →    └─ SetStat/ModifyStat
├─ NameColor                   ├─ SetStat()
└─ Race                        └─ ModifyStat()
```

**API:**
```csharp
// Identity (delegated from VNCharacter definition)
VNCharacter Definition { get; }
string DisplayName { get; }
Sprite Portrait { get; }
Color NameColor { get; }
Race Race { get; }

// Stats (delegated to CharacterStats)
int GetStat(StatDefinition stat);
void SetStat(StatDefinition stat, int value);
void ModifyStat(StatDefinition stat, int delta);
Dictionary<StatDefinition, int> GetAllStats();
void LoadStats(Dictionary<StatDefinition, int> data);
void ResetAllStats();
```

**Setup:**
1. Create a GameObject in your scene
2. Add a VNCharacter reference via Inspector
3. Add CharacterInstance component — it auto-creates CharacterStats
4. Use `StatManager` to access stats globally

### 3. Global Access

#### **StatManager** (`StatManager.cs`)
A singleton MonoBehaviour providing global access to character stats throughout the game.

**Responsibilities:**
- Provide singleton entry point for reading/writing character stats
- Wrap CharacterStats API for easy access via CharacterInstance
- Log stat mutations in Editor for debugging
- Persist across scene loads via DontDestroyOnLoad

**Design:**
- Uses standard singleton pattern: `public static StatManager Instance`
- Duplicate detection: destroys duplicate instances on Awake
- Delegation: all operations delegate to target character's CharacterStats

**API:**
```csharp
// Global stat queries
int GetStat(CharacterInstance character, StatDefinition stat);
void SetStat(CharacterInstance character, StatDefinition stat, int value);
void ModifyStat(CharacterInstance character, StatDefinition stat, int delta);
```

**Usage:**
```csharp
// Anywhere in your code
CharacterInstance player = /* reference to player character */;
StatDefinition strength = /* reference to strength stat */;

int currentStrength = StatManager.Instance.GetStat(player, strength);
StatManager.Instance.ModifyStat(player, strength, +5);
```

**Setup:**
1. Create a GameObject called "StatManager"
2. Add StatManager component
3. Mark as DontDestroyOnLoad (automatic in Awake)
4. Ensure it exists in your first scene or bootstrap scene

## Integration with Event System

### StatCondition

Evaluates whether a player stat meets a numeric threshold.

**Status:** Placeholder implementation (requires player character reference)
**Fields:**
- `StatDefinition Stat` — which stat to check
- `ComparisonOperator Comparison` — how to compare (==, >=, <=)
- `int Value` — threshold to compare against

**TODO:**
Implement player character reference. Options:
1. Add `PlayerCharacter` singleton
2. Add `CharacterInstance` field to `EventData`
3. Use scene finder to locate player GameObject

### StatConsequence

Modifies a player stat by a signed delta after an event completes.

**Status:** Placeholder implementation (requires player character reference)
**Fields:**
- `StatDefinition Stat` — which stat to modify
- `int Delta` — amount to add/subtract

**Integration with EventManager:**
After an event's dialogue ends, EventManager applies all consequences including stat modifications. This allows conditions checked in the recheck phase to see updated stat values.

**Example Flow:**
```
Event triggers
  ↓
Dialogue plays
  ↓
Event completes
  ↓
StatConsequence modifies Strength +5
  ↓
EventManager rechecks events at current location
  ↓
StatCondition "Strength >= 100" now evaluates with new Strength value
  ↓
Next eligible event may trigger
```

## Save/Load Integration

### Saving
```csharp
// Get player stats for saving
Dictionary<StatDefinition, int> statsSnapshot = player.GetAllStats();
// Pass statsSnapshot to your save system
save.PlayerStats = statsSnapshot;
```

### Loading
```csharp
// Restore player stats from loaded save
var player = /* find or instantiate player */;
player.LoadStats(save.PlayerStats);
// All stat values are now restored
```

## Data Flow Diagram

```
┌──────────────────┐
│  StatDefinition  │ (Asset) — defines stat metadata
└────────┬─────────┘
         │
         ↓
┌──────────────────┐
│  StatDatabase    │ (Asset) — O(1) lookup by ID
└────────┬─────────┘
         │
         ├─→ StatCondition (Event eligibility check)
         │
         ├─→ StatConsequence (Modify stats after event)
         │
         └─→ StatManager (Global access point)
                  │
                  ↓
         ┌─────────────────┐
         │ CharacterInstance│ (Runtime GameObject)
         └────────┬────────┘
                  │
                  ↓
         ┌────────────────┐
         │ CharacterStats  │ (Component on GameObject)
         └─────────────────┘
              (Stores actual runtime stat values)
```

## Best Practices

### 1. Creating Stats

✅ **DO:**
- Create one StatDefinition per stat in your game
- Use consistent naming conventions (e.g., "Strength", "Charisma")
- Set sensible defaults and constraints
- Create a StatDatabase and add all definitions to it

❌ **DON'T:**
- Create duplicate stat definitions with the same meaning
- Leave stat definitions with empty DisplayNames
- Manually edit auto-generated IDs

### 2. Accessing Stats

✅ **DO:**
- Use `StatManager.Instance.GetStat(character, stat)` for read access
- Use `CharacterInstance` to access both identity and stats
- Null-check before accessing stats
- Use `ModifyStat()` for incremental changes to respect clamping

❌ **DON'T:**
- Access `CharacterStats` directly from other systems (go through StatManager)
- Store raw stat values in other places (source of truth is CharacterStats)
- Bypass clamping by calling SetStat with out-of-range values

### 3. Saving/Loading

✅ **DO:**
- Call `character.GetAllStats()` when saving
- Call `character.LoadStats(snapshot)` when loading
- Save the stat snapshot separately from character references
- Handle the case where a character is not found on load

❌ **DON'T:**
- Serialize individual stat values in separate assets
- Assume stats persist between scene loads (use DontDestroyOnLoad or reload)
- Load stats without clearing previous values first

## Complete Example: Setup in a Scene

```csharp
// 1. Create a Bootstrap scene with StatManager
void SetupGame()
{
    // StatManager setup (only once at game start)
    var statManagerGO = new GameObject("StatManager");
    var statManager = statManagerGO.AddComponent<StatManager>();
    // StatManager Awake() automatically handles DontDestroyOnLoad
}

// 2. Create player character in scene or via prefab
void CreatePlayer()
{
    var playerGO = new GameObject("Player");
    var playerInstance = playerGO.AddComponent<CharacterInstance>();

    // Assign VNCharacter definition
    playerInstance._definition = Resources.Load<VNCharacter>("Characters/Protagonist");

    // CharacterInstance.OnEnable() auto-creates CharacterStats
}

// 3. Use stats throughout the game
void DamagePlayer(int amount)
{
    var player = FindObjectOfType<CharacterInstance>();
    var healthStat = Resources.Load<StatDefinition>("Stats/Health");

    StatManager.Instance.ModifyStat(player, healthStat, -amount);
}

// 4. Save/Load stats
void SaveGame()
{
    var player = FindObjectOfType<CharacterInstance>();
    var statsToSave = player.GetAllStats();
    // Serialize statsToSave to disk
}

void LoadGame()
{
    var player = FindObjectOfType<CharacterInstance>();
    var statsFromDisk = /* deserialize stats */;
    player.LoadStats(statsFromDisk);
}
```

## Testing Checklist

- [ ] Created StatDatabase with all stats assigned
- [ ] Each StatDefinition has valid DisplayName and ID
- [ ] StatManager singleton exists and persists across scenes
- [ ] CharacterInstance component auto-creates CharacterStats
- [ ] `StatManager.GetStat()` returns correct default values on first access
- [ ] `StatManager.ModifyStat()` respects min/max clamping
- [ ] Stats persist when CharacterInstance DontDestroyOnLoad is enabled
- [ ] Save/Load round-trip preserves stat values
- [ ] Editor logs show stat mutations in #if UNITY_EDITOR sections

## Common Issues & Solutions

**Issue:** "CharacterStats not found on GameObject"
- **Solution:** Ensure CharacterInstance is on the same GameObject or call `AddComponent<CharacterStats>()` explicitly

**Issue:** Stat values are always 0
- **Solution:** Check that StatDefinition has a non-zero DefaultValue, or call `SetStat()` explicitly

**Issue:** Stats don't persist between scenes
- **Solution:** Use `DontDestroyOnLoad()` on both StatManager and player character, or reload from save

**Issue:** Stat mutations not logged in Editor
- **Solution:** Check that `UNITY_EDITOR` conditional compilation symbols are correct

## Future Enhancements

1. **Player Character Reference in Events:**
   - Add CharacterInstance field to EventData to support stat conditions on any character
   - Complete StatCondition and StatConsequence implementations

2. **Stat Modifiers:**
   - Add temporary stat modifiers (buffs/debuffs) on top of base values
   - Track modifier sources and durations

3. **Stat Relationships:**
   - Implement stat synergies (e.g., Strength affects physical damage)
   - Add derived stats computed from base stats

4. **UI Integration:**
   - Create stat display panels showing current/max values
   - Add stat change notifications/animations

5. **Achievements:**
   - Track stat milestones for achievement unlock conditions
   - Log stat history for analytics
