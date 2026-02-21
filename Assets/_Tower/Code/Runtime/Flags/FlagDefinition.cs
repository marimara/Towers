using UnityEngine;

/// <summary>
/// ScriptableObject documenting a single boolean flag in the game.
///
/// Responsibilities:
///   - Carry the identity and description of one flag (ID, name, purpose).
///
/// ID is auto-generated and managed by the base UniqueIdScriptableObject class.
/// No runtime logic — this is a reference asset for designers to document
/// and organise flags without touching code.
/// </summary>
[CreateAssetMenu(menuName = "Game/Flag Definition")]
public class FlagDefinition : UniqueIdScriptableObject
{
    // -------------------------------------------------------------------------
    // Display
    // -------------------------------------------------------------------------

    [Tooltip("Human-readable name of this flag (e.g., \"Met Elara\", \"Quest Started\").")]
    public string DisplayName;

    // -------------------------------------------------------------------------
    // Documentation
    // -------------------------------------------------------------------------

    [Tooltip("Purpose and usage of this flag. When is it set? What does it control?")]
    [TextArea(3, 6)]
    public string Description;
}
