using UnityEditor;
using UnityEngine;

public class DialogueNodeEditorWindow : EditorWindow
{
    private DialogueData dialogueData;

    private Vector2 dragOffset;

    private DialogueNode linkingNode;
    private int linkingChoiceIndex = -1;

    private int focusChoiceNode = -1;
    private int focusChoiceIndex = -1;

    // Zoom & Pan
    private Vector2 panOffset;
    private float zoom = 1f;

    private const float ZoomMin = 0.4f;
    private const float ZoomMax = 2f;

    [MenuItem("VN/Dialogue Node Editor")]
    public static void Open()
    {
        GetWindow<DialogueNodeEditorWindow>("Dialogue Editor");
    }

    private void OnGUI()
    {
        DrawToolbar();
        HandlePanAndZoom();

        Matrix4x4 oldMatrix = GUI.matrix;
        GUI.matrix = Matrix4x4.TRS(
            panOffset,
            Quaternion.identity,
            Vector3.one * zoom
        );

        if (dialogueData == null) return;

        HandleLinking();
        DrawConnections();

        BeginWindows();

        for (int i = 0; i < dialogueData.Nodes.Count; i++)
        {
            var node = dialogueData.Nodes[i];

            Rect rect = new Rect(
                node.EditorPosition.x,
                node.EditorPosition.y,
                280,
                GetNodeHeight(node)
            );

            Rect newRect = GUI.Window(
                i,
                rect,
                DrawNodeWindow,
                $"Node {node.Id}"
            );

            node.EditorPosition = new Vector2(newRect.x, newRect.y);
        }

        EndWindows();

        if (GUI.changed)
            EditorUtility.SetDirty(dialogueData);

        GUI.matrix = oldMatrix;
    }

    private void DrawToolbar()
    {
        GUILayout.BeginHorizontal(EditorStyles.toolbar);

        dialogueData = (DialogueData)EditorGUILayout.ObjectField(
            dialogueData,
            typeof(DialogueData),
            false,
            GUILayout.Width(300)
        );

        if (dialogueData != null)
        {
            if (GUILayout.Button("➕ Add Node", EditorStyles.toolbarButton))
            {
                AddNode();
            }
        }

        GUILayout.EndHorizontal();
    }

    private void AddNode()
    {
        DialogueNode newNode = new DialogueNode();

        newNode.Id = dialogueData.Nodes.Count;
        newNode.Text = "New dialogue...";
        newNode.Speaker = Speaker.Narrator;

        newNode.EditorPosition = new Vector2(
            100 + dialogueData.Nodes.Count * 30,
            100
        );

        dialogueData.Nodes.Add(newNode);
        EditorUtility.SetDirty(dialogueData);
    }

    private void DrawNodeWindow(int id)
    {
        var node = dialogueData.Nodes[id];

        Color oldColor = GUI.backgroundColor;

        switch (node.Speaker)
        {
            case Speaker.Left:
                GUI.backgroundColor = new Color(0.25f, 0.35f, 0.7f);
                break;

            case Speaker.Right:
                GUI.backgroundColor = new Color(0.7f, 0.25f, 0.25f);
                break;

            case Speaker.Narrator:
                GUI.backgroundColor = new Color(0.4f, 0.4f, 0.4f);
                break;
        }

        GUILayout.BeginVertical("box");

        GUILayout.BeginHorizontal();
        GUILayout.Label($"Node {node.Id}", EditorStyles.boldLabel);

        if (GUILayout.Button("✖", GUILayout.Width(24)))
        {
            DeleteNode(id);
            return;
        }

        GUILayout.EndHorizontal();

        node.Speaker = (Speaker)EditorGUILayout.EnumPopup(node.Speaker);

        node.Text = EditorGUILayout.TextArea(
            node.Text,
            GUILayout.Height(50)
        );

        GUILayout.Space(6);
        GUILayout.Label("Choices:");

        if (node.Choices == null)
            node.Choices = new();

        for (int i = 0; i < node.Choices.Count; i++)
        {
            GUI.backgroundColor = new Color(0.25f, 0.6f, 0.3f);

            GUILayout.BeginHorizontal("box");

            node.Choices[i].Text = EditorGUILayout.TextField(
                node.Choices[i].Text
            );

            if (GUILayout.Button("○", GUILayout.Width(22)))
            {
                linkingNode = node;
                linkingChoiceIndex = i;
            }

            if (GUILayout.Button("✖", GUILayout.Width(22)))
            {
                node.Choices.RemoveAt(i);
                return;
            }

            GUILayout.EndHorizontal();
        }

        GUI.backgroundColor = new Color(0.25f, 0.6f, 0.3f);

        if (GUILayout.Button("+ Add Choice"))
        {
            node.Choices.Add(new DialogueChoice
            {
                Text = "New choice...",
                NextNode = -1
            });
        }

        GUILayout.EndVertical();

        GUI.backgroundColor = oldColor;

        GUI.DragWindow();
    }

    private void DrawConnections()
    {
        Event e = Event.current;

        foreach (var node in dialogueData.Nodes)
        {
            if (node.Choices == null) continue;

            for (int i = 0; i < node.Choices.Count; i++)
            {
                var choice = node.Choices[i];

                if (choice.NextNode < 0 ||
                    choice.NextNode >= dialogueData.Nodes.Count)
                    continue;

                var target = dialogueData.Nodes[choice.NextNode];

                Vector2 start = new Vector2(
                    node.EditorPosition.x + 280,
                    node.EditorPosition.y + 120 + i * 40
                );

                Vector2 end = new Vector2(
                    target.EditorPosition.x,
                    target.EditorPosition.y + GetNodeHeight(target) * 0.5f
                );

                DrawBezier(start, end, Color.cyan);

                if (e.type == EventType.MouseDown &&
                    IsMouseOverConnection(start, end))
                {
                    choice.NextNode = -1;
                    e.Use();
                    EditorUtility.SetDirty(dialogueData);
                }
            }
        }
    }

    private void DrawBezier(Vector2 start, Vector2 end, Color color)
    {
        Handles.BeginGUI();

        Handles.DrawBezier(
            start,
            end,
            start + Vector2.right * 80,
            end + Vector2.left * 80,
            color,
            null,
            3f
        );

        Handles.EndGUI();
    }

    private void DeleteNode(int index)
    {
        dialogueData.Nodes.RemoveAt(index);
        ReindexNodes();
        EditorUtility.SetDirty(dialogueData);
    }

    private void ReindexNodes()
    {
        for (int i = 0; i < dialogueData.Nodes.Count; i++)
        {
            dialogueData.Nodes[i].Id = i;
        }

        foreach (var node in dialogueData.Nodes)
        {
            if (node.NextNode >= dialogueData.Nodes.Count)
                node.NextNode = -1;

            if (node.Choices != null)
            {
                foreach (var choice in node.Choices)
                {
                    if (choice.NextNode >= dialogueData.Nodes.Count)
                        choice.NextNode = -1;
                }
            }
        }
    }

    private void HandleLinking()
    {
        if (linkingNode == null || linkingChoiceIndex == -1)
            return;

        Event e = Event.current;

        Vector2 mousePos = (e.mousePosition - panOffset) / zoom;

        DrawTemporaryConnection(mousePos);

        if (e.type != EventType.MouseUp)
            return;

        foreach (var target in dialogueData.Nodes)
        {
            Rect rect = new Rect(
                target.EditorPosition.x,
                target.EditorPosition.y,
                280,
                GetNodeHeight(target)
            );

            if (!rect.Contains(mousePos)) continue;
            if (target == linkingNode) break;

            linkingNode.Choices[linkingChoiceIndex].NextNode = target.Id;

            linkingNode = null;
            linkingChoiceIndex = -1;

            EditorUtility.SetDirty(dialogueData);
            e.Use();
            break;
        }
    }

    private void DrawTemporaryConnection(Vector2 mousePos)
    {
        if (linkingNode == null) return;

        Vector2 start = new Vector2(
            linkingNode.EditorPosition.x + 280,
            linkingNode.EditorPosition.y + 80
        );

        DrawBezier(start, mousePos, Color.yellow);
        Repaint();
    }

    private void CreateChoiceLink(DialogueNode from, DialogueNode to)
    {
        if (from.Choices == null)
            from.Choices = new();

        DialogueChoice newChoice = new DialogueChoice
        {
            Text = "New choice...",
            NextNode = to.Id
        };

        from.Choices.Add(newChoice);
        from.NextNode = -1;
    }

    private bool IsMouseOverConnection(Vector2 start, Vector2 end)
    {
        float distance = HandleUtility.DistancePointBezier(
            (Event.current.mousePosition - panOffset) / zoom,
            start,
            end,
            start + Vector2.right * 80,
            end + Vector2.left * 80
        );

        return distance < 10f;
    }

    private void HandlePanAndZoom()
    {
        Event e = Event.current;

        if (e.type == EventType.ScrollWheel)
        {
            float zoomDelta = -e.delta.y * 0.03f;
            zoom = Mathf.Clamp(zoom + zoomDelta, ZoomMin, ZoomMax);
            e.Use();
        }

        if (e.type == EventType.MouseDrag && e.button == 2)
        {
            panOffset += e.delta;
            e.Use();
        }
    }

    private float GetNodeHeight(DialogueNode node)
    {
        int choiceCount = node.Choices != null
            ? node.Choices.Count
            : 0;

        return 160 + choiceCount * 40 + 30;
    }

    private Color GetSpeakerColor(Speaker speaker)
    {
        switch (speaker)
        {
            case Speaker.Left:
                return new Color(0.35f, 0.45f, 0.8f);

            case Speaker.Right:
                return new Color(0.8f, 0.35f, 0.35f);

            case Speaker.Narrator:
                return new Color(0.5f, 0.5f, 0.5f);

            default:
                return Color.white;
        }
    }
}
