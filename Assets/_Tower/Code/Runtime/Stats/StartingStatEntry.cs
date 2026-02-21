using UnityEngine;

/// <summary>
/// Serializable data structure that pairs a stat definition with its starting value.
/// Used by VNCharacter and other character initialization systems to define
/// per-character stat overrides.
///
/// This structure is intentionally simple — just a container for design-time configuration.
/// No runtime logic or validation; systems consuming this data are responsible for
/// applying bounds checking and clamping as needed.
/// </summary>
[System.Serializable]
public struct StartingStatEntry
{
    [Tooltip("Reference to the stat definition this entry applies to.")]
    public StatDefinition Stat;

    [Tooltip("Starting value for this stat.\n" +
             "Systems will apply the stat definition's min/max constraints as appropriate.")]
    public int Value;

    /// <summary>
    /// Convenience constructor for creating starting stat entries.
    /// </summary>
    /// <param name="stat">The stat definition reference</param>
    /// <param name="value">The starting value</param>
    public StartingStatEntry(StatDefinition stat, int value)
    {
        Stat = stat;
        Value = value;
    }
}
