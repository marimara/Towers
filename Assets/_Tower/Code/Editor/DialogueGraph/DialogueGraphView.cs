using UnityEditor.Experimental.GraphView;
using UnityEngine;
using UnityEngine.UIElements;
using System.Collections.Generic;
using System.Linq;
using UnityEditor;

public class DialogueGraphView : GraphView
{
    private DialogueData currentData;

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

        RegisterCallback<KeyDownEvent>(evt =>
        {
            if (evt.keyCode == KeyCode.Delete || evt.keyCode == KeyCode.Backspace)
            {
                DeleteSelection();
                evt.StopPropagation();
            }
        });
    }

    // =========================
    // SALVAR CONEXÕES
    // =========================

    private GraphViewChange OnGraphChanged(GraphViewChange change)
    {
        if (currentData == null)
            return change;

        // EDGE CRIADA
        if (change.edgesToCreate != null)
        {
            foreach (var edge in change.edgesToCreate)
            {
                var fromView = edge.output.node as DialogueNodeView;
                var toView = edge.input.node as DialogueNodeView;

                if (fromView == null || toView == null)
                    continue;

                var fromNode = fromView.NodeData;

                int portIndex = fromView.ChoiceOutputs.IndexOf(edge.output);

                if (portIndex < 0)
                    continue;

                if (fromNode.Choices == null)
                    fromNode.Choices = new List<DialogueChoice>();

                while (fromNode.Choices.Count <= portIndex)
                    fromNode.Choices.Add(new DialogueChoice { NextNode = -1 });

                fromNode.Choices[portIndex].NextNode = toView.NodeData.Id;

                EditorUtility.SetDirty(currentData);
            }
        }

        // EDGE REMOVIDA
        if (change.elementsToRemove != null)
        {
            foreach (var element in change.elementsToRemove)
            {
                if (element is Edge edge)
                {
                    var fromView = edge.output.node as DialogueNodeView;
                    var toView = edge.input.node as DialogueNodeView;

                    if (fromView == null || toView == null)
                        continue;

                    var fromNode = fromView.NodeData;

                    if (fromNode.Choices != null)
                    {
                        fromNode.Choices.RemoveAll(c => c.NextNode == toView.NodeData.Id);
                        EditorUtility.SetDirty(currentData);
                    }
                }

                // NODE REMOVIDO
                if (element is DialogueNodeView nodeView)
                {
                    currentData.Nodes.Remove(nodeView.NodeData);
                    EditorUtility.SetDirty(currentData);
                }
            }
        }

        return change;
    }

    // =========================
    // DELETE PERSONALIZADO
    // =========================

    public override EventPropagation DeleteSelection()
    {
        if (currentData == null)
            return base.DeleteSelection();

        foreach (var element in selection.ToList())
        {
            if (element is DialogueNodeView nodeView)
            {
                currentData.Nodes.Remove(nodeView.NodeData);
            }

            if (element is Edge edge)
            {
                var fromView = edge.output.node as DialogueNodeView;
                var toView = edge.input.node as DialogueNodeView;

                if (fromView != null && fromView.NodeData.Choices != null)
                {
                    fromView.NodeData.Choices
                        .RemoveAll(c => c.NextNode == toView.NodeData.Id);
                }
            }
        }

        EditorUtility.SetDirty(currentData);

        return base.DeleteSelection();
    }

    // =========================
    // CRIAR NODE
    // =========================

    public override void BuildContextualMenu(ContextualMenuPopulateEvent evt)
    {
        evt.menu.AppendAction("Create Dialogue Node", _ =>
        {
            if (currentData == null)
                return;

            var node = new DialogueNode
            {
                Id = currentData.Nodes.Count,
                Text = "New dialogue...",
                Speaker = Speaker.Narrator,
                EditorPosition = evt.localMousePosition
            };

            currentData.Nodes.Add(node);
            EditorUtility.SetDirty(currentData);

            AddElement(CreateNodeView(node));
        });
    }

    public override List<Port> GetCompatiblePorts(Port startPort, NodeAdapter adapter)
    {
        return ports.ToList().Where(port =>
            startPort != port &&
            startPort.node != port.node &&
            startPort.direction != port.direction
        ).ToList();
    }

    // =========================
    // LOAD
    // =========================

    public void LoadData(DialogueData data)
    {
        currentData = data;

        DeleteElements(graphElements.ToList());

        if (data == null)
            return;

        foreach (var node in data.Nodes)
            AddElement(CreateNodeView(node));

        foreach (var view in nodes.OfType<DialogueNodeView>())
        {
            if (view.NodeData.Choices == null)
                continue;

            foreach (var choice in view.NodeData.Choices)
            {
                var target = nodes
                    .OfType<DialogueNodeView>()
                    .FirstOrDefault(n => n.NodeData.Id == choice.NextNode);

                if (target == null)
                    continue;

                int index = view.NodeData.Choices.IndexOf(choice);

                if (index < 0 || index >= view.ChoiceOutputs.Count)
                    continue;

                var port = view.ChoiceOutputs[index];
                var edge = port.ConnectTo(target.Input);

                AddElement(edge);
            }
        }
    }

    private DialogueNodeView CreateNodeView(DialogueNode node)
    {
        var view = new DialogueNodeView(node, currentData);
        view.SetPosition(new Rect(node.EditorPosition, new Vector2(250, 180)));
        return view;
    }
}
