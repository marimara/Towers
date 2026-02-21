using System;
using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEngine;

/// <summary>
/// Base class for custom PropertyDrawers that manage SerializeReference polymorphic lists.
///
/// Provides:
///   - "Add [Type]" button that opens GenericMenu with concrete subclass options
///   - Automatic instantiation and addition of selected types
///   - Removal of empty placeholder elements
///   - Auto-expansion of newly added elements
///   - Proper Undo/Redo integration
///
/// Subclasses only need to override:
///   - BaseType property (the abstract base class)
///   - GetConcreteTypes() method (find all concrete subclasses)
///   - ButtonLabel property (e.g., "Add Condition")
/// </summary>
public abstract class PolymorphicListDrawerBase : PropertyDrawer
{
    private const float ButtonHeight = 24f;
    private const float Padding = 2f;

    // -------------------------------------------------------------------------
    // Abstract API - Override in subclasses
    // -------------------------------------------------------------------------

    /// <summary>
    /// The base type (abstract) for this list (e.g., EventCondition).
    /// </summary>
    protected abstract Type BaseType { get; }

    /// <summary>
    /// Button label shown in the Inspector (e.g., "Add Condition").
    /// </summary>
    protected abstract string ButtonLabel { get; }

    /// <summary>
    /// Return all concrete subclasses of BaseType that should appear in the menu.
    /// </summary>
    protected abstract Type[] GetConcreteTypes();

    // -------------------------------------------------------------------------
    // PropertyDrawer Overrides
    // -------------------------------------------------------------------------

    public override float GetPropertyHeight(SerializedProperty property, GUIContent label)
    {
        if (!property.isArray)
            return EditorGUIUtility.singleLineHeight;

        // Foldout + Button always visible
        float height = EditorGUIUtility.singleLineHeight + ButtonHeight + Padding * 2;

        // Add element heights if expanded
        if (property.isExpanded)
        {
            for (int i = 0; i < property.arraySize; i++)
            {
                var element = property.GetArrayElementAtIndex(i);
                if (element.managedReferenceValue != null)
                {
                    height += EditorGUI.GetPropertyHeight(element, true) + Padding;
                }
            }
        }

        return height;
    }

    public override void OnGUI(Rect position, SerializedProperty property, GUIContent label)
    {
        try
        {
            // SerializeReference lists may report as Generic type; check properly
            if (!property.isArray)
            {
                EditorGUI.PropertyField(position, property, label);
                return;
            }

            // ===== CLEANUP FIRST =====
            // Remove null entries before drawing anything
            RemoveEmptyElements(property);

        // Draw foldout with count
        EditorGUI.indentLevel++;
        var foldoutRect = new Rect(position.x, position.y, position.width - 20, EditorGUIUtility.singleLineHeight);

        GUIContent foldoutLabel = new GUIContent(
            $"{label.text} ({property.arraySize})",
            label.tooltip
        );
        property.isExpanded = EditorGUI.Foldout(foldoutRect, property.isExpanded, foldoutLabel, true);
        EditorGUI.indentLevel--;

        float currentY = position.y + EditorGUIUtility.singleLineHeight + Padding;

        // Draw "Add" button - Always visible
        var buttonRect = new Rect(position.x, currentY, position.width, ButtonHeight);
        if (GUI.Button(buttonRect, ButtonLabel))
        {
            ShowAddMenu(property);
        }

        currentY += ButtonHeight + Padding;

        // Draw array elements if expanded
        if (property.isExpanded)
        {
            for (int i = 0; i < property.arraySize; i++)
            {
                var element = property.GetArrayElementAtIndex(i);

                // Skip drawing null elements (they'll be removed at start of next OnGUI)
                if (element.managedReferenceValue == null)
                    continue;

                float elementHeight = EditorGUI.GetPropertyHeight(element, true);
                var elementRect = new Rect(position.x, currentY, position.width, elementHeight);

                EditorGUI.PropertyField(elementRect, element, true);
                currentY += elementHeight + Padding;
            }
        }
        }
        catch (Exception ex)
        {
            EditorGUI.HelpBox(position, $"PropertyDrawer error: {ex.Message}", MessageType.Error);
            Debug.LogError($"[PolymorphicListDrawer] Exception in OnGUI for {property.name}: {ex.Message}\n{ex.StackTrace}");
        }
    }

    // -------------------------------------------------------------------------
    // Implementation - Add Menu
    // -------------------------------------------------------------------------

    private void ShowAddMenu(SerializedProperty property)
    {
        var menu = new GenericMenu();
        var concreteTypes = GetConcreteTypes();

        foreach (var type in concreteTypes)
        {
            var displayName = GetDisplayName(type);
            menu.AddItem(new GUIContent(displayName), false, () => AddElement(property, type));
        }

        menu.ShowAsContext();
    }

    private void AddElement(SerializedProperty property, Type type)
    {
        try
        {
            Undo.RecordObject(property.serializedObject.targetObject, $"Add {type.Name}");

            property.arraySize++;
            var newElement = property.GetArrayElementAtIndex(property.arraySize - 1);

            // Create instance via SerializeReference
            var instance = Activator.CreateInstance(type);
            if (instance == null)
            {
                Debug.LogError($"Failed to create instance of {type.Name}. Reverting array size.");
                property.arraySize--;
                return;
            }

            newElement.managedReferenceValue = instance;
            property.serializedObject.ApplyModifiedProperties();

            // Mark for expansion AFTER serialization
            newElement.isExpanded = true;
            property.serializedObject.ApplyModifiedProperties();

            Undo.FlushUndoRecordObjects();
        }
        catch (Exception ex)
        {
            Debug.LogError($"Failed to add {type.Name}: {ex.Message}");
        }
    }

    // -------------------------------------------------------------------------
    // Implementation - Cleanup
    // -------------------------------------------------------------------------

    private void RemoveEmptyElements(SerializedProperty property)
    {
        bool removed = false;

        // Iterate backwards to safely delete elements
        for (int i = property.arraySize - 1; i >= 0; i--)
        {
            var element = property.GetArrayElementAtIndex(i);

            // Check if element is null
            if (element.managedReferenceValue == null)
            {
                property.DeleteArrayElementAtIndex(i);
                removed = true;
            }
        }

        // Apply changes if any elements were removed
        if (removed)
        {
            property.serializedObject.ApplyModifiedProperties();
        }
    }

    // -------------------------------------------------------------------------
    // Utility
    // -------------------------------------------------------------------------

    private static string GetDisplayName(Type type)
    {
        // Remove "Condition" or "Consequence" suffix for cleaner display
        string name = type.Name;
        if (name.EndsWith("Condition"))
            name = name.Substring(0, name.Length - "Condition".Length);
        if (name.EndsWith("Consequence"))
            name = name.Substring(0, name.Length - "Consequence".Length);
        return name;
    }

    /// <summary>
    /// Helper to find all concrete subclasses of a base type in all assemblies.
    /// </summary>
    protected static Type[] FindConcreteSubclasses(Type baseType)
    {
        return AppDomain.CurrentDomain
            .GetAssemblies()
            .SelectMany(a => a.GetTypes())
            .Where(t => !t.IsAbstract && baseType.IsAssignableFrom(t) && t != baseType)
            .OrderBy(t => t.Name)
            .ToArray();
    }
}
