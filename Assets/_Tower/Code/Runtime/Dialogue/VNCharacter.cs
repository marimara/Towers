using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// ScriptableObject representing a visual novel character.
/// Stores display data for presentation in dialogue scenes and starting stat configuration.
///
/// ID is auto-generated and managed by the base UniqueIdScriptableObject class.
/// </summary>
[CreateAssetMenu(menuName = "VN/Character")]
public class VNCharacter : UniqueIdScriptableObject
{
    // -------------------------------------------------------------------------
    // Display Data
    // -------------------------------------------------------------------------

    [Tooltip("Name shown in the dialogue UI name box.")]
    public string DisplayName;

    [Tooltip("Portrait sprite for this character.")]
    public Sprite Portrait;

    [Tooltip("Color used for the character's name text.")]
    public Color NameColor = Color.white;

    [Tooltip("Character race (e.g., Human, Elf, Dwarf).")]
    public Race Race;

    // -------------------------------------------------------------------------
    // Starting Stats
    // -------------------------------------------------------------------------

    [Tooltip("Per-character starting stat configuration.\n" +
             "Each entry defines the initial value for a specific stat.\n" +
             "Leave empty to use StatDefinition defaults.")]
    public List<StartingStatEntry> StartingStats = new List<StartingStatEntry>();
}
