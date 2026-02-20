using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEngine;

/// <summary>
/// Custom Inspector for <see cref="FlagManager"/>.
///
/// At runtime: renders a live read-only table of every flag in the store,
/// sorted alphabetically, with coloured value indicators and action buttons.
/// At edit-time: shows a notice explaining that flags are runtime-only.
///
/// Editor-only — stripped from builds entirely.
/// </summary>
[CustomEditor(typeof(FlagManager))]
public class FlagManagerEditor : Editor
{
    // -------------------------------------------------------------------------
    // Cached styles — built once, reused every repaint
    // -------------------------------------------------------------------------

    private GUIStyle _headerStyle;
    private GUIStyle _rowStyle;
    private GUIStyle _trueStyle;
    private GUIStyle _falseStyle;
    private GUIStyle _noticeStyle;

    // Colour constants
    private static readonly Color TrueColour  = new(0.35f, 0.80f, 0.35f); // green
    private static readonly Color FalseColour = new(0.85f, 0.40f, 0.35f); // red
    private static readonly Color RowEvenColour = new(0.22f, 0.22f, 0.22f, 1f);
    private static readonly Color RowOddColour  = new(0.19f, 0.19f, 0.19f, 1f);

    // -------------------------------------------------------------------------
    // Inspector draw
    // -------------------------------------------------------------------------

    public override void OnInspectorGUI()
    {
        // Always draw the default MonoBehaviour fields (there are none on
        // FlagManager, but this future-proofs the inspector).
        DrawPropertiesExcluding(serializedObject, "m_Script");

        EditorGUILayout.Space(6);

        BuildStyles();

        if (!Application.isPlaying)
        {
            DrawEditModeNotice();
            return;
        }

        var manager = (FlagManager)target;
        Dictionary<string, bool> flags = manager.GetAllFlags();

        DrawHeader(flags.Count);
        EditorGUILayout.Space(4);

        if (flags.Count == 0)
        {
            EditorGUILayout.LabelField("No flags set.", _noticeStyle);
        }
        else
        {
            DrawFlagTable(flags);
        }

        EditorGUILayout.Space(8);
        DrawActionButtons(manager);

        // Keep repainting every frame so the table stays live while the game runs.
        Repaint();
    }

    // -------------------------------------------------------------------------
    // Sections
    // -------------------------------------------------------------------------

    private void DrawEditModeNotice()
    {
        EditorGUILayout.LabelField(
            "Flags are runtime-only. Enter Play Mode to inspect the live flag store.",
            _noticeStyle);
    }

    private void DrawHeader(int count)
    {
        using (new EditorGUILayout.HorizontalScope())
        {
            EditorGUILayout.LabelField("Live Flag Store", _headerStyle);
            GUILayout.FlexibleSpace();
            EditorGUILayout.LabelField(
                $"{count} flag{(count == 1 ? "" : "s")}",
                EditorStyles.miniLabel,
                GUILayout.Width(60));
        }

        // Separator line
        Rect rect = GUILayoutUtility.GetRect(1, 1, GUILayout.ExpandWidth(true));
        EditorGUI.DrawRect(rect, new Color(0.5f, 0.5f, 0.5f, 0.5f));
    }

    private void DrawFlagTable(Dictionary<string, bool> flags)
    {
        // Sort alphabetically for stable, scannable display.
        var sorted = flags.OrderBy(kvp => kvp.Key).ToList();

        // Column header row
        using (new EditorGUILayout.HorizontalScope())
        {
            EditorGUILayout.LabelField("Flag ID",    EditorStyles.miniBoldLabel);
            EditorGUILayout.LabelField("Value", EditorStyles.miniBoldLabel, GUILayout.Width(48));
        }

        for (int i = 0; i < sorted.Count; i++)
        {
            DrawFlagRow(sorted[i].Key, sorted[i].Value, i);
        }
    }

    private void DrawFlagRow(string id, bool value, int index)
    {
        // Alternating row background
        Rect rowRect = GUILayoutUtility.GetRect(0, EditorGUIUtility.singleLineHeight + 2,
                                                GUILayout.ExpandWidth(true));
        EditorGUI.DrawRect(rowRect, index % 2 == 0 ? RowEvenColour : RowOddColour);

        // Layout content inside the row rect with a small horizontal margin
        Rect contentRect = new(rowRect.x + 4, rowRect.y + 1,
                               rowRect.width - 8, rowRect.height - 2);

        float valueWidth = 48f;
        Rect idRect    = new(contentRect.x, contentRect.y,
                             contentRect.width - valueWidth - 4, contentRect.height);
        Rect valueRect = new(contentRect.xMax - valueWidth, contentRect.y,
                             valueWidth, contentRect.height);

        // Flag ID — selectable so designers can copy it
        EditorGUI.SelectableLabel(idRect, id, _rowStyle);

        // Value badge
        EditorGUI.LabelField(valueRect, value ? "true" : "false",
                             value ? _trueStyle : _falseStyle);
    }

    private void DrawActionButtons(FlagManager manager)
    {
        using (new EditorGUILayout.HorizontalScope())
        {
            // Force Refresh — triggers an immediate repaint
            if (GUILayout.Button("Force Refresh", GUILayout.Height(22)))
            {
                Repaint();
            }

            GUILayout.Space(6);

            // Clear All Flags — guarded by a confirmation dialog
            GUI.backgroundColor = new Color(0.9f, 0.35f, 0.3f);

            if (GUILayout.Button("Clear All Flags", GUILayout.Height(22)))
            {
                if (EditorUtility.DisplayDialog(
                    "Clear All Flags",
                    $"This will remove all {manager.GetAllFlags().Count} flag(s) from FlagManager. This cannot be undone.",
                    "Clear", "Cancel"))
                {
                    manager.ResetAll();
                    Repaint();
                }
            }

            GUI.backgroundColor = Color.white;
        }
    }

    // -------------------------------------------------------------------------
    // Style initialisation
    // -------------------------------------------------------------------------

    private void BuildStyles()
    {
        if (_headerStyle != null)
            return;

        _headerStyle = new GUIStyle(EditorStyles.boldLabel)
        {
            fontSize = 12,
        };

        _rowStyle = new GUIStyle(EditorStyles.label)
        {
            fontSize  = 11,
            alignment = TextAnchor.MiddleLeft,
        };

        _trueStyle = new GUIStyle(EditorStyles.miniLabel)
        {
            alignment  = TextAnchor.MiddleCenter,
            fontStyle  = FontStyle.Bold,
            normal     = { textColor = TrueColour },
        };

        _falseStyle = new GUIStyle(EditorStyles.miniLabel)
        {
            alignment  = TextAnchor.MiddleCenter,
            fontStyle  = FontStyle.Bold,
            normal     = { textColor = FalseColour },
        };

        _noticeStyle = new GUIStyle(EditorStyles.helpBox)
        {
            fontSize  = 11,
            wordWrap  = true,
            alignment = TextAnchor.MiddleLeft,
            padding   = new RectOffset(8, 8, 6, 6),
        };
    }
}
