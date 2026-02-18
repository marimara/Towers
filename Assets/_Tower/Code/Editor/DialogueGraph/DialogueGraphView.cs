using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.Experimental.GraphView;
using UnityEngine;
using UnityEngine.UIElements;

/// <summary>
/// GraphView for the dialogue editor.
///
/// Key design decisions vs. the original:
///  - OnGraphChanged is the SOLE writer of data. DeleteSelection() override is removed
///    to avoid double-mutation.
///  - Edge creation distinguishes NextOutputPort (linear flow) from ChoiceOutputPorts
///    (branching), writing the correct GUID field on each.
///  - Edge removal uses port-index lookup for per-choice precision, not RemoveAll by
///    target ID (which would wipe every choice pointing to the same node).
///  - Context menu node position is converted via contentViewContainer.WorldToLocal()
///    to correctly map panel space → graph content space.
///  - Node positions are stored in DialogueNodeEditorData (Editor-only companion asset),
///    not on the runtime DialogueNode.
/// </summary>
public class DialogueGraphView : GraphView
{
    private DialogueData _currentData;
    private DialogueNodeEditorData _editorData;

    // -------------------------------------------------------------------------
    // Constructor
    // -------------------------------------------------------------------------

    public DialogueGraphView()
    {
        style.flexGrow = 1;

        SetupZoom(ContentZoomer.DefaultMinScale, ContentZoomer.DefaultMaxScale);
        this.AddManipulator(new ContentDragger());
        this.AddManipulator(new SelectionDragger());
        this.AddManipulator(new RectangleSelector());

        var grid = new GridBackground();
        Insert(0, grid);
        grid.StretchToParentSize();

        graphViewChanged = OnGraphChanged;
    }

    // -------------------------------------------------------------------------
    // Load
    // -------------------------------------------------------------------------

    public void LoadData(DialogueData data)
    {
        _currentData = data;
        DeleteElements(graphElements.ToList());

        if (data == null)
        {
            _editorData = null;
            return;
        }

        _editorData = LoadOrCreateEditorData(data);
        data.BuildLookup();

        // Create node views
        foreach (var node in data.Nodes)
            AddElement(CreateNodeView(node));

        // Reconnect edges from saved data
        RestoreEdges();
    }

    // -------------------------------------------------------------------------
    // Toolbar-triggered explicit save
    // -------------------------------------------------------------------------

    public void SaveAssets()
    {
        if (_currentData != null)
            EditorUtility.SetDirty(_currentData);
        if (_editorData != null)
            EditorUtility.SetDirty(_editorData);
        AssetDatabase.SaveAssets();
    }

    // -------------------------------------------------------------------------
    // Start-node indicator
    // -------------------------------------------------------------------------

    public void RefreshStartNodeIndicators()
    {
        foreach (var view in nodes.OfType<DialogueNodeView>())
            view.SetStartNodeIndicator(view.NodeData.Guid == _currentData?.StartNodeGuid);
    }

    // -------------------------------------------------------------------------
    // GraphView overrides
    // -------------------------------------------------------------------------

    public override List<Port> GetCompatiblePorts(Port startPort, NodeAdapter adapter)
    {
        return ports.ToList().Where(port =>
            port != startPort &&
            port.node != startPort.node &&
            port.direction != startPort.direction
        ).ToList();
    }

    public override void BuildContextualMenu(ContextualMenuPopulateEvent evt)
    {
        evt.menu.AppendAction("Create Dialogue Node", _ =>
        {
            if (_currentData == null)
            {
                Debug.LogWarning("[DialogueGraphView] Load a DialogueData asset first.");
                return;
            }

            // Convert from panel space to graph content space
            Vector2 graphPos = contentViewContainer.WorldToLocal(evt.localMousePosition);

            var node = new DialogueNode
            {
                Guid        = System.Guid.NewGuid().ToString(),
                DisplayName = "",
                Text        = "New dialogue...",
                Speaker     = Speaker.Narrator
            };

            Undo.RecordObject(_currentData, "Create Dialogue Node");
            _currentData.Nodes.Add(node);
            _currentData.InvalidateLookup();
            EditorUtility.SetDirty(_currentData);

            _editorData?.SetPosition(node.Guid, graphPos);
            if (_editorData != null)
                EditorUtility.SetDirty(_editorData);

            AddElement(CreateNodeView(node));
            RefreshStartNodeIndicators();
        });
    }

    // -------------------------------------------------------------------------
    // GraphViewChange — sole data writer
    // -------------------------------------------------------------------------

    private GraphViewChange OnGraphChanged(GraphViewChange change)
    {
        if (_currentData == null)
            return change;

        // --- Edges created ---
        if (change.edgesToCreate != null)
        {
            foreach (var edge in change.edgesToCreate)
            {
                var fromView = edge.output.node as DialogueNodeView;
                var toView   = edge.input.node  as DialogueNodeView;
                if (fromView == null || toView == null) continue;

                Undo.RecordObject(_currentData, "Connect Dialogue Node");

                if (edge.output == fromView.NextOutputPort)
                {
                    // Linear next connection
                    fromView.NodeData.NextNodeGuid = toView.NodeData.Guid;
                }
                else
                {
                    // Choice connection — use port index for precision
                    int idx = fromView.ChoiceOutputPorts.IndexOf(edge.output);
                    if (idx >= 0 && idx < fromView.NodeData.Choices.Count)
                        fromView.NodeData.Choices[idx].NextNodeGuid = toView.NodeData.Guid;
                }

                EditorUtility.SetDirty(_currentData);
            }
        }

        // --- Elements removed ---
        if (change.elementsToRemove != null)
        {
            foreach (var element in change.elementsToRemove)
            {
                switch (element)
                {
                    case Edge edge:
                    {
                        var fromView = edge.output.node as DialogueNodeView;
                        var toView   = edge.input.node  as DialogueNodeView;
                        if (fromView == null || toView == null) break;

                        Undo.RecordObject(_currentData, "Disconnect Dialogue Node");

                        if (edge.output == fromView.NextOutputPort)
                        {
                            fromView.NodeData.NextNodeGuid = null;
                        }
                        else
                        {
                            // Per-port removal: only clear the specific choice, not all choices to the same target
                            int idx = fromView.ChoiceOutputPorts.IndexOf(edge.output);
                            if (idx >= 0 && idx < fromView.NodeData.Choices.Count)
                                fromView.NodeData.Choices[idx].NextNodeGuid = null;
                        }

                        EditorUtility.SetDirty(_currentData);
                        break;
                    }

                    case DialogueNodeView nodeView:
                    {
                        Undo.RecordObject(_currentData, "Delete Dialogue Node");
                        _currentData.Nodes.Remove(nodeView.NodeData);
                        _currentData.InvalidateLookup();
                        _editorData?.RemoveNode(nodeView.NodeData.Guid);

                        if (_editorData != null)
                            EditorUtility.SetDirty(_editorData);

                        EditorUtility.SetDirty(_currentData);
                        break;
                    }
                }
            }
        }

        // --- Moved elements — persist positions to editor data ---
        if (change.movedElements != null && _editorData != null)
        {
            foreach (var element in change.movedElements)
            {
                if (element is DialogueNodeView nodeView)
                {
                    var pos = nodeView.GetPosition().position;
                    _editorData.SetPosition(nodeView.NodeData.Guid, pos);
                }
            }
            EditorUtility.SetDirty(_editorData);
        }

        return change;
    }

    // -------------------------------------------------------------------------
    // Internal helpers
    // -------------------------------------------------------------------------

    private DialogueNodeView CreateNodeView(DialogueNode node)
    {
        var view = new DialogueNodeView(node, _currentData);

        Vector2 pos  = _editorData?.GetPosition(node.Guid) ?? new Vector2(100f, 100f);
        view.SetPosition(new Rect(pos, new Vector2(260f, 200f)));

        view.SetStartNodeIndicator(node.Guid == _currentData.StartNodeGuid);
        return view;
    }

    private void RestoreEdges()
    {
        var viewsByGuid = nodes
            .OfType<DialogueNodeView>()
            .ToDictionary(v => v.NodeData.Guid);

        foreach (var view in viewsByGuid.Values)
        {
            // Restore linear Next edge
            if (!string.IsNullOrEmpty(view.NodeData.NextNodeGuid) &&
                viewsByGuid.TryGetValue(view.NodeData.NextNodeGuid, out var nextTarget))
            {
                var edge = view.NextOutputPort.ConnectTo(nextTarget.InputPort);
                AddElement(edge);
            }

            // Restore choice edges
            if (view.NodeData.Choices == null) continue;

            for (int i = 0; i < view.NodeData.Choices.Count; i++)
            {
                var choice = view.NodeData.Choices[i];
                if (string.IsNullOrEmpty(choice.NextNodeGuid)) continue;
                if (!viewsByGuid.TryGetValue(choice.NextNodeGuid, out var choiceTarget)) continue;
                if (i >= view.ChoiceOutputPorts.Count) continue;

                var edge = view.ChoiceOutputPorts[i].ConnectTo(choiceTarget.InputPort);
                AddElement(edge);
            }
        }
    }

    private static DialogueNodeEditorData LoadOrCreateEditorData(DialogueData data)
    {
        string dataPath  = AssetDatabase.GetAssetPath(data);
        string dir       = Path.GetDirectoryName(dataPath);
        string baseName  = Path.GetFileNameWithoutExtension(dataPath);
        string layoutPath = Path.Combine(dir!, baseName + "_EditorLayout.asset").Replace('\\', '/');

        var existing = AssetDatabase.LoadAssetAtPath<DialogueNodeEditorData>(layoutPath);
        if (existing != null)
            return existing;

        var newData = ScriptableObject.CreateInstance<DialogueNodeEditorData>();
        AssetDatabase.CreateAsset(newData, layoutPath);
        AssetDatabase.SaveAssets();
        return newData;
    }
}
