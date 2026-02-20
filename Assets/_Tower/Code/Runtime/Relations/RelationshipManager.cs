using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Manages unilateral relationships between VNCharacter pairs.
/// Base value is 50, modified by race relationships from RaceRelationshipMatrix.
/// All values are clamped between 1 and 100.
/// </summary>
public class RelationshipManager
{
    private const int BASE_VALUE = 50;
    private const int MIN_VALUE = 1;
    private const int MAX_VALUE = 100;

    private Dictionary<(VNCharacter from, VNCharacter to), int> _relationships = new();
    private RaceRelationshipMatrix _matrix;

    /// <summary>
    /// Initialize relationships for all character pairs.
    /// Applies race modifiers from the matrix to the base value.
    /// </summary>
    public void Initialize(List<VNCharacter> importantCharacters, RaceRelationshipMatrix matrix)
    {
        _matrix = matrix;
        _relationships.Clear();

        if (importantCharacters == null || importantCharacters.Count == 0)
            return;

        foreach (var from in importantCharacters)
        {
            if (from == null) continue;

            foreach (var to in importantCharacters)
            {
                if (to == null || from == to) continue;

                int value = BASE_VALUE;

                // Apply race modifier if matrix exists
                if (matrix != null)
                {
                    int raceModifier = GetRaceModifier(from.Race, to.Race);
                    value += raceModifier;
                }

                _relationships[(from, to)] = Mathf.Clamp(value, MIN_VALUE, MAX_VALUE);
            }
        }
    }

    /// <summary>
    /// Get the relationship value from one character to another.
    /// Returns BASE_VALUE if the relationship hasn't been initialized.
    /// </summary>
    public int Get(VNCharacter from, VNCharacter to)
    {
        if (from == null || to == null || from == to)
            return BASE_VALUE;

        if (_relationships.TryGetValue((from, to), out int value))
            return value;

        return BASE_VALUE;
    }

    /// <summary>
    /// Modify the relationship value between two characters by a delta.
    /// Automatically clamps the result between MIN_VALUE and MAX_VALUE.
    /// Creates relationship entry if it doesn't exist (base 50 + race modifier).
    /// </summary>
    public void Modify(VNCharacter from, VNCharacter to, int delta)
    {
        if (from == null || to == null || from == to)
            return;

        // Ensure relationship exists before modifying
        if (!_relationships.ContainsKey((from, to)))
        {
            int baseValue = BASE_VALUE;
            if (_matrix != null)
            {
                int raceModifier = GetRaceModifier(from.Race, to.Race);
                baseValue += raceModifier;
            }
            _relationships[(from, to)] = Mathf.Clamp(baseValue, MIN_VALUE, MAX_VALUE);
        }

        int current = _relationships[(from, to)];
        int newValue = Mathf.Clamp(current + delta, MIN_VALUE, MAX_VALUE);
        _relationships[(from, to)] = newValue;
    }

    private int GetRaceModifier(Race from, Race to)
    {
        if (_matrix == null || _matrix.Entries == null)
            return 0;

        foreach (var entry in _matrix.Entries)
        {
            if (entry.From == from && entry.To == to)
                return entry.InitialModifier;
        }

        return 0;
    }
}
