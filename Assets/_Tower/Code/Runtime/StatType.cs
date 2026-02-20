/// <summary>
/// Identifies which character stat a <see cref="StatCondition"/> checks.
///
/// Append new values at the end only — serialised as int by Unity.
/// </summary>
public enum StatType
{
    /// <summary>Raw physical power; affects combat damage.</summary>
    Strength,

    /// <summary>Nimbleness and reaction speed.</summary>
    Agility,

    /// <summary>Raw magical aptitude.</summary>
    Intellect,

    /// <summary>Persuasion, social influence, and first impressions.</summary>
    Charisma,

    /// <summary>Ability to endure hardship and resist effects.</summary>
    Endurance,

    /// <summary>Fortune-based outcomes and rare event chances.</summary>
    Luck,
}
