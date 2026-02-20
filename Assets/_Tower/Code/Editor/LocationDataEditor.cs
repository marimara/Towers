using UnityEditor;
using UnityEngine;

/// <summary>
/// Custom Inspector for LocationData.
/// Renders the auto-generated ID as a read-only field so designers can
/// copy it for reference without being able to edit it accidentally.
/// All other fields are drawn by the default Inspector pipeline.
/// </summary>
[CustomEditor(typeof(LocationData))]
public class LocationDataEditor : Editor
{
    // Cached style — created once per Inspector instance.
    private GUIStyle _readOnlyStyle;

    public override void OnInspectorGUI()
    {
        // Sync serialised fields before drawing.
        serializedObject.Update();

        DrawReadOnlyId();

        EditorGUILayout.Space(4);

        // Draw every serialised field except _id (HideInInspector keeps it hidden
        // in the default loop; we drew it manually above).
        DrawPropertiesExcluding(serializedObject, "m_Script");

        serializedObject.ApplyModifiedProperties();
    }

    // -------------------------------------------------------------------------
    // Private helpers
    // -------------------------------------------------------------------------

    private void DrawReadOnlyId()
    {
        var locationData = (LocationData)target;

        _readOnlyStyle ??= new GUIStyle(EditorStyles.helpBox)
        {
            wordWrap      = false,
            stretchWidth  = true,
            fontSize      = EditorStyles.label.fontSize,
            normal        = { textColor = new Color(0.6f, 0.6f, 0.6f) },
        };

        using (new EditorGUILayout.HorizontalScope())
        {
            EditorGUILayout.PrefixLabel(new GUIContent(
                "ID",
                "Auto-generated GUID. Read-only — assigned on asset creation."));

            EditorGUILayout.SelectableLabel(
                locationData.Id,
                _readOnlyStyle,
                GUILayout.Height(EditorGUIUtility.singleLineHeight));
        }
    }
}
