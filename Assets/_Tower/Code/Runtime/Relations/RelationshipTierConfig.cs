using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// ScriptableObject defining relationship tiers and their value ranges.
/// </summary>
[CreateAssetMenu(menuName = "Game/Relationship Tiers")]
public class RelationshipTierConfig : ScriptableObject
{
    public List<RelationshipTier> Tiers = new();

    /// <summary>
    /// Returns the tier name for the given relationship value.
    /// Returns "Unknown" if no tier matches.
    /// </summary>
    public string GetTierName(int value)
    {
        if (Tiers == null || Tiers.Count == 0)
            return "Unknown";

        foreach (var tier in Tiers)
        {
            if (value >= tier.Min && value <= tier.Max)
                return tier.Name;
        }

        return "Unknown";
    }
}
