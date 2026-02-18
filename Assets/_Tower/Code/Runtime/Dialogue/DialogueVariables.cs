using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Runtime key-value store for dialogue session variables.
/// Used by conditions and commands to read/write game state during a conversation.
///
/// Values survive node transitions but are discarded when a new StartDialogue() is called
/// (or whenever the owning DialogueRunner is destroyed), unless persisted externally.
/// </summary>
public class DialogueVariables
{
    private readonly Dictionary<string, object> _vars = new();

    // -------------------------------------------------------------------------
    // Write
    // -------------------------------------------------------------------------

    public void Set(string key, object value)   => _vars[key] = value;
    public void SetBool(string key, bool value)   => _vars[key] = value;
    public void SetInt(string key, int value)     => _vars[key] = value;
    public void SetFloat(string key, float value) => _vars[key] = value;
    public void SetString(string key, string value) => _vars[key] = value;

    public void Remove(string key) => _vars.Remove(key);
    public void Clear()            => _vars.Clear();

    // -------------------------------------------------------------------------
    // Read
    // -------------------------------------------------------------------------

    public bool Has(string key) => _vars.ContainsKey(key);

    public T Get<T>(string key, T defaultValue = default)
    {
        if (_vars.TryGetValue(key, out var raw) && raw is T typed)
            return typed;
        return defaultValue;
    }

    public bool   GetBool(string key, bool defaultValue = false)     => Get(key, defaultValue);
    public int    GetInt(string key, int defaultValue = 0)           => Get(key, defaultValue);
    public float  GetFloat(string key, float defaultValue = 0f)      => Get(key, defaultValue);
    public string GetString(string key, string defaultValue = "")    => Get(key, defaultValue);

    // -------------------------------------------------------------------------
    // Debug
    // -------------------------------------------------------------------------

    public void DebugDump()
    {
        foreach (var kv in _vars)
            Debug.Log($"[DialogueVariables] {kv.Key} = {kv.Value}");
    }
}
