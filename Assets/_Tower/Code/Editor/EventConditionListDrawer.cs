using System;
using UnityEditor;

/// <summary>
/// Custom PropertyDrawer for List<EventCondition> fields.
///
/// Replaces the default SerializeReference list UI with:
///   - "Add Condition" button
///   - GenericMenu listing all concrete EventCondition subclasses
///   - Auto-expansion of newly added elements
///   - Automatic cleanup of empty placeholders
///
/// Supported condition types:
///   - FlagCondition
///   - StatCondition
///   - TimeCondition
///   - TimePeriodCondition
///   - DayCondition
///   - RelationshipTierCondition
/// </summary>
[CustomPropertyDrawer(typeof(System.Collections.Generic.List<EventCondition>))]
public class EventConditionListDrawer : PolymorphicListDrawerBase
{
    protected override Type BaseType => typeof(EventCondition);
    protected override string ButtonLabel => "Add Condition";

    protected override Type[] GetConcreteTypes()
    {
        return FindConcreteSubclasses(typeof(EventCondition));
    }
}
