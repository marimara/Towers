using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Singleton MonoBehaviour that owns the global boolean flag store.
///
/// Responsibilities:
///   - Store named boolean flags in a <see cref="Dictionary{TKey,TValue}"/> keyed by string ID.
///   - Provide a clean read/write/query/reset API consumed by <see cref="FlagCondition"/>
///     and <see cref="FlagConsequence"/>.
///   - Persist across scene loads via DontDestroyOnLoad.
///   - Log every mutation in the Editor so designers can trace flag changes without
///     attaching a debugger.
///
/// Save/load surface (no serialisation format assumed):
///   - <see cref="GetAllFlags"/> — snapshot the store for the save system to write.
///   - <see cref="LoadFlags"/>   — merge a previously saved snapshot back into the store.
///
/// What this class does NOT do:
///   - No UI code.
///   - No serialisation — format and I/O are owned by the save system.
/// </summary>
public class FlagManager : MonoBehaviour
{
    // -------------------------------------------------------------------------
    // Singleton
    // -------------------------------------------------------------------------

    public static FlagManager Instance { get; private set; }

    // -------------------------------------------------------------------------
    // Private state
    // -------------------------------------------------------------------------

    // Null-checked lazy init — dictionary is only allocated on first actual use,
    // avoiding a wasted allocation on the duplicate instance that Awake destroys.
    private Dictionary<string, bool> _flags;
    private Dictionary<string, bool> Flags => _flags ??= new Dictionary<string, bool>();

    // -------------------------------------------------------------------------
    // Unity lifecycle
    // -------------------------------------------------------------------------

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Debug.LogWarning($"[FlagManager] Multiple instances detected. Destroying duplicate on '{name}'.", this);
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
    /// Returns the stored value of the flag with the given <paramref name="id"/>.
    /// If the flag has never been set, returns <paramref name="defaultValue"/> instead
    /// of throwing — absent flags are never errors.
    /// Uses <see cref="Dictionary{TKey,TValue}.TryGetValue"/> internally:
    /// zero allocation, no exception on missing keys.
    /// </summary>
    /// <param name="id">The flag identifier. Case-sensitive.</param>
    /// <param name="defaultValue">
    /// Value returned when the flag is not present in the store.
    /// Defaults to <c>false</c>, matching the "flag not set = off" convention.
    /// Pass <c>true</c> to implement opt-out flags (present = disabled).
    /// </param>
    public bool GetFlag(string id, bool defaultValue = false)
    {
        if (string.IsNullOrEmpty(id))
        {
            Debug.LogWarning("[FlagManager] GetFlag called with null or empty id.");
            return defaultValue;
        }

        return Flags.TryGetValue(id, out bool stored) ? stored : defaultValue;
    }

    /// <summary>
    /// Sets the flag with the given <paramref name="id"/> to <paramref name="value"/>.
    /// Creates the entry if it does not already exist.
    /// Logs the change in the Editor for traceability.
    /// </summary>
    /// <param name="id">The flag identifier. Case-sensitive.</param>
    /// <param name="value">The value to write.</param>
    public void SetFlag(string id, bool value)
    {
        if (string.IsNullOrEmpty(id))
        {
            Debug.LogWarning("[FlagManager] SetFlag called with null or empty id — flag not written.");
            return;
        }

        Flags.TryGetValue(id, out bool previous);
        Flags[id] = value;

#if UNITY_EDITOR
        if (previous != value)
            Debug.Log($"[FlagManager] '{id}': {previous} → {value}");
        else
            Debug.Log($"[FlagManager] '{id}' set to {value} (unchanged)");
#endif
    }

    /// <summary>
    /// Returns <c>true</c> if the flag has ever been explicitly set via
    /// <see cref="SetFlag"/>, regardless of its current value.
    /// Use this to distinguish "flag was set to false" from "flag was never touched".
    /// </summary>
    /// <param name="id">The flag identifier. Case-sensitive.</param>
    public bool HasFlag(string id)
    {
        if (string.IsNullOrEmpty(id))
        {
            Debug.LogWarning("[FlagManager] HasFlag called with null or empty id.");
            return false;
        }

        return Flags.ContainsKey(id);
    }

    /// <summary>
    /// Removes the flag entry entirely, as if it were never set.
    /// Subsequent <see cref="GetFlag"/> calls for this ID will return <c>false</c>;
    /// subsequent <see cref="HasFlag"/> calls will return <c>false</c>.
    /// </summary>
    /// <param name="id">The flag identifier. Case-sensitive.</param>
    public void ClearFlag(string id)
    {
        if (string.IsNullOrEmpty(id))
        {
            Debug.LogWarning("[FlagManager] ClearFlag called with null or empty id.");
            return;
        }

        bool removed = Flags.Remove(id);

#if UNITY_EDITOR
        if (removed)
            Debug.Log($"[FlagManager] '{id}' cleared.");
        else
            Debug.Log($"[FlagManager] ClearFlag('{id}'): flag was not set — nothing to clear.");
#endif
    }

    /// <summary>
    /// Removes all flag entries. Use at the start of a new game or when loading
    /// a save that will re-populate the dictionary via <see cref="SetFlag"/>.
    /// </summary>
    public void ResetAll()
    {
        int count = Flags.Count;
        Flags.Clear();

#if UNITY_EDITOR
        Debug.Log($"[FlagManager] ResetAll — cleared {count} flag(s).");
#endif
    }

    /// <summary>
    /// Returns a snapshot of all current flag entries.
    /// Pass this to your save system to write to disk.
    /// The returned dictionary is a copy — mutating it does not affect the store.
    /// </summary>
    public Dictionary<string, bool> GetAllFlags() => new(Flags);

    /// <summary>
    /// Merges a saved flag snapshot into the current store.
    /// Each entry in <paramref name="data"/> is written with <see cref="SetFlag"/>,
    /// overwriting any conflicting in-memory value (saved state is authoritative).
    /// Existing flags whose keys are absent from <paramref name="data"/> are left
    /// untouched — they represent runtime state set after the last save point.
    /// Null or empty <paramref name="data"/> is accepted silently (no-op).
    /// </summary>
    /// <param name="data">
    /// The flag snapshot produced by a previous <see cref="GetAllFlags"/> call.
    /// Null entries (empty string keys) are skipped with a warning.
    /// </param>
    public void LoadFlags(Dictionary<string, bool> data)
    {
        if (data == null || data.Count == 0)
        {
#if UNITY_EDITOR
            Debug.Log("[FlagManager] LoadFlags called with null or empty data — nothing to merge.");
#endif
            return;
        }

        int merged  = 0;
        int skipped = 0;

        foreach (var entry in data)
        {
            if (string.IsNullOrEmpty(entry.Key))
            {
                Debug.LogWarning("[FlagManager] LoadFlags: skipping entry with null or empty key.");
                skipped++;
                continue;
            }

            SetFlag(entry.Key, entry.Value);
            merged++;
        }

#if UNITY_EDITOR
        Debug.Log($"[FlagManager] LoadFlags complete — merged {merged} flag(s), skipped {skipped}.");
#endif
    }
}
