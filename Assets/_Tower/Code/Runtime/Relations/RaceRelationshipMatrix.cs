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

    /// <summary>
    /// Automatically generates the full unilateral race relationship matrix.
    /// </summary>
    [ContextMenu("Auto Generate Matrix")]
    public void AutoGenerate()
    {
        Entries.Clear();

        var allRaces = System.Enum.GetValues(typeof(Race));

        foreach (Race from in allRaces)
        {
            foreach (Race to in allRaces)
            {
                int modifier = GetModifier(from, to);
                
                Entries.Add(new RaceRelationEntry
                {
                    From = from,
                    To = to,
                    InitialModifier = modifier
                });
            }
        }

#if UNITY_EDITOR
        UnityEditor.EditorUtility.SetDirty(this);
#endif
    }

    private int GetModifier(Race from, Race to)
    {
        // Same race bonus
        if (from == to)
            return 15;

        // Political modifiers
        return (from, to) switch
        {
            // Humans
            (Race.Human, Race.Elf) => 5,
            (Race.Human, Race.DarkElf) => -5,
            (Race.Human, Race.Dwarf) => -10,
            (Race.Human, Race.Mermaid) => 5,
            (Race.Human, Race.Ghoul) => -20,
            (Race.Human, Race.Oni) => -10,
            (Race.Human, Race.Demon) => -25,
            (Race.Human, Race.Feral) => -15,
            (Race.Human, Race.Draconite) => -5,
            (Race.Human, Race.Tiefling) => -10,

            // Elfs
            (Race.Elf, Race.Human) => 5,
            (Race.Elf, Race.DarkElf) => -25,
            (Race.Elf, Race.Dwarf) => -5,
            (Race.Elf, Race.Mermaid) => 10,
            (Race.Elf, Race.Ghoul) => -30,
            (Race.Elf, Race.Demon) => -35,
            (Race.Elf, Race.Feral) => -15,
            (Race.Elf, Race.Draconite) => 0,
            (Race.Elf, Race.Tiefling) => -20,

            // DarkElfs
            (Race.DarkElf, Race.Human) => -5,
            (Race.DarkElf, Race.Elf) => -20,
            (Race.DarkElf, Race.Dwarf) => -10,
            (Race.DarkElf, Race.Mermaid) => 0,
            (Race.DarkElf, Race.Ghoul) => -10,
            (Race.DarkElf, Race.Demon) => 5,
            (Race.DarkElf, Race.Feral) => 0,
            (Race.DarkElf, Race.Draconite) => 5,
            (Race.DarkElf, Race.Tiefling) => 10,

            // Dwarfs
            (Race.Dwarf, Race.Human) => 10,
            (Race.Dwarf, Race.Elf) => -5,
            (Race.Dwarf, Race.DarkElf) => -10,
            (Race.Dwarf, Race.Mermaid) => 0,
            (Race.Dwarf, Race.Ghoul) => -25,
            (Race.Dwarf, Race.Demon) => -30,
            (Race.Dwarf, Race.Feral) => -15,
            (Race.Dwarf, Race.Draconite) => 5,
            (Race.Dwarf, Race.Tiefling) => -10,

            // Mermaids
            (Race.Mermaid, Race.Human) => 5,
            (Race.Mermaid, Race.Elf) => 10,
            (Race.Mermaid, Race.DarkElf) => 0,
            (Race.Mermaid, Race.Dwarf) => 0,
            (Race.Mermaid, Race.Ghoul) => -15,
            (Race.Mermaid, Race.Demon) => -20,
            (Race.Mermaid, Race.Feral) => -10,
            (Race.Mermaid, Race.Draconite) => 5,
            (Race.Mermaid, Race.Tiefling) => -5,

            // Ghouls
            (Race.Ghoul, Race.Human) => -20,
            (Race.Ghoul, Race.Elf) => -30,
            (Race.Ghoul, Race.DarkElf) => -10,
            (Race.Ghoul, Race.Dwarf) => -25,
            (Race.Ghoul, Race.Mermaid) => -15,
            (Race.Ghoul, Race.Demon) => 10,
            (Race.Ghoul, Race.Oni) => 5,
            (Race.Ghoul, Race.Draconite) => 0,
            (Race.Ghoul, Race.Tiefling) => 5,

            // Onis
            (Race.Oni, Race.Human) => -10,
            (Race.Oni, Race.Elf) => -15,
            (Race.Oni, Race.DarkElf) => 0,
            (Race.Oni, Race.Dwarf) => 5,
            (Race.Oni, Race.Mermaid) => -5,
            (Race.Oni, Race.Ghoul) => 5,
            (Race.Oni, Race.Demon) => 10,
            (Race.Oni, Race.Feral) => 10,
            (Race.Oni, Race.Draconite) => 5,
            (Race.Oni, Race.Tiefling) => 5,

            // Demons
            (Race.Demon, Race.Human) => -25,
            (Race.Demon, Race.Elf) => -35,
            (Race.Demon, Race.DarkElf) => 5,
            (Race.Demon, Race.Dwarf) => -30,
            (Race.Demon, Race.Mermaid) => -20,
            (Race.Demon, Race.Ghoul) => 10,
            (Race.Demon, Race.Oni) => 10,
            (Race.Demon, Race.Feral) => 5,
            (Race.Demon, Race.Draconite) => 0,
            (Race.Demon, Race.Tiefling) => 20,

            // Ferals
            (Race.Feral, Race.Human) => -15,
            (Race.Feral, Race.Elf) => -15,
            (Race.Feral, Race.DarkElf) => 0,
            (Race.Feral, Race.Dwarf) => -15,
            (Race.Feral, Race.Mermaid) => -10,
            (Race.Feral, Race.Ghoul) => 5,
            (Race.Feral, Race.Oni) => 10,
            (Race.Feral, Race.Demon) => 5,
            (Race.Feral, Race.Draconite) => 5,
            (Race.Feral, Race.Tiefling) => 0,

            // Draconites
            (Race.Draconite, Race.Human) => -5,
            (Race.Draconite, Race.Elf) => 0,
            (Race.Draconite, Race.DarkElf) => 5,
            (Race.Draconite, Race.Dwarf) => 5,
            (Race.Draconite, Race.Mermaid) => 5,
            (Race.Draconite, Race.Ghoul) => 0,
            (Race.Draconite, Race.Oni) => 5,
            (Race.Draconite, Race.Demon) => 0,
            (Race.Draconite, Race.Feral) => 5,
            (Race.Draconite, Race.Tiefling) => 0,

            // Tieflings
            (Race.Tiefling, Race.Human) => -10,
            (Race.Tiefling, Race.Elf) => -20,
            (Race.Tiefling, Race.DarkElf) => 10,
            (Race.Tiefling, Race.Dwarf) => -10,
            (Race.Tiefling, Race.Mermaid) => -5,
            (Race.Tiefling, Race.Ghoul) => 5,
            (Race.Tiefling, Race.Oni) => 5,
            (Race.Tiefling, Race.Demon) => 20,
            (Race.Tiefling, Race.Feral) => 0,
            (Race.Tiefling, Race.Draconite) => 0,

            _ => 0
        };
    }
}
