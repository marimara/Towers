using UnityEngine;

/// <summary>
/// ScriptableObject documenting a single character stat in the game.
///
/// Responsibilities:
///   - Carry the identity, description, and constraints of one stat
///     (ID, name, purpose, default/min/max values, clamping behaviour).
///
/// ID is auto-generated and managed by the base UniqueIdScriptableObject class.
/// No runtime logic — this is a reference asset for designers to document
/// and configure stats without touching code.
/// </summary>
[CreateAssetMenu(menuName = "Game/Stat Definition")]
public class StatDefinition : UniqueIdScriptableObject
{
    // -------------------------------------------------------------------------
    // Display
    // -------------------------------------------------------------------------

    [Tooltip("Human-readable name of this stat (e.g., \"Strength\", \"Magic\", \"Sanity\").")]
    public string DisplayName;

    // -------------------------------------------------------------------------
    // Documentation
    // -------------------------------------------------------------------------

    [Tooltip("Purpose and usage of this stat. What does it affect? How is it used?")]
    [TextArea(3, 6)]
    public string Description;

    // -------------------------------------------------------------------------
    // Value constraints
    // -------------------------------------------------------------------------

    [Tooltip("Default value for this stat when a character is created.")]
    public int DefaultValue = 50;

    [Tooltip("Minimum allowed value for this stat.")]
    public int MinValue = 0;

    [Tooltip("Maximum allowed value for this stat.")]
    public int MaxValue = 100;

    [Tooltip("When true, stat values are automatically clamped to [MinValue, MaxValue] range. " +
             "When false, values can exceed the range (useful for temporary modifiers).")]
    public bool ClampToRange = true;
}
