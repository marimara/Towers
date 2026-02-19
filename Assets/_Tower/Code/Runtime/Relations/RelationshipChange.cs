using Sirenix.OdinInspector;
using UnityEngine;

/// <summary>
/// Defines a relationship change between two characters.
/// </summary>
[System.Serializable]
public class RelationshipChange
{
    [Tooltip("If true, automatically uses current node speaker and other active character.")]
    public bool AutoBetweenCurrentSpeakers;

    [ShowIf(nameof(ShowManualFields))]
    public VNCharacter From;

    [ShowIf(nameof(ShowManualFields))]
    public VNCharacter To;

    public int Delta;

    [Tooltip("If true, applies the delta in both directions (From→To and To→From).")]
    public bool Mutual;

    private bool ShowManualFields() => !AutoBetweenCurrentSpeakers;
}
