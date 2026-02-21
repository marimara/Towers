using System;
using UnityEditor;

/// <summary>
/// Custom PropertyDrawer for List<EventConsequence> fields.
///
/// Replaces the default SerializeReference list UI with:
///   - "Add Consequence" button
///   - GenericMenu listing all concrete EventConsequence subclasses
///   - Auto-expansion of newly added elements
///   - Automatic cleanup of empty placeholders
///
/// Supported consequence types:
///   - FlagConsequence
///   - StatConsequence
///   - RelationshipConsequence
/// </summary>
[CustomPropertyDrawer(typeof(System.Collections.Generic.List<EventConsequence>))]
public class EventConsequenceListDrawer : PolymorphicListDrawerBase
{
    protected override Type BaseType => typeof(EventConsequence);
    protected override string ButtonLabel => "Add Consequence";

    protected override Type[] GetConcreteTypes()
    {
        return FindConcreteSubclasses(typeof(EventConsequence));
    }
}
