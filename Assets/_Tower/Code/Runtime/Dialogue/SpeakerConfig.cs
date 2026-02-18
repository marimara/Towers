using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// ScriptableObject that maps each <see cref="Speaker"/> enum value to display data.
/// Assign in the DialogueRunner inspector. Decouples presentation from the enum name.
/// </summary>
[CreateAssetMenu(menuName = "VN/Speaker Config")]
public class SpeakerConfig : ScriptableObject
{
    [System.Serializable]
    public class Entry
    {
        public Speaker Speaker;

        [Tooltip("Name shown in the dialogue UI name box.")]
        public string DisplayName;

        [Tooltip("Optional portrait sprite for this speaker.")]
        public Sprite Portrait;

        [Tooltip("Colour applied to the name text.")]
        public Color NameColor = Color.white;
    }

    public List<Entry> Speakers = new();

    private Dictionary<Speaker, Entry> _map;

    // -------------------------------------------------------------------------
    // Lookup
    // -------------------------------------------------------------------------

    /// <summary>Returns the entry for the given speaker, or null if not configured.</summary>
    public Entry Get(Speaker speaker)
    {
        EnsureMap();
        return _map.TryGetValue(speaker, out var entry) ? entry : null;
    }

    /// <summary>Display name for the speaker, falling back to enum.ToString().</summary>
    public string GetDisplayName(Speaker speaker)
    {
        return Get(speaker)?.DisplayName ?? speaker.ToString();
    }

    // -------------------------------------------------------------------------
    // Internal
    // -------------------------------------------------------------------------

    private void EnsureMap()
    {
        if (_map != null) return;
        _map = new Dictionary<Speaker, Entry>(Speakers.Count);
        foreach (var e in Speakers)
            _map[e.Speaker] = e;
    }

    private void OnValidate() => _map = null; // Rebuild on inspector change
}
