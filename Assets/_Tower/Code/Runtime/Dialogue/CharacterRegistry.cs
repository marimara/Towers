using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Singleton MonoBehaviour that maintains a registry of all active CharacterInstance objects.
///
/// Responsibilities:
///   - Automatically register CharacterInstance when it enables
///   - Automatically unregister CharacterInstance when it disables
///   - Maintain a mapping from VNCharacter definition to runtime instance
///   - Provide queries to find character instances by definition
///   - Persist across scene loads via DontDestroyOnLoad
///
/// What this class does NOT do:
///   - No UI rendering
///   - No save/load logic
///   - No character lifecycle management (creation/destruction handled elsewhere)
///   - No gameplay logic
/// </summary>
public class CharacterRegistry : MonoBehaviour
{
    // -------------------------------------------------------------------------
    // Singleton
    // -------------------------------------------------------------------------

    public static CharacterRegistry Instance { get; private set; }

    // -------------------------------------------------------------------------
    // State
    // -------------------------------------------------------------------------

    /// <summary>
    /// Maps each VNCharacter definition to its runtime instance.
    /// A character can only have one active instance at a time.
    /// </summary>
    private Dictionary<VNCharacter, CharacterInstance> _registry = new();

    // -------------------------------------------------------------------------
    // Lifecycle
    // -------------------------------------------------------------------------

    private void Awake()
    {
        // Singleton instantiation with duplicate detection
        if (Instance != null && Instance != this)
        {
            Debug.LogWarning($"[CharacterRegistry] Multiple instances detected. Destroying duplicate on '{name}'.", this);
            Destroy(gameObject);
            return;
        }

        Instance = this;
        DontDestroyOnLoad(gameObject);

#if UNITY_EDITOR
        Debug.Log("[CharacterRegistry] Initialized");
#endif
    }

    private void OnDestroy()
    {
        if (Instance == this)
            Instance = null;
    }

#if UNITY_EDITOR
    /// <summary>
    /// Editor-only: Clean up stale references on domain reload.
    /// This prevents memory leaks when entering play mode with invalid references.
    /// </summary>
    private void OnEnable()
    {
        if (Instance != this)
            return;

        // Validate and remove any stale references
        var keysToRemove = new List<VNCharacter>();
        foreach (var kvp in _registry)
        {
            if (kvp.Value == null || kvp.Value.gameObject == null)
            {
                keysToRemove.Add(kvp.Key);
            }
        }

        if (keysToRemove.Count > 0)
        {
            foreach (var key in keysToRemove)
            {
                _registry.Remove(key);
                Debug.LogWarning($"[CharacterRegistry] Cleaned up stale reference for '{key.DisplayName}'");
            }

            Debug.Log($"[CharacterRegistry] Cleanup complete — removed {keysToRemove.Count} stale reference(s)");
        }
    }
#endif

    // -------------------------------------------------------------------------
    // Registration API
    // -------------------------------------------------------------------------

    /// <summary>
    /// Register a character instance in the registry.
    /// Called automatically by CharacterInstance.OnEnable().
    /// Warns if the character definition is already registered.
    /// Prevents memory leaks by unregistering stale references.
    /// </summary>
    /// <param name="character">The character instance to register</param>
    public void Register(CharacterInstance character)
    {
        if (character == null)
        {
            Debug.LogError("[CharacterRegistry] Cannot register null character instance.");
            return;
        }

        VNCharacter definition = character.Definition;

        // Ignore null definitions
        if (definition == null)
        {
            Debug.LogWarning($"[CharacterRegistry] Cannot register character instance with null definition.", character);
            return;
        }

        // Check for duplicate registration and prevent memory leaks
        if (_registry.TryGetValue(definition, out var existingInstance))
        {
            // Verify the existing instance is still valid
            if (existingInstance != null && existingInstance.gameObject != null)
            {
                Debug.LogWarning($"[CharacterRegistry] Character '{definition.DisplayName}' is already registered. " +
                               "Overwriting previous instance — this may indicate a design issue.",
                               character);
            }
            else
            {
                // Stale reference detected — clean it up
                Debug.LogWarning($"[CharacterRegistry] Stale reference cleaned up for '{definition.DisplayName}'.");
            }
        }

        _registry[definition] = character;

#if UNITY_EDITOR
        Debug.Log($"[CharacterRegistry] Registered '{definition.DisplayName}' ({_registry.Count} total)");
#endif
    }

    /// <summary>
    /// Unregister a character instance from the registry.
    /// Called automatically by CharacterInstance.OnDisable().
    /// </summary>
    /// <param name="character">The character instance to unregister</param>
    public void Unregister(CharacterInstance character)
    {
        if (character == null)
            return;

        VNCharacter definition = character.Definition;

        // Ignore null definitions
        if (definition == null)
            return;

        if (_registry.ContainsKey(definition))
        {
            _registry.Remove(definition);

#if UNITY_EDITOR
            Debug.Log($"[CharacterRegistry] Unregistered '{definition.DisplayName}'");
#endif
        }
    }

    // -------------------------------------------------------------------------
    // Query API
    // -------------------------------------------------------------------------

    /// <summary>
    /// Get the runtime instance for a character definition.
    /// Returns null if the character is not currently active.
    /// Automatically cleans up stale references if detected.
    /// </summary>
    /// <param name="character">The character definition to look up</param>
    /// <returns>The active CharacterInstance, or null if not found</returns>
    public CharacterInstance GetInstance(VNCharacter character)
    {
        if (character == null)
            return null;

        if (!_registry.TryGetValue(character, out var instance))
            return null;

        // Defensive check: clean up stale reference
        if (instance == null || instance.gameObject == null)
        {
            _registry.Remove(character);
            Debug.LogWarning($"[CharacterRegistry] Stale reference removed for '{character.DisplayName}'");
            return null;
        }

        return instance;
    }

    /// <summary>
    /// Check if a character definition has an active instance.
    /// </summary>
    /// <param name="character">The character definition to check</param>
    /// <returns>True if the character is currently active, false otherwise</returns>
    public bool HasInstance(VNCharacter character)
    {
        return character != null && _registry.ContainsKey(character);
    }

    /// <summary>
    /// Get a read-only view of all registered characters.
    /// Use this for iteration or inspection; do not modify directly.
    /// </summary>
    public IReadOnlyDictionary<VNCharacter, CharacterInstance> All => _registry;
}
