using UnityEditor;
using UnityEngine;
using System.Collections.Generic;

public class DialogueNodeEditorWindow : EditorWindow
{
    private DialogueData dialogueData;

    private Vector2 pan;
    private float zoom = 1f;

    private const float ZoomMin = 0.4f;
    private const float ZoomMax = 2.5f;

    private DialogueNode linkingNode;
    private int linkingChoice = -1;

    private Rect canvasRect = new Rect(-100000, -100000, 200000, 200000);

    [MenuItem("VN/Dialogue Node Editor")]
    public static void Open()
    {
        GetWindow<DialogueNodeEditorWindow>("Dialogue Editor");
    }

    void OnGUI()
    {
        DrawToolbar();
        HandleInput();

        if (dialogueData == null)
            return;

        if (dialogueData.Nodes == null)
            dialogueData.Nodes = new List<DialogueNode>();

        DrawGrid();

        Matrix4x4 old = GUI.matrix;
        GUI.matrix = Matrix4x4.TRS(pan, Quaternion.identity, Vector3.one * zoom);

        DrawConnections();
        DrawNodes();

        GUI.matrix = old;

        HandleContextMenu(); // <-- vamos adicionar isso

        if (GUI.changed)
            EditorUtility.SetDirty(dialogueData);
    }

    #region Toolbar

    void DrawToolbar()
    {
        GUILayout.BeginHorizontal(EditorStyles.toolbar);

        dialogueData = (DialogueData)EditorGUILayout.ObjectField(
            dialogueData,
            typeof(DialogueData),
            false,
            GUILayout.Width(300));

        if (dialogueData && GUILayout.Button("➕ Add Node", EditorStyles.toolbarButton))
            AddNode();

        GUILayout.EndHorizontal();
    }

    #endregion

    #region Input

    void HandleInput()
    {
        Event e = Event.current;

        if (e.type == EventType.ScrollWheel)
        {
            float oldZoom = zoom;
            zoom = Mathf.Clamp(zoom - e.delta.y * 0.03f, ZoomMin, ZoomMax);

            Vector2 mouse = e.mousePosition;
            pan += (mouse - pan) - (oldZoom / zoom) * (mouse - pan);

            e.Use();
        }

        if (e.type == EventType.MouseDrag && e.button == 2)
        {
            pan += e.delta;
            e.Use();
        }
    }

    Vector2 ScreenToGraph(Vector2 pos)
    {
        return (pos - pan) / zoom;
    }

    #endregion

    #region Grid

    void DrawGrid()
    {
        int spacing = 40;
        Handles.color = new Color(0, 0, 0, 0.25f);

        Vector2 offset = new Vector2(pan.x % spacing, pan.y % spacing);

        for (float x = offset.x; x < position.width; x += spacing)
            Handles.DrawLine(new Vector3(x, 0), new Vector3(x, position.height));

        for (float y = offset.y; y < position.height; y += spacing)
            Handles.DrawLine(new Vector3(0, y), new Vector3(position.width, y));
    }

    #endregion

    #region Nodes

    void DrawNodes()
    {
        BeginWindows();

        for (int i = 0; i < dialogueData.Nodes.Count; i++)
        {
            DialogueNode node = dialogueData.Nodes[i];

            Rect r = new Rect(
                node.EditorPosition.x,
                node.EditorPosition.y,
                280,
                GetHeight(node));

            r = GUI.Window(i, r, DrawNodeWindow, $"Node {node.Id}");
            node.EditorPosition = r.position;
        }

        EndWindows();
    }

    void DrawNodeWindow(int id)
    {
        DialogueNode node = dialogueData.Nodes[id];

        Color old = GUI.backgroundColor;
        GUI.backgroundColor = GetSpeakerColor(node.Speaker);

        GUILayout.BeginVertical("box");

        node.Speaker = (Speaker)EditorGUILayout.EnumPopup(node.Speaker);
        node.Text = EditorGUILayout.TextArea(node.Text, GUILayout.Height(50));

        GUILayout.Space(4);
        GUILayout.Label("Choices:");

        if (node.Choices == null)
            node.Choices = new List<DialogueChoice>();

        for (int i = 0; i < node.Choices.Count; i++)
        {
            GUILayout.BeginHorizontal("box");

            node.Choices[i].Text = EditorGUILayout.TextField(node.Choices[i].Text);

            if (GUILayout.Button("○", GUILayout.Width(22)))
            {
                linkingNode = node;
                linkingChoice = i;
            }

            if (GUILayout.Button("✖", GUILayout.Width(22)))
            {
                node.Choices.RemoveAt(i);
                return;
            }

            GUILayout.EndHorizontal();
        }

        if (GUILayout.Button("+ Add Choice"))
            node.Choices.Add(new DialogueChoice { Text = "New choice...", NextNode = -1 });

        if (GUILayout.Button("Delete Node"))
        {
            dialogueData.Nodes.RemoveAt(id);
            Reindex();
            return;
        }

        GUILayout.EndVertical();

        GUI.backgroundColor = old;

        GUI.DragWindow();
    }

    #endregion

    #region Connections

    void DrawConnections()
    {
        Event e = Event.current;

        foreach (DialogueNode node in dialogueData.Nodes)
        {
            if (node.Choices == null) continue;

            for (int i = 0; i < node.Choices.Count; i++)
            {
                int next = node.Choices[i].NextNode;
                if (next < 0 || next >= dialogueData.Nodes.Count) continue;

                DialogueNode target = dialogueData.Nodes[next];

                Vector2 start = node.EditorPosition + new Vector2(280, 80 + i * 38);
                Vector2 end = target.EditorPosition + new Vector2(0, GetHeight(target) * 0.5f);

                DrawBezier(start, end, Color.cyan);

                if (e.type == EventType.MouseDown &&
                    DistanceToBezier(ScreenToGraph(e.mousePosition), start, end) < 10f)
                {
                    node.Choices[i].NextNode = -1;
                    e.Use();
                }
            }
        }

        if (linkingNode != null)
            DrawTempLink();
    }

    void DrawTempLink()
    {
        Vector2 mouse = ScreenToGraph(Event.current.mousePosition);

        Vector2 start = linkingNode.EditorPosition +
                        new Vector2(280, 80 + linkingChoice * 38);

        DrawBezier(start, mouse, Color.yellow);

        if (Event.current.type == EventType.MouseUp)
        {
            foreach (DialogueNode target in dialogueData.Nodes)
            {
                Rect r = new Rect(
                    target.EditorPosition,
                    new Vector2(280, GetHeight(target)));

                if (r.Contains(mouse) && target != linkingNode)
                {
                    linkingNode.Choices[linkingChoice].NextNode = target.Id;
                    break;
                }
            }

            linkingNode = null;
            linkingChoice = -1;
        }

        Repaint();
    }

    void DrawBezier(Vector2 start, Vector2 end, Color col)
    {
        Handles.BeginGUI();
        Handles.DrawBezier(
            start,
            end,
            start + Vector2.right * 80,
            end + Vector2.left * 80,
            col,
            null,
            3f);
        Handles.EndGUI();
    }

    float DistanceToBezier(Vector2 p, Vector2 a, Vector2 b)
    {
        return HandleUtility.DistancePointBezier(
            p,
            a,
            b,
            a + Vector2.right * 80,
            b + Vector2.left * 80);
    }

    #endregion

    #region Utils

    void AddNode()
    {
        Vector2 graphCenter = ScreenToGraph(position.size * 0.5f);

        DialogueNode n = new DialogueNode
        {
            Id = dialogueData.Nodes.Count,
            Text = "New dialogue...",
            Speaker = Speaker.Narrator,
            EditorPosition = graphCenter
        };

        dialogueData.Nodes.Add(n);
        EditorUtility.SetDirty(dialogueData);
    }


    void Reindex()
    {
        for (int i = 0; i < dialogueData.Nodes.Count; i++)
            dialogueData.Nodes[i].Id = i;

        foreach (var n in dialogueData.Nodes)
            if (n.Choices != null)
                foreach (var c in n.Choices)
                    if (c.NextNode >= dialogueData.Nodes.Count)
                        c.NextNode = -1;
    }

    float GetHeight(DialogueNode n)
    {
        int c = n.Choices != null ? n.Choices.Count : 0;
        return 140 + c * 38;
    }

    Color GetSpeakerColor(Speaker s)
    {
        return s switch
        {
            Speaker.Left => new Color(0.35f, 0.45f, 0.85f),
            Speaker.Right => new Color(0.85f, 0.35f, 0.35f),
            Speaker.Narrator => new Color(0.45f, 0.45f, 0.45f),
            _ => Color.white
        };
    }

    #endregion
    
    void HandleContextMenu()
    {
        Event e = Event.current;

        if (e.type == EventType.MouseDown && e.button == 1)
        {
            Vector2 graphPos = ScreenToGraph(e.mousePosition);

            GenericMenu menu = new GenericMenu();

            menu.AddItem(new GUIContent("Create Node"), false, () =>
            {
                DialogueNode n = new DialogueNode
                {
                    Id = dialogueData.Nodes.Count,
                    Text = "New dialogue...",
                    Speaker = Speaker.Narrator,
                    EditorPosition = graphPos
                };

                dialogueData.Nodes.Add(n);
                EditorUtility.SetDirty(dialogueData);
            });

            menu.ShowAsContext();
            e.Use();
        }
    }

}
