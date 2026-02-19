using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Runtime service managing character relationships.
/// Singleton pattern ensures single instance across the game.
/// Initializes relationships on Awake and provides public API for querying/modifying.
/// </summary>
public class RelationshipSystem : MonoBehaviour
{
    // -------------------------------------------------------------------------
    // Singleton
    // -------------------------------------------------------------------------

    public static RelationshipSystem Instance { get; private set; }

    // -------------------------------------------------------------------------
    // Inspector
    // -------------------------------------------------------------------------

    [Header("Configuration")]
    [SerializeField] private RaceRelationshipMatrix _raceMatrix;
    [SerializeField] private RelationshipTierConfig _tierConfig;

    [Header("Important Characters")]
    [Tooltip("Characters to initialize relationships for at game start.")]
    [SerializeField] private List<VNCharacter> _importantCharacters = new();

    // -------------------------------------------------------------------------
    // Private state
    // -------------------------------------------------------------------------

    private RelationshipManager _manager;

    // -------------------------------------------------------------------------
    // Unity lifecycle
    // -------------------------------------------------------------------------

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Debug.LogWarning($"[RelationshipSystem] Multiple instances detected. Destroying duplicate on '{name}'.", this);
            Destroy(gameObject);
            return;
        }

        Instance = this;
        DontDestroyOnLoad(gameObject);

        ValidateConfiguration();
        InitializeManager();
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
    /// Get the relationship value from one character to another.
    /// Returns base value if not initialized.
    /// </summary>
    public int GetRelationship(VNCharacter from, VNCharacter to)
    {
        if (_manager == null)
        {
            Debug.LogWarning("[RelationshipSystem] Manager not initialized.");
            return 50;
        }

        return _manager.Get(from, to);
    }

    /// <summary>
    /// Modify the relationship value between two characters by a delta.
    /// Automatically clamped between 1 and 100.
    /// </summary>
    public void ModifyRelationship(VNCharacter from, VNCharacter to, int delta)
    {
        if (_manager == null)
        {
            Debug.LogWarning("[RelationshipSystem] Manager not initialized.");
            return;
        }

        _manager.Modify(from, to, delta);
    }

    /// <summary>
    /// Get the tier name for the relationship between two characters.
    /// Returns "Unknown" if tier config is not assigned or no tier matches.
    /// </summary>
    public string GetRelationshipTier(VNCharacter from, VNCharacter to)
    {
        if (_tierConfig == null)
        {
            Debug.LogWarning("[RelationshipSystem] TierConfig not assigned.");
            return "Unknown";
        }

        int value = GetRelationship(from, to);
        return _tierConfig.GetTierName(value);
    }

    /// <summary>
    /// Reinitialize the relationship system with a new set of characters.
    /// Useful for dynamic character loading or scene transitions.
    /// </summary>
    public void Reinitialize(List<VNCharacter> characters)
    {
        if (_manager == null)
            _manager = new RelationshipManager();

        if (characters == null || characters.Count == 0)
        {
            Debug.LogWarning("[RelationshipSystem] Reinitialize called with null or empty character list.");
            return;
        }

        _manager.Initialize(characters, _raceMatrix);
        Debug.Log($"[RelationshipSystem] Reinitialized with {characters.Count} characters.");
    }

    // -------------------------------------------------------------------------
    // Initialization & Validation
    // -------------------------------------------------------------------------

    private void InitializeManager()
    {
        _manager = new RelationshipManager();

        if (_importantCharacters == null || _importantCharacters.Count == 0)
        {
            Debug.LogWarning("[RelationshipSystem] No important characters assigned. Relationships will not be initialized.", this);
            return;
        }

        _manager.Initialize(_importantCharacters, _raceMatrix);
        Debug.Log($"[RelationshipSystem] Initialized with {_importantCharacters.Count} characters.", this);
    }

    private void ValidateConfiguration()
    {
        bool hasIssues = false;

        if (_raceMatrix == null)
        {
            Debug.LogWarning("[RelationshipSystem] RaceRelationshipMatrix is not assigned. Race modifiers will not be applied.", this);
            hasIssues = true;
        }

        if (_tierConfig == null)
        {
            Debug.LogWarning("[RelationshipSystem] RelationshipTierConfig is not assigned. GetRelationshipTier() will return 'Unknown'.", this);
            hasIssues = true;
        }

        if (_importantCharacters == null || _importantCharacters.Count == 0)
        {
            Debug.LogWarning("[RelationshipSystem] ImportantCharacters list is empty. No relationships will be initialized.", this);
            hasIssues = true;
        }
        else
        {
            // Check for null entries
            int nullCount = 0;
            foreach (var character in _importantCharacters)
            {
                if (character == null)
                    nullCount++;
            }

            if (nullCount > 0)
            {
                Debug.LogWarning($"[RelationshipSystem] ImportantCharacters contains {nullCount} null entries. These will be skipped.", this);
                hasIssues = true;
            }
        }

        if (!hasIssues)
        {
            Debug.Log("[RelationshipSystem] Configuration validated successfully.", this);
        }
    }
}
