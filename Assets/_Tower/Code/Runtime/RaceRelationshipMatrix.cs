using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// ScriptableObject containing unilateral relationship modifiers between races.
/// </summary>
[CreateAssetMenu(menuName = "Game/Race Relationship Matrix")]
public class RaceRelationshipMatrix : ScriptableObject
{
    [System.Serializable]
    public class RaceRelationEntry
    {
        [Tooltip("The race initiating the relationship.")]
        public Race From;

        [Tooltip("The race receiving the relationship.")]
        public Race To;

        [Tooltip("Initial relationship modifier value.")]
        public int InitialModifier;
    }

    public List<RaceRelationEntry> Entries = new();
}
