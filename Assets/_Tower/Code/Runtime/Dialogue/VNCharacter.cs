using UnityEngine;

/// <summary>
/// ScriptableObject representing a visual novel character.
/// Stores display data for presentation in dialogue scenes.
/// </summary>
[CreateAssetMenu(menuName = "VN/Character")]
public class VNCharacter : ScriptableObject
{
    [Tooltip("Name shown in the dialogue UI name box.")]
    public string DisplayName;

    [Tooltip("Portrait sprite for this character.")]
    public Sprite Portrait;

    [Tooltip("Color used for the character's name text.")]
    public Color NameColor = Color.white;
}
