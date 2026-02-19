/// <summary>
/// Defines a single relationship effect between two characters.
/// </summary>
[System.Serializable]
public class RelationshipEffectEntry
{
    public VNCharacter From;
    public VNCharacter To;
    public int Delta;
}
