using UnityEditor;
using UnityEngine;
using System.Collections.Generic;

public class DialogueNodeEditorWindow : EditorWindow
{
    private DialogueData dialogueData;

    private Vector2 panOffset;
    private float zoom = 1f;

    private const float ZoomMin = 0.4f;
    private const float ZoomMax = 2f;
    private const float NodeWidth = 280f;

    private DialogueNode linkingNode;
    private int linkingChoiceIndex = -1;

    [MenuItem("VN/Dialogue Node Editor")]
    public static void Open()
    {
        GetWindow<DialogueNodeEditorWindow>("Dialogue Editor");
    }

    private void OnGUI()
    {
        DrawToolbar();
        HandlePanZoom();

        if (dialogueData == null) return;

        Matrix4x4 old = GUI.matrix;
        GUI.matrix = Matrix4x4.TRS(panOffset, Quaternion.identity, Vector3.one * zoom);

        HandleLinking();
        DrawConnections();
        DrawNodes();

        GUI.matrix = old;

        if (GUI.changed)
            EditorUtility.SetDirty(dialogueData);
    }

    #region Toolbar

    private void DrawToolbar()
    {
        GUILayout.BeginHorizontal(EditorStyles.toolbar);

        dialogueData = (DialogueData)EditorGUILayout.ObjectField(
            dialogueData,
            typeof(DialogueData),
            false,
            GUILayout.Width(280)
        );

        if (dialogueData && GUILayout.Button("➕ Add Node", EditorStyles.toolbarButton))
            CreateNode();

        GUILayout.EndHorizontal();
    }

    #endregion

    #region Nodes

    private void DrawNodes()
    {
        BeginWindows();

        for (int i = 0; i < dialogueData.Nodes.Count; i++)
        {
            var node = dialogueData.Nodes[i];

            Rect rect = new Rect(
                node.EditorPosition.x,
                node.EditorPosition.y,
                NodeWidth,
                GetNodeHeight(node)
            );

            Rect newRect = GUI.Window(i, rect, id => DrawNode(id), $"Node {node.Id}");

            node.EditorPosition = new Vector2(newRect.x, newRect.y);
        }

        EndWindows();
    }

    private void DrawNode(int id)
    {
        var node = dialogueData.Nodes[id];

        Color old = GUI.backgroundColor;
        GUI.backgroundColor = GetSpeakerColor(node.Speaker);

        GUILayout.BeginVertical("box");

        DrawNodeHeader(id);
        DrawNodeContent(node);
        DrawChoices(node);

        GUILayout.EndVertical();

        GUI.backgroundColor = old;
        GUI.DragWindow();
    }

    private void DrawNodeHeader(int id)
    {
        GUILayout.BeginHorizontal();
        GUILayout.Label($"Node {id}", EditorStyles.boldLabel);

        if (GUILayout.Button("✖", GUILayout.Width(22)))
        {
            RemoveNode(id);
            GUIUtility.ExitGUI();
        }

        GUILayout.EndHorizontal();
    }

    private void DrawNodeContent(DialogueNode node)
    {
        node.Speaker = (Speaker)EditorGUILayout.EnumPopup(node.Speaker);
        node.Text = EditorGUILayout.TextArea(node.Text, GUILayout.Height(50));
    }

    private void DrawChoices(DialogueNode node)
    {
        if (node.Choices == null)
            node.Choices = new List<DialogueChoice>();

        GUILayout.Space(6);
        GUILayout.Label("Choices");

        for (int i = 0; i < node.Choices.Count; i++)
            DrawChoiceRow(node, i);

        if (GUILayout.Button("+ Add Choice"))
            node.Choices.Add(NewChoice());
    }

    private void DrawChoiceRow(DialogueNode node, int index)
    {
        GUI.backgroundColor = new Color(0.25f, 0.6f, 0.3f);

        GUILayout.BeginHorizontal("box");

        node.Choices[index].Text =
            EditorGUILayout.TextField(node.Choices[index].Text);

        if (GUILayout.Button("○", GUILayout.Width(22)))
        {
            linkingNode = node;
            linkingChoiceIndex = index;
        }

        if (GUILayout.Button("✖", GUILayout.Width(22)))
        {
            node.Choices.RemoveAt(index);
            GUIUtility.ExitGUI();
        }

        GUILayout.EndHorizontal();
    }

    #endregion

    #region Connections

    private void DrawConnections()
    {
        foreach (var node in dialogueData.Nodes)
        {
            if (node.Choices == null) continue;

            for (int i = 0; i < node.Choices.Count; i++)
            {
                var choice = node.Choices[i];
                if (!IsValidTarget(choice.NextNode)) continue;

                var target = dialogueData.Nodes[choice.NextNode];

                Vector2 start = GetChoiceOutput(node, i);
                Vector2 end = GetNodeInput(target);

                DrawBezier(start, end, Color.cyan);

                if (IsClickOnConnection(start, end))
                    choice.NextNode = -1;
            }
        }
    }

    private void HandleLinking()
    {
        if (linkingNode == null) return;

        Event e = Event.current;
        Vector2 mouse = (e.mousePosition - panOffset) / zoom;

        DrawBezier(GetChoiceOutput(linkingNode, linkingChoiceIndex), mouse, Color.yellow);

        if (e.type != EventType.MouseUp) return;

        foreach (var node in dialogueData.Nodes)
        {
            if (GetNodeRect(node).Contains(mouse))
            {
                if (node != linkingNode)
                    linkingNode.Choices[linkingChoiceIndex].NextNode = node.Id;

                linkingNode = null;
                linkingChoiceIndex = -1;
                e.Use();
                break;
            }
        }
    }

    #endregion

    #region Helpers

    private void CreateNode()
    {
        dialogueData.Nodes.Add(new DialogueNode
        {
            Id = dialogueData.Nodes.Count,
            Text = "New dialogue...",
            Speaker = Speaker.Narrator,
            EditorPosition = new Vector2(150, 150)
        });
    }

    private void RemoveNode(int id)
    {
        dialogueData.Nodes.RemoveAt(id);

        for (int i = 0; i < dialogueData.Nodes.Count; i++)
            dialogueData.Nodes[i].Id = i;

        foreach (var node in dialogueData.Nodes)
            if (node.Choices != null)
                foreach (var c in node.Choices)
                    if (c.NextNode >= dialogueData.Nodes.Count)
                        c.NextNode = -1;
    }

    private DialogueChoice NewChoice()
    {
        return new DialogueChoice
        {
            Text = "New choice...",
            NextNode = -1
        };
    }

    private Rect GetNodeRect(DialogueNode node)
    {
        return new Rect(
            node.EditorPosition.x,
            node.EditorPosition.y,
            NodeWidth,
            GetNodeHeight(node)
        );
    }

    private Vector2 GetChoiceOutput(DialogueNode node, int index)
    {
        return new Vector2(
            node.EditorPosition.x + NodeWidth,
            node.EditorPosition.y + 110 + index * 40
        );
    }

    private Vector2 GetNodeInput(DialogueNode node)
    {
        return new Vector2(
            node.EditorPosition.x,
            node.EditorPosition.y + GetNodeHeight(node) * 0.5f
        );
    }

    private bool IsValidTarget(int index)
    {
        return index >= 0 && index < dialogueData.Nodes.Count;
    }

    private bool IsClickOnConnection(Vector2 start, Vector2 end)
    {
        if (Event.current.type != EventType.MouseDown) return false;

        float dist = HandleUtility.DistancePointBezier(
            (Event.current.mousePosition - panOffset) / zoom,
            start,
            end,
            start + Vector2.right * 80,
            end + Vector2.left * 80
        );

        return dist < 10f;
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

    private void HandlePanZoom()
    {
        Event e = Event.current;

        if (e.type == EventType.ScrollWheel)
        {
            zoom = Mathf.Clamp(zoom - e.delta.y * 0.03f, ZoomMin, ZoomMax);
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
        return 160 + (node.Choices?.Count ?? 0) * 40;
    }

    private Color GetSpeakerColor(Speaker speaker)
    {
        return speaker switch
        {
            Speaker.Left => new Color(0.3f, 0.4f, 0.8f),
            Speaker.Right => new Color(0.8f, 0.35f, 0.35f),
            Speaker.Narrator => new Color(0.5f, 0.5f, 0.5f),
            _ => Color.white
        };
    }

    #endregion
}
