using UnityEngine;

/// <summary>
/// Singleton MonoBehaviour that provides global access to character stats.
///
/// Responsibilities:
///   - Provide a singleton entry point for reading/writing character stats from anywhere.
///   - Wrap the <see cref="CharacterStats"/> component API for easy access via CharacterInstance.
///   - Log stat mutations in the Editor for debugging and traceability.
///   - Persist across scene loads via DontDestroyOnLoad.
///
/// Design:
///   - Works with CharacterInstance objects (which hold VNCharacter definition + CharacterStats component).
///   - CharacterInstance bridges the gap between the VNCharacter asset definition and runtime stats.
///   - StatManager delegates all stat operations to the target character's CharacterStats component.
///
/// What this class does NOT do:
///   - No UI code.
///   - No save/load — save system owns serialisation.
///   - No game logic beyond stat mutation.
/// </summary>
public class StatManager : MonoBehaviour
{
    // -------------------------------------------------------------------------
    // Singleton
    // -------------------------------------------------------------------------

    public static StatManager Instance { get; private set; }

    // -------------------------------------------------------------------------
    // Unity lifecycle
    // -------------------------------------------------------------------------

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Debug.LogWarning($"[StatManager] Multiple instances detected. Destroying duplicate on '{name}'.", this);
            Destroy(gameObject);
            return;
        }

        Instance = this;
        DontDestroyOnLoad(gameObject);
    }

    private void OnDestroy()
    {
        if (Instance == this)
            Instance = null;
    }

    // -------------------------------------------------------------------------
    // Public API
    // -------------------------------------------------------------------------

    /// <summary>
    /// Get the current value of a stat for a character instance.
    /// Automatically initialises the stat from its definition if never accessed.
    /// Returns 0 if the character instance is null, has no CharacterStats component, or the stat is null.
    /// </summary>
    /// <param name="character">The CharacterInstance to query. Can be null.</param>
    /// <param name="stat">The stat definition to read. Can be null.</param>
    public int GetStat(CharacterInstance character, StatDefinition stat)
    {
        if (character == null)
        {
            Debug.LogWarning("[StatManager] GetStat called with null character instance.");
            return 0;
        }

        if (stat == null)
        {
            Debug.LogWarning("[StatManager] GetStat called with null stat definition.");
            return 0;
        }

        return character.GetStat(stat);
    }

    /// <summary>
    /// Set the current value of a stat for a character instance to an exact amount.
    /// Respects the stat's Min/Max constraints if <see cref="StatDefinition.ClampToRange"/> is enabled.
    /// Does nothing if the character instance is null, has no CharacterStats component, or the stat is null.
    /// </summary>
    /// <param name="character">The CharacterInstance to modify. Can be null.</param>
    /// <param name="stat">The stat definition to write. Can be null.</param>
    /// <param name="value">The new value to set.</param>
    public void SetStat(CharacterInstance character, StatDefinition stat, int value)
    {
        if (character == null)
        {
            Debug.LogWarning("[StatManager] SetStat called with null character instance.");
            return;
        }

        if (stat == null)
        {
            Debug.LogWarning("[StatManager] SetStat called with null stat definition.");
            return;
        }

        int previous = character.GetStat(stat);
        character.SetStat(stat, value);
        int updated = character.GetStat(stat);

        string characterName = !string.IsNullOrEmpty(character.DisplayName) ? character.DisplayName : character.gameObject.name;

#if UNITY_EDITOR
        if (previous != updated)
            Debug.Log($"[StatManager] {characterName} {stat.DisplayName}: {previous} → {updated}");
        else
            Debug.Log($"[StatManager] {characterName} {stat.DisplayName} set to {updated} (unchanged)");
#endif
    }

    /// <summary>
    /// Modify the current value of a stat for a character instance by a signed delta.
    /// Respects the stat's Min/Max constraints if <see cref="StatDefinition.ClampToRange"/> is enabled.
    /// Does nothing if the character instance is null, has no CharacterStats component, or the stat is null.
    /// </summary>
    /// <param name="character">The CharacterInstance to modify. Can be null.</param>
    /// <param name="stat">The stat definition to modify. Can be null.</param>
    /// <param name="delta">Amount to add (positive) or subtract (negative).</param>
    public void ModifyStat(CharacterInstance character, StatDefinition stat, int delta)
    {
        if (character == null)
        {
            Debug.LogWarning("[StatManager] ModifyStat called with null character instance.");
            return;
        }

        if (stat == null)
        {
            Debug.LogWarning("[StatManager] ModifyStat called with null stat definition.");
            return;
        }

        int previous = character.GetStat(stat);
        character.ModifyStat(stat, delta);
        int updated = character.GetStat(stat);

        string characterName = !string.IsNullOrEmpty(character.DisplayName) ? character.DisplayName : character.gameObject.name;

#if UNITY_EDITOR
        if (delta != 0)
            Debug.Log($"[StatManager] {characterName} {stat.DisplayName}: {previous} + {delta:+#;-#;0} → {updated}");
#endif
    }

}
