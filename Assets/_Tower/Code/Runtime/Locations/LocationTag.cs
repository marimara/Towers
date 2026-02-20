/// <summary>
/// Strongly typed tags for categorising and filtering locations.
/// Add new values here as the game expands; existing assets serialise by integer index,
/// so only append — never reorder or remove entries.
/// </summary>
public enum LocationTag
{
    City,
    Guild,
    Tavern,
    Forest,
    Dungeon,
    SafeZone,
    Market,
    CombatZone,
    StoryCritical,
}
